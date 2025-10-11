using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject startMenuPanel;
    public GameObject slotMenuPanel;

    [Header("Slot Buttons")]
    public Button[] slotButtons;      // 3 slot buttons
    public Button[] deleteButtons;    // 3 delete buttons

    private void Start()
    {
        ShowStartMenu();
        SetupSlotButtons();
    }

    private void SetupSlotButtons()
    {
        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i + 1; // slots 1-3
            slotButtons[i].onClick.AddListener(() => OnSlotSelected(slotIndex));
            deleteButtons[i].onClick.AddListener(() => OnDeleteSlot(slotIndex));
        }
    }

    public void ShowStartMenu()
    {
        startMenuPanel.SetActive(true);
        slotMenuPanel.SetActive(false);
    }

    public void ShowSlotMenu()
    {
        startMenuPanel.SetActive(false);
        slotMenuPanel.SetActive(true);

        for (int i = 0; i < slotButtons.Length; i++)
        {
            int slotIndex = i + 1;
            bool exists = SaveSystem.SaveExists(slotIndex);

            slotButtons[i].GetComponentInChildren<TextMeshProUGUI>().text =
                exists ? "Continue" : "New Game";

            deleteButtons[i].gameObject.SetActive(exists);
        }
    }

    public void OnStartPressed()
    {
        ShowSlotMenu();
    }

    private void OnSlotSelected(int slotIndex)
    {
        bool exists = SaveSystem.SaveExists(slotIndex);

        if (exists)
        {
            SaveManager.Instance.LoadGame(slotIndex);
        }
        else
        {
            SaveManager.Instance.activeSlot = slotIndex;
            SaveManager.Instance.CurrentSave = new GameSaveData();
            SaveManager.Instance.SaveGame();
            SaveManager.Instance.LoadGame(slotIndex);
        }

        // Hide menu after loading
        startMenuPanel.SetActive(false);
        slotMenuPanel.SetActive(false);
    }

    private void OnDeleteSlot(int slotIndex)
    {
        // Optional: Add confirmation popup
        SaveManager.Instance.DeleteSave(slotIndex);
        ShowSlotMenu(); // refresh UI
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
