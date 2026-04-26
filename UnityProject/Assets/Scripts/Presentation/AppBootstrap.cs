using UnityEngine;

/// <summary>
/// Wires up all service implementations and registers them with the ServiceLocator.
/// Attach to a persistent GameObject in the first scene.
/// </summary>
public class AppBootstrap : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        // Storage
        var storage = new JsonStorageProvider();
        ServiceLocator.Register<IStorageProvider>(storage);

        // Domain services
        var courseService = new CourseService(storage);
        var sessionService = new SessionService(storage);
        ServiceLocator.Register<ICourseService>(courseService);
        ServiceLocator.Register<ISessionService>(sessionService);

        // AI layer — real Android speech recognizer on device, stub elsewhere
#if UNITY_ANDROID && !UNITY_EDITOR
        ITranscriptionService transcription = new AndroidSpeechTranscriptionService();
#else
        ITranscriptionService transcription = new StubTranscriptionService();
#endif
        var apiKey = PlayerPrefs.GetString("OpenAIApiKey", "");
        ISummaryService summary = !string.IsNullOrWhiteSpace(apiKey)
            ? (ISummaryService)new OpenAISummaryService(storage)
            : new StubSummaryService(storage);
        ServiceLocator.Register<ITranscriptionService>(transcription);
        ServiceLocator.Register<ISummaryService>(summary);

        // Export
        var export = new MarkdownExportService(sessionService, storage);
        ServiceLocator.Register<IExportService>(export);

        Debug.Log("[AppBootstrap] All services registered.");
    }
}
