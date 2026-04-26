using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings screen: lets the user enter and persist their OpenAI API key.
/// The key is stored in PlayerPrefs and read by OpenAISummaryService at runtime.
/// </summary>
public class SettingsScreen : ScreenBase
{
    [Header("References")]
    [SerializeField] private TMP_InputField _apiKeyInput;
    [SerializeField] private Button _saveButton;
    [SerializeField] private Button _backButton;
    [SerializeField] private TMP_Text _statusText;

    private const string ApiKeyPrefKey = "OpenAIApiKey";

    private void Awake()
    {
        _saveButton.onClick.AddListener(SaveSettings);
        _backButton.onClick.AddListener(AppNavigator.GoToCourseList);
    }

    protected override System.Threading.Tasks.Task OnShowAsync()
    {
        _apiKeyInput.text = PlayerPrefs.GetString(ApiKeyPrefKey, "");
        _statusText.text = "";
        return System.Threading.Tasks.Task.CompletedTask;
    }

    private void SaveSettings()
    {
        var key = _apiKeyInput.text.Trim();
        PlayerPrefs.SetString(ApiKeyPrefKey, key);
        PlayerPrefs.Save();
        _statusText.text = string.IsNullOrEmpty(key) ? "API key cleared." : "API key saved.";
    }
}
