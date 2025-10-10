using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Light2D))]
public class WindowLightController : MonoBehaviour
{
    private Light2D spriteLight;
    private DayNightCycle dayNightCycle;

    [Header("Light Schedule (in-game hours)")]
    public float turnOnHour = 18f;   // 6 PM in-game
    public float turnOffHour = 24f;  // Midnight in-game

    void Start()
    {
        spriteLight = GetComponent<Light2D>();
        dayNightCycle = FindFirstObjectByType<DayNightCycle>();

        if (spriteLight == null)
            Debug.LogError("No Light2D component found on this GameObject.");
        if (dayNightCycle == null)
            Debug.LogError("No DayNightCycle found in scene.");
    }

    void Update()
    {
        if (spriteLight == null || dayNightCycle == null) return;

        float currentHour = dayNightCycle.CurrentHour;

        // Handle overnight range (e.g., 18 to 24 or 18 to 6)
        bool shouldBeOn = false;
        if (turnOffHour > turnOnHour)
        {
            // Same-day range
            shouldBeOn = currentHour >= turnOnHour && currentHour < turnOffHour;
        }
        else
        {
            // Overnight range (e.g., 18 to 6)
            shouldBeOn = currentHour >= turnOnHour || currentHour < turnOffHour;
        }

        spriteLight.enabled = shouldBeOn;
    }
}
