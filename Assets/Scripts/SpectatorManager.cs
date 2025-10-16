using UnityEngine;
using System.Collections.Generic;

public class SpectatorManager : MonoBehaviour
{
    public static SpectatorManager Instance;

    [Tooltip("List of gameplay scripts to disable when in Spectator Mode.")]
    public List<MonoBehaviour> disableInSpectator = new List<MonoBehaviour>();

    private bool currentSpectatorState = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to SaveManager load event
        SaveManager.GameLoaded += ApplySpectatorMode;
    }

    private void OnDestroy()
    {
        SaveManager.GameLoaded -= ApplySpectatorMode;
    }

    private void Start()
    {
        // Apply once on start just in case
        ApplySpectatorMode();
    }

    public void ApplySpectatorMode()
    {
        if (SaveManager.Instance == null) return;

        bool isSpectator = SaveManager.Instance.Spectator;

        if (isSpectator == currentSpectatorState)
            return; // no change

        currentSpectatorState = isSpectator;

        foreach (var script in disableInSpectator)
        {
            if (script == null) continue;
            script.enabled = !isSpectator;
        }

        Debug.Log($"[SpectatorManager] Spectator mode {(isSpectator ? "ENABLED" : "DISABLED")}");
    }

    private void Update()
    {

    }
}
