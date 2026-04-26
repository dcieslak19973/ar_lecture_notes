using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// Stub summary service that produces a rule-based summary from notes.
/// Replace with an LLM/cloud API implementation without changing any call sites.
/// </summary>
public class StubSummaryService : ISummaryService
{
    private readonly IStorageProvider _storage;
    private const string Collection = "summaries";

    public StubSummaryService(IStorageProvider storage) => _storage = storage;

    public async Task<Summary> GenerateSummaryAsync(
        string sessionId,
        List<NoteItem> notes,
        List<TranscriptSegment> transcript)
    {
        var important = notes.Where(n => n.Marker == MarkerType.Important).Select(n => n.Content).ToList();
        var examItems = notes.Where(n => n.Marker == MarkerType.ExamItem).Select(n => n.Content).ToList();
        var assignments = notes.Where(n => n.Marker == MarkerType.Assignment).Select(n => n.Content).ToList();
        var bullets = notes.Where(n => n.Type == NoteItemType.Bullet).Select(n => n.Content).ToList();

        var condensed = bullets.Count > 0
            ? string.Join("\n", bullets.Take(5))
            : "No bullet notes captured.";

        var summary = new Summary
        {
            Id = System.Guid.NewGuid().ToString(),
            SessionId = sessionId,
            CondensedText = condensed,
            KeyTerms = important,
            ActionItems = examItems.Concat(assignments).ToList(),
            GeneratedAt = System.DateTime.UtcNow
        };

        await _storage.SaveAsync(Collection, summary.Id, summary);
        return summary;
    }
}
