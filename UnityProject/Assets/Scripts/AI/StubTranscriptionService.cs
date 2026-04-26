using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Stub transcription service that logs segments without actual speech recognition.
/// Replace with a real implementation (e.g., Android SpeechRecognizer via JNI,
/// or a cloud API) without changing any call sites.
/// </summary>
public class StubTranscriptionService : ITranscriptionService
{
    public bool IsAvailable => false;

    public Task StartTranscriptionAsync(string sessionId, Action<TranscriptSegment> onSegment)
    {
        Debug.Log("[Transcription] Stub: transcription not available in this build.");
        return Task.CompletedTask;
    }

    public Task StopTranscriptionAsync()
    {
        Debug.Log("[Transcription] Stub: stop called.");
        return Task.CompletedTask;
    }
}
