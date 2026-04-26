using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Real ITranscriptionService implementation using Android's on-device SpeechRecognizer.
/// Only compiled on Android device builds (not in the Editor).
/// Bridges Java callbacks via AndroidJavaProxy so no network or API key is required.
/// </summary>
#if UNITY_ANDROID && !UNITY_EDITOR
public class AndroidSpeechTranscriptionService : AndroidJavaProxy, ITranscriptionService
{
    private AndroidJavaObject _bridge;
    private Action<TranscriptSegment> _onSegment;
    private string _sessionId;
    private bool _running;
    private int _sequenceIndex;

    private readonly Queue<(string text, float confidence, bool isFinal)> _pending = new();
    private readonly object _lock = new object();

    public AndroidSpeechTranscriptionService()
        : base("com.arlecture.speech.ISpeechCallback") { }

    public bool IsAvailable
    {
        get
        {
            try
            {
                using var srClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
                using var activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                    .GetStatic<AndroidJavaObject>("currentActivity");
                return srClass.CallStatic<bool>("isRecognitionAvailable", activity);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SpeechRecognizer] IsAvailable check failed: {e.Message}");
                return false;
            }
        }
    }

    public Task StartTranscriptionAsync(string sessionId, Action<TranscriptSegment> onSegment)
    {
        _sessionId = sessionId;
        _onSegment = onSegment;
        _running = true;
        _sequenceIndex = 0;

        _bridge = new AndroidJavaObject("com.arlecture.speech.SpeechRecognizerBridge", this);
        _bridge.Call("startListening");

        MainThreadDispatcher.Instance.StartCoroutine(DispatchLoop());

        return Task.CompletedTask;
    }

    public Task StopTranscriptionAsync()
    {
        _running = false;
        if (_bridge != null)
        {
            _bridge.Call("stopListening");
            _bridge.Dispose();
            _bridge = null;
        }
        return Task.CompletedTask;
    }

    // Called from Java thread via AndroidJavaProxy
    void onPartialResult(string text)
    {
        // Partial results are display-only; not persisted as segments
        MainThreadDispatcher.Instance.Enqueue(() =>
        {
            // Could update a live transcript display here if desired
        });
    }

    // Called from Java thread via AndroidJavaProxy
    void onFinalResult(string text, float confidence)
    {
        lock (_lock) { _pending.Enqueue((text, confidence, true)); }
    }

    // Called from Java thread via AndroidJavaProxy
    void onError(int errorCode)
    {
        Debug.LogWarning($"[SpeechRecognizer] Error code: {errorCode}");
    }

    private IEnumerator DispatchLoop()
    {
        while (_running)
        {
            List<(string text, float confidence, bool isFinal)> batch = null;
            lock (_lock)
            {
                if (_pending.Count > 0)
                {
                    batch = new List<(string, float, bool)>(_pending);
                    _pending.Clear();
                }
            }
            if (batch != null)
            {
                foreach (var (text, confidence, _) in batch)
                {
                    var segment = new TranscriptSegment
                    {
                        Id = Guid.NewGuid().ToString(),
                        SessionId = _sessionId,
                        Text = text,
                        Confidence = confidence,
                        Timestamp = DateTime.UtcNow,
                        SequenceIndex = _sequenceIndex++
                    };
                    _onSegment?.Invoke(segment);
                }
            }
            yield return null;
        }
    }
}
#endif
