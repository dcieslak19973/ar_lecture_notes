using System.Collections;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen: active lecture capture — bullet notes, transcription, markers.
/// </summary>
public class LectureCaptureScreen : ScreenBase
{
    [Header("References")]
    [SerializeField] private TMP_Text _sessionTitleText;
    [SerializeField] private TMP_Text _transcriptFeedText;
    [SerializeField] private TMP_InputField _bulletInput;
    [SerializeField] private Button _addBulletButton;
    [SerializeField] private Button _markImportantButton;
    [SerializeField] private Button _markExamButton;
    [SerializeField] private Button _markAssignmentButton;
    [SerializeField] private Button _endSessionButton;
    [SerializeField] private Button _toggleTranscriptionButton;
    [SerializeField] private Transform _noteListContainer;
    [SerializeField] private GameObject _noteRowPrefab;

    private ISessionService _sessions;
    private ITranscriptionService _transcription;
    private LectureSession _activeSession;

    private void Awake()
    {
        _sessions = ServiceLocator.Get<ISessionService>();
        _transcription = ServiceLocator.Get<ITranscriptionService>();

        _addBulletButton.onClick.AddListener(() => RunAsync(AddBulletAsync, "AddBullet"));
        _markImportantButton.onClick.AddListener(() => RunAsync(() => AddMarkerAsync(MarkerType.Important), "MarkImportant"));
        _markExamButton.onClick.AddListener(() => RunAsync(() => AddMarkerAsync(MarkerType.ExamItem), "MarkExam"));
        _markAssignmentButton.onClick.AddListener(() => RunAsync(() => AddMarkerAsync(MarkerType.Assignment), "MarkAssignment"));
        _endSessionButton.onClick.AddListener(() => RunAsync(EndSessionAsync, "EndSession"));
        _toggleTranscriptionButton.onClick.AddListener(() => RunAsync(ToggleTranscriptionAsync, "ToggleTranscription"));

        if (!_transcription.IsAvailable)
            _toggleTranscriptionButton.interactable = false;
    }

    protected override async Task OnShowAsync()
    {
        _transcriptBuffer.Clear();
        _transcriptFeedText.text = "";
        _activeSession = await _sessions.StartSessionAsync(
            AppNavigator.CurrentCourseId,
            title: "");
        _sessionTitleText.text = _activeSession.Title;
    }

    private async Task AddBulletAsync()
    {
        var text = _bulletInput.text.Trim();
        if (string.IsNullOrEmpty(text)) return;
        var note = await _sessions.AddNoteItemAsync(_activeSession.Id, text, NoteItemType.Bullet);
        _bulletInput.text = "";
        AppendNoteRow(note);
    }

    private async Task AddMarkerAsync(MarkerType marker)
    {
        var label = marker.ToString();
        var note = await _sessions.AddNoteItemAsync(_activeSession.Id, $"[{label}]", NoteItemType.Marker, marker);
        AppendNoteRow(note);
    }

    private bool _transcribing = false;
    private readonly StringBuilder _transcriptBuffer = new();
    private const int MaxTranscriptChars = 5000;
    private async Task ToggleTranscriptionAsync()
    {
        if (_transcribing)
        {
            await _transcription.StopTranscriptionAsync();
            _transcribing = false;
            _toggleTranscriptionButton.GetComponentInChildren<TMP_Text>().text = "Start Mic";
        }
        else
        {
            await _transcription.StartTranscriptionAsync(_activeSession.Id, OnTranscriptSegment);
            _transcribing = true;
            _toggleTranscriptionButton.GetComponentInChildren<TMP_Text>().text = "Stop Mic";
        }
    }

    private void OnTranscriptSegment(TranscriptSegment segment)
    {
        if (_transcriptBuffer.Length > 0)
            _transcriptBuffer.Append('\n');
        _transcriptBuffer.Append(segment.Text);

        if (_transcriptBuffer.Length > MaxTranscriptChars)
            _transcriptBuffer.Remove(0, _transcriptBuffer.Length - MaxTranscriptChars);

        _transcriptFeedText.text = _transcriptBuffer.ToString();
        RunAsync(() => _sessions.AddNoteItemAsync(
            _activeSession.Id, segment.Text, NoteItemType.Transcript), "SaveTranscript");
    }

    private async Task EndSessionAsync()
    {
        if (_transcribing) await _transcription.StopTranscriptionAsync();
        await _sessions.EndSessionAsync(_activeSession.Id);
        AppNavigator.GoToReview(_activeSession.Id);
    }

    private void AppendNoteRow(NoteItem note)
    {
        var row = Instantiate(_noteRowPrefab, _noteListContainer);
        row.GetComponentInChildren<TMP_Text>().text = note.Content;
    }
}
