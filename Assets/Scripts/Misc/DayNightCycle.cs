using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings (minutes)")]
    public float dayDurationMinutes = 30f;
    public float nightDurationMinutes = 15f;

    [Header("Lighting")]
    public Light2D globalLight;
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.1f, 0.1f, 0.3f);
    public float dayIntensity = 1f;
    public float nightIntensity = 0.3f;

    [Header("Optional Start Time (0-24)")]
    [Range(0f, 24f)]
    public float startHour = 6f;

    public float CurrentHour;
    public bool IsDay { get; private set; }

    private float daySeconds;
    private float nightSeconds;

    void Update()
    {
        if (startHour < 0)
        {
            return;
        }
        else
        {
            AdvanceTime();
            UpdateLighting();
        }
    }

    void AdvanceTime()
    {


        float deltaTime = Time.deltaTime;

        if (IsDay)
        {
            CurrentHour += (12f / daySeconds) * deltaTime; // 12-hour day
        }
        else
        {
            CurrentHour += (6f / nightSeconds) * deltaTime; // 6-hour night
        }

        if (CurrentHour >= 24f)
            CurrentHour -= 24f;

        IsDay = CurrentHour >= 6f && CurrentHour < 18f;
    }

    void UpdateLighting()
    {
        if (globalLight == null) return;

        // Dawn transition: 5–6 AM
        if (CurrentHour >= 5f && CurrentHour < 6f)
        {
            float t = Mathf.InverseLerp(5f, 6f, CurrentHour);
            globalLight.color = Color.Lerp(nightColor, dayColor, t);
            globalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, t);
        }
        // Daytime: 6 AM – 6 PM
        else if (CurrentHour >= 6f && CurrentHour < 18f)
        {
            globalLight.color = dayColor;
            globalLight.intensity = dayIntensity;
        }
        // Dusk transition: 6–7 PM
        else if (CurrentHour >= 18f && CurrentHour < 19f)
        {
            float t = Mathf.InverseLerp(18f, 19f, CurrentHour);
            globalLight.color = Color.Lerp(dayColor, nightColor, t);
            globalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, t);
        }
        // Night: 7 PM – 5 AM
        else
        {
            globalLight.color = nightColor;
            globalLight.intensity = nightIntensity;
        }
    }

    public void SetTime(float time)
    {
        startHour = 6;
        CurrentHour = time;
        daySeconds = dayDurationMinutes * 60f;
        nightSeconds = nightDurationMinutes * 60f;

        IsDay = CurrentHour >= 6f && CurrentHour < 18f;
    }
}
