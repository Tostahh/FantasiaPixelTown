using UnityEngine;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject pauseMenuPanel;
    public GameObject settingsPanel;

    public Image pauseButton;

    [SerializeField] private Sprite PausedSprite;
    [SerializeField] private Sprite PlaySprite;

    [Header("References")]
    public SaveManager saveSystem; // assign in Inspector if needed

    private bool isPaused = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        pauseMenuPanel.SetActive(isPaused);
        settingsPanel.SetActive(false); // ensure settings close when resuming
        Time.timeScale = isPaused ? 0f : 1f;

        if(isPaused)
        {
            pauseButton.sprite = PausedSprite;
        }
        else
        {
            pauseButton.sprite = PlaySprite;
        }
    }

    public void ManualSave()
    {
        if (saveSystem != null)
        {
            saveSystem.SaveGame();
            Debug.Log("Game manually saved.");
        }
        else
        {
            Debug.LogWarning("SaveSystem reference not set in PauseMenuController.");
        }
    }

    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}
