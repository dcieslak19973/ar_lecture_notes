using UnityEngine;
using TMPro;

/// <summary>
/// Floating toast notification. Show short feedback messages (e.g., "Exported!").
/// Call Toast.Show("message") from anywhere.
/// </summary>
public class Toast : MonoBehaviour
{
    private static Toast _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _messageText;

    private void Awake()
    {
        _instance = this;
        _panel.SetActive(false);
    }

    public static void Show(string message, float duration = 2f)
    {
        if (_instance == null) return;
        _instance.ShowInternal(message, duration);
    }

    private void ShowInternal(string message, float duration)
    {
        _messageText.text = message;
        _panel.SetActive(true);
        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), duration);
    }

    private void Hide() => _panel.SetActive(false);
}
