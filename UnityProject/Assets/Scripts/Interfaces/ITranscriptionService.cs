using System;
using System.Threading.Tasks;

public interface ITranscriptionService
{
    bool IsAvailable { get; }
    Task StartTranscriptionAsync(string sessionId, Action<TranscriptSegment> onSegment);
    Task StopTranscriptionAsync();
}
