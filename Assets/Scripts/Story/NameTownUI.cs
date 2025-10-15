using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TownNamingUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI promptLabel;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button confirmButton;

    private Action<string> onConfirm;

    private void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        panel?.SetActive(false);
    }

    public void Open(string prompt, Action<string> callback)
    {
        promptLabel.text = prompt;
        onConfirm = callback;
        inputField.text = "";
        panel.SetActive(true);
        inputField.Select();
        inputField.ActivateInputField();
    }

    private void OnConfirmClicked()
    {
        string townName = inputField.text.Trim();
        panel.SetActive(false);
        onConfirm?.Invoke(townName);
    }
}
