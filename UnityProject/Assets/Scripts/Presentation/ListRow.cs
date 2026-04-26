using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable row for course and session lists.
/// Wire up in the prefab inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class ListRow : MonoBehaviour
{
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _subtitleText;

    private Button _button;

    private void Awake() => _button = GetComponent<Button>();

    public void Configure(string title, string subtitle, System.Action onClick)
    {
        _titleText.text = title;
        if (_subtitleText != null) _subtitleText.text = subtitle ?? "";
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(() => onClick?.Invoke());
    }
}
