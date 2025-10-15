using UnityEngine;

[CreateAssetMenu(menuName = "Story/Custom Actions/Name Town")]
public class NameTownActionSO : CustomStoryActionSO
{
    [Header("Optional Prompt Settings")]
    public string promptText = "What would you like to name your town?";

    public override void Execute()
    {
        var namingUI = GameObject.FindFirstObjectByType<TownNamingUI>();
        if (namingUI == null)
        {
            Debug.LogWarning("[NameTownAction] No TownNamingUI found in scene!");
            return;
        }

        namingUI.Open(promptText, OnTownNameConfirmed);
    }

    private void OnTownNameConfirmed(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            Debug.LogWarning("[NameTownAction] Town name cannot be empty!");
            return;
        }

        var gameSaveData = SaveManager.Instance?.CurrentSave;
        if (gameSaveData != null)
        {
            ChapterProgressionManager.Instance.TownName = newName;
            gameSaveData.TownName = newName;
            SaveManager.Instance.SaveGame();
            Debug.Log($"[NameTownAction] Town named: {newName}");
        }
        else
        {
            Debug.LogWarning("[NameTownAction] Could not save town name — progress data missing!");
        }
    }
}
