using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISummaryService
{
    Task<Summary> GenerateSummaryAsync(string sessionId, List<NoteItem> notes, List<TranscriptSegment> transcript);
}
