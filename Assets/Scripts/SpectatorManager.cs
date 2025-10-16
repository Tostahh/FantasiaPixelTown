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
        Instance = this;
    }

    public void ApplySpectatorMode()
    {
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
        if(SaveManager.Instance.Spectator)
        {
            ApplySpectatorMode();
        }
    }
}
