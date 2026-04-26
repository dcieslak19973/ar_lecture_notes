using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Calls the OpenAI Chat Completions API (gpt-4o-mini) to produce a structured
/// lecture summary. Falls back to StubSummaryService when no API key is configured.
///
/// API key must be stored in PlayerPrefs under the key "OpenAIApiKey".
/// Obtain a key at https://platform.openai.com/api-keys (pay-as-you-go account).
/// </summary>
public class OpenAISummaryService : ISummaryService
{
    private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string Model = "gpt-4o-mini";
    private const string ApiKeyPrefKey = "OpenAIApiKey";
    private const string StorageCollection = "summaries";
    private const int TimeoutSeconds = 60;

    private readonly IStorageProvider _storage;
    private readonly StubSummaryService _fallback;

    public OpenAISummaryService(IStorageProvider storage)
    {
        _storage = storage;
        _fallback = new StubSummaryService(storage);
    }

    public async Task<Summary> GenerateSummaryAsync(
        string sessionId,
        List<NoteItem> notes,
        List<TranscriptSegment> transcript)
    {
        var apiKey = PlayerPrefs.GetString(ApiKeyPrefKey, "");
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Debug.LogWarning("[OpenAISummaryService] No API key found in PlayerPrefs (key: \"OpenAIApiKey\"). Falling back to stub.");
            return await _fallback.GenerateSummaryAsync(sessionId, notes, transcript);
        }

        try
        {
            var gptData = await CallOpenAIAsync(apiKey, notes, transcript);
            var summary = new Summary
            {
                Id = Guid.NewGuid().ToString(),
                SessionId = sessionId,
                CondensedText = gptData.condensed ?? "Summary unavailable.",
                KeyTerms = gptData.keyTerms ?? new List<string>(),
                ActionItems = gptData.actionItems ?? new List<string>(),
                GeneratedAt = DateTime.UtcNow
            };
            await _storage.SaveAsync(StorageCollection, summary.Id, summary);
            return summary;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[OpenAISummaryService] API call failed: {ex.Message}. Falling back to stub.");
            return await _fallback.GenerateSummaryAsync(sessionId, notes, transcript);
        }
    }

    private async Task<GptSummaryData> CallOpenAIAsync(
        string apiKey,
        List<NoteItem> notes,
        List<TranscriptSegment> transcript)
    {
        var prompt = BuildPrompt(notes, transcript);
        var requestBody = BuildRequestJson(prompt);
        var bodyBytes = Encoding.UTF8.GetBytes(requestBody);

        using var req = new UnityWebRequest(ApiUrl, UnityWebRequest.kHttpVerbPOST);
        req.uploadHandler = new UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = TimeoutSeconds;
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", "Bearer " + apiKey);

        await SendAsync(req);

        if (req.result != UnityWebRequest.Result.Success)
        {
            var responseBody = req.downloadHandler != null ? req.downloadHandler.text : null;
            var errorDetail = ExtractOpenAIErrorDetail(responseBody);
            var message = $"HTTP {req.responseCode}: {req.error}";

            if (!string.IsNullOrWhiteSpace(errorDetail))
                message += $" Response: {errorDetail}";

            throw new Exception(message);
        }

        return ParseResponse(req.downloadHandler.text);
    }

    private static string ExtractOpenAIErrorDetail(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
            return null;

        try
        {
            var errorResponse = JsonUtility.FromJson<OpenAIErrorResponse>(responseBody);
            if (!string.IsNullOrWhiteSpace(errorResponse?.error?.message))
                return errorResponse.error.message;
        }
        catch
        {
            // Ignore parsing issues and fall back to the raw response body.
        }

        return responseBody;
    }

    [Serializable]
    private class OpenAIErrorResponse
    {
        public OpenAIErrorPayload error;
    }

    [Serializable]
    private class OpenAIErrorPayload
    {
        public string message;
    }

    private static string BuildPrompt(List<NoteItem> notes, List<TranscriptSegment> transcript)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a study assistant. Summarize the following lecture into a JSON object with no markdown fences.");
        sb.AppendLine("Return ONLY valid JSON in this exact format:");
        sb.AppendLine("{\"condensed\":\"...\",\"keyTerms\":[\"term1\",\"term2\"],\"actionItems\":[\"item1\",\"item2\"]}");
        sb.AppendLine();

        if (transcript != null && transcript.Count > 0)
        {
            sb.AppendLine("=== TRANSCRIPT ===");
            foreach (var seg in transcript)
                sb.AppendLine(seg.Text);
            sb.AppendLine();
        }

        if (notes != null && notes.Count > 0)
        {
            sb.AppendLine("=== NOTES ===");
            foreach (var note in notes)
            {
                var prefix = note.Type == NoteItemType.Marker ? $"[{note.Marker}] " : "• ";
                sb.AppendLine(prefix + note.Content);
            }
        }

        return sb.ToString();
    }

    private static string BuildRequestJson(string userPrompt)
    {
        // Manually escape the prompt to avoid requiring a JSON library.
        var escaped = userPrompt
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");

        return $"{{\"model\":\"{Model}\",\"messages\":[{{\"role\":\"user\",\"content\":\"{escaped}\"}}],\"response_format\":{{\"type\":\"json_object\"}},\"temperature\":0.3}}";
    }

    private static GptSummaryData ParseResponse(string responseJson)
    {
        // Extract "content" field from the first choice's message.
        var contentStart = responseJson.IndexOf("\"content\":", StringComparison.Ordinal);
        if (contentStart < 0)
            throw new Exception("No 'content' field in response.");

        // Find the JSON string value after "content":
        var quoteStart = responseJson.IndexOf('"', contentStart + 10);
        if (quoteStart < 0)
            throw new Exception("Malformed content field.");

        var content = ExtractJsonStringValue(responseJson, quoteStart);

        // content should now be the inner JSON returned by the model.
        // Unescape newlines and try to parse.
        content = content.Trim();

        // Strip optional markdown fences the model might include despite instructions.
        if (content.StartsWith("```"))
        {
            var fenceEnd = content.IndexOf('\n');
            content = fenceEnd >= 0 ? content.Substring(fenceEnd + 1) : content.Substring(3);
        }
        if (content.EndsWith("```"))
            content = content.Substring(0, content.LastIndexOf("```", StringComparison.Ordinal));

        content = content.Trim();

        var data = JsonUtility.FromJson<GptSummaryData>(content);
        if (data == null)
            throw new Exception("Failed to parse GptSummaryData from model output.");

        return data;
    }

    /// <summary>
    /// Extracts the unescaped string value starting with the opening quote at <paramref name="quoteIndex"/>
    /// from <paramref name="json"/>. Handles JSON escape sequences.
    /// </summary>
    private static string ExtractJsonStringValue(string json, int quoteIndex)
    {
        var sb = new StringBuilder();
        var i = quoteIndex + 1;
        while (i < json.Length)
        {
            var c = json[i];
            if (c == '"') break;
            if (c == '\\' && i + 1 < json.Length)
            {
                i++;
                switch (json[i])
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 < json.Length)
                        {
                            var hex = json.Substring(i + 1, 4);
                            try
                            {
                                var codePoint = Convert.ToInt32(hex, 16);
                                sb.Append((char)codePoint);
                                i += 4;
                                break;
                            }
                            catch
                            {
                                // Preserve malformed escape text rather than silently
                                // corrupting it into a different value.
                            }
                        }
                        sb.Append('\\');
                        sb.Append('u');
                        break;
                    default: sb.Append(json[i]); break;
                }
            }
            else
            {
                sb.Append(c);
            }
            i++;
        }
        return sb.ToString();
    }

    private static Task SendAsync(UnityWebRequest req)
    {
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.TrySetResult(true);
        return tcs.Task;
    }

    // ── Inner serializable types ──────────────────────────────────────────────

    [Serializable]
    private class GptSummaryData
    {
        public string condensed;
        public List<string> keyTerms = new List<string>();
        public List<string> actionItems = new List<string>();
    }
}
