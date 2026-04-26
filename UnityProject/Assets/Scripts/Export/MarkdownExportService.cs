using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class MarkdownExportService : IExportService
{
    private readonly ISessionService _sessions;
    private readonly IStorageProvider _storage;

    public MarkdownExportService(ISessionService sessions, IStorageProvider storage)
    {
        _sessions = sessions;
        _storage = storage;
    }

    public async Task<string> ExportToMarkdownAsync(string sessionId)
    {
        var session = await _sessions.GetSessionAsync(sessionId);
        if (session == null) return string.Empty;

        var notes = await _sessions.GetNoteItemsAsync(sessionId);
        var summary = session.SummaryId != null
            ? await _storage.LoadAsync<Summary>("summaries", session.SummaryId)
            : null;

        var course = await _storage.LoadAsync<Course>("courses", session.CourseId);
        var courseName = course?.Name ?? "Unknown Course";

        var sb = new StringBuilder();

        // Obsidian YAML frontmatter
        sb.AppendLine("---");
        sb.AppendLine($"title: {EscapeYamlString(session.Title)}");
        sb.AppendLine($"date: {session.StartTime:yyyy-MM-dd}");
        sb.AppendLine($"course: {EscapeYamlString(courseName)}");
        sb.AppendLine($"duration_min: {(session.EndTime - session.StartTime).TotalMinutes:F0}");
        sb.AppendLine("tags:");
        sb.AppendLine("  - lecture");
        sb.AppendLine($"  - {EscapeYamlString(courseName)}");
        sb.AppendLine("---");
        sb.AppendLine();

        sb.AppendLine($"# {session.Title}");
        sb.AppendLine($"**Date:** {session.StartTime:yyyy-MM-dd}  ");
        sb.AppendLine($"**Duration:** {(session.EndTime - session.StartTime).TotalMinutes:F0} min");
        sb.AppendLine();

        if (summary != null)
        {
            sb.AppendLine("## Summary");
            sb.AppendLine(summary.CondensedText);
            sb.AppendLine();

            if (summary.KeyTerms?.Count > 0)
            {
                sb.AppendLine("### Key Terms");
                foreach (var t in summary.KeyTerms) sb.AppendLine($"- {t}");
                sb.AppendLine();
            }

            if (summary.ActionItems?.Count > 0)
            {
                sb.AppendLine("### Action Items");
                foreach (var a in summary.ActionItems) sb.AppendLine($"- [ ] {a}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("## Notes");
        foreach (var note in notes)
        {
            var prefix = note.Marker switch
            {
                MarkerType.Important => "⭐ ",
                MarkerType.ExamItem  => "📝 ",
                MarkerType.Assignment => "📌 ",
                _                    => ""
            };
            if (note.Type == NoteItemType.Transcript)
                sb.AppendLine($"> {note.Content}");
            else
                sb.AppendLine($"- {prefix}{note.Content}");
        }

        return sb.ToString();
    }

    public async Task<string> ExportToPlainTextAsync(string sessionId)
    {
        var md = await ExportToMarkdownAsync(sessionId);
        // strip markdown symbols for plain text
        return md.Replace("#", "").Replace("**", "").Replace("- [ ]", "[]").Replace("- ", "  • ");
    }

    public Task SaveToFileAsync(string content, string filename)
    {
        var path = Path.Combine(Application.persistentDataPath, "exports", filename);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content, Encoding.UTF8);
        Debug.Log($"[Export] Saved to {path}");
        return Task.CompletedTask;
    }

    public async Task<string> ExportToObsidianVaultAsync(string sessionId)
    {
        var session = await _sessions.GetSessionAsync(sessionId);
        if (session == null) throw new InvalidOperationException($"Session {sessionId} not found.");

        var course = await _storage.LoadAsync<Course>("courses", session.CourseId);
        var courseName = SanitizeFolderName(course?.Name ?? "Unknown Course");

        var content = await ExportToMarkdownAsync(sessionId);

        // Vault structure: {VaultPath}/{CourseName}/{Date} - {Title}.md
        var vaultPath = ObsidianSettings.VaultPath;
        var courseFolder = Path.Combine(vaultPath, courseName);
        Directory.CreateDirectory(courseFolder);

        var safeTitle = SanitizeFolderName(session.Title);
        var filename = $"{session.StartTime:yyyy-MM-dd} - {safeTitle}.md";
        var filePath = Path.Combine(courseFolder, filename);

        File.WriteAllText(filePath, content, Encoding.UTF8);
        Debug.Log($"[Obsidian] Exported to {filePath}");
        return filePath;
    }

    private static string SanitizeFolderName(string name) =>
        string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();

    private static string SanitizeTag(string name) =>
        name.ToLowerInvariant().Replace(" ", "-");

    private static string EscapeYamlString(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var escaped = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n");
        return $"\"{escaped}\"";
    }
}
