using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Screen: post-session review — summary, notes, search, export.
/// </summary>
public class ReviewScreen : ScreenBase
{
    [Header("References")]
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _summaryText;
    [SerializeField] private TMP_Text _keyTermsText;
    [SerializeField] private TMP_Text _actionItemsText;
    [SerializeField] private Transform _noteListContainer;
    [SerializeField] private GameObject _noteRowPrefab;
    [SerializeField] private TMP_InputField _searchInput;
    [SerializeField] private Button _searchButton;
    [SerializeField] private Button _exportMarkdownButton;
    [SerializeField] private Button _exportObsidianButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private GameObject _busyIndicator;

    private ISessionService _sessions;
    private ISummaryService _summaryService;
    private IExportService _export;

    private void Awake()
    {
        _sessions = ServiceLocator.Get<ISessionService>();
        _summaryService = ServiceLocator.Get<ISummaryService>();
        _export = ServiceLocator.Get<IExportService>();

        _searchButton.onClick.AddListener(() => RunAsync(SearchAsync, "Search"));
        _exportMarkdownButton.onClick.AddListener(() => RunAsync(ExportAsync, "Export"));
        _exportObsidianButton.onClick.AddListener(() => RunAsync(ExportToObsidianAsync, "ExportObsidian"));
        _backButton.onClick.AddListener(AppNavigator.GoToCourseList);
    }

    protected override async Task OnShowAsync()
    {
        SetBusy(true);
        var sessionId = AppNavigator.CurrentSessionId;
        var session = await _sessions.GetSessionAsync(sessionId);
        var notes = await _sessions.GetNoteItemsAsync(sessionId);
        var summary = await _summaryService.GenerateSummaryAsync(sessionId, notes, new System.Collections.Generic.List<TranscriptSegment>());

        _titleText.text = session.Title;
        _summaryText.text = summary.CondensedText;
        _keyTermsText.text = summary.KeyTerms.Count > 0
            ? string.Join(", ", summary.KeyTerms)
            : "—";
        _actionItemsText.text = summary.ActionItems.Count > 0
            ? string.Join("\n• ", summary.ActionItems)
            : "—";

        PopulateNotes(notes);
        SetBusy(false);
    }

    private void PopulateNotes(List<NoteItem> notes)
    {
        foreach (Transform child in _noteListContainer) Destroy(child.gameObject);
        foreach (var note in notes)
        {
            var row = Instantiate(_noteRowPrefab, _noteListContainer);
            row.GetComponentInChildren<TMP_Text>().text = note.Content;
        }
    }

    private async Task SearchAsync()
    {
        var query = _searchInput.text.Trim();
        if (string.IsNullOrEmpty(query)) return;
        var results = await _sessions.SearchSessionsAsync(query);
        // TODO: show search results in a separate panel
        Debug.Log($"[Search] {results.Count} results for '{query}'");
    }

    private async Task ExportAsync()
    {
        SetBusy(true);
        var content = await _export.ExportToMarkdownAsync(AppNavigator.CurrentSessionId);
        var filename = $"notes_{AppNavigator.CurrentSessionId[..8]}.md";
        await _export.SaveToFileAsync(content, filename);
        SetBusy(false);
    }

    private async Task ExportToObsidianAsync()
    {
        SetBusy(true);
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            bool granted = await AndroidPermissionHelper.RequestPermissionAsync(
                "android.permission.WRITE_EXTERNAL_STORAGE");
            if (!granted)
            {
                Toast.Show("Storage permission denied — cannot write to Obsidian vault.");
                return;
            }
#endif
            var path = await _export.ExportToObsidianVaultAsync(AppNavigator.CurrentSessionId);
            Toast.Show($"Saved to Obsidian vault");
            Debug.Log($"[Obsidian] Written to: {path}");
        }
        catch (System.Exception e)
        {
            Toast.Show($"Export failed: {e.Message}");
            Debug.LogError($"[Obsidian] Export error: {e}");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy) => _busyIndicator.SetActive(busy);
}
