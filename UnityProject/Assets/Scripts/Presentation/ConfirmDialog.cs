using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Reusable confirmation dialog. Usage:
///   ConfirmDialog.Show("Delete this course?", onConfirm: () => { ... });
/// </summary>
public class ConfirmDialog : MonoBehaviour
{
    private static ConfirmDialog _instance;

    [SerializeField] private GameObject _panel;
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private void Awake()
    {
        _instance = this;
        _cancelButton.onClick.AddListener(Hide);
        _panel.SetActive(false);
    }

    public static void Show(string message, Action onConfirm)
    {
        if (_instance == null) return;
        _instance.ShowInternal(message, onConfirm);
    }

    private void ShowInternal(string message, Action onConfirm)
    {
        _messageText.text = message;
        _confirmButton.onClick.RemoveAllListeners();
        _confirmButton.onClick.AddListener(() => { onConfirm?.Invoke(); Hide(); });
        _panel.SetActive(true);
    }

    private void Hide() => _panel.SetActive(false);
}
