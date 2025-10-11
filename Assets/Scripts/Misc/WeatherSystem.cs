using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

public class WeatherSystem : MonoBehaviour
{
    public static Action<WeatherType> OnWeatherChanged = delegate { };

    [Header("References")]
    public DayNightCycle dayNightCycle;
    public Light2D globalLight;

    [Header("Particle Systems")]
    public ParticleSystem rainParticles;
    public ParticleSystem windParticles;
    public ParticleSystem sunnyParticles; // optional dust/sun rays

    [Header("Weather Tint Settings")]
    public Color clearTint = Color.white;
    public Color sunnyTint = Color.white;
    public Color rainyTint = new Color(0.6f, 0.7f, 0.8f);
    public Color windyTint = new Color(0.9f, 0.9f, 0.95f);

    [Header("Intensity Modifiers")]
    [Range(0.5f, 1.2f)] public float clearIntensity = 1f;
    [Range(0.5f, 1.2f)] public float sunnyIntensity = 1f;
    [Range(0.5f, 1.2f)] public float rainyIntensity = 0.8f;
    [Range(0.5f, 1.2f)] public float windyIntensity = 0.9f;

    [Header("Change Settings")]
    public int changeHour = 6; // new weather each in-game morning
    public float chanceClear = 0.4f;
    public float chanceSunny = 0.3f;
    public float chanceRainy = 0.2f;
    public float chanceWindy = 0.1f;

    public WeatherType CurrentWeather { get; private set; }

    private float lastWeatherChangeDay = -1f;

    void Start()
    {
        if (!dayNightCycle)
            dayNightCycle = FindObjectOfType<DayNightCycle>();

        // If no save data injected, randomize
        if (CurrentWeather == 0 && lastWeatherChangeDay == -1f)
        {
            ChooseRandomWeather();
            ApplyWeatherSettings();
        }
    }


    void Update()
    {
        // Check once per new in-game day (around 6am)
        if (dayNightCycle.CurrentHour >= changeHour && lastWeatherChangeDay != Mathf.Floor(dayNightCycle.CurrentHour / 24f))
        {
            ChooseRandomWeather();
            ApplyWeatherSettings();
            lastWeatherChangeDay = Mathf.Floor(dayNightCycle.CurrentHour / 24f);
        }

        // Blend current weather tint with day/night color every frame
        BlendLightingWithWeather();
    }

    void ChooseRandomWeather()
    {
        float r = UnityEngine.Random.value;

        if (r < chanceClear)
            CurrentWeather = WeatherType.Clear;
        else if (r < chanceClear + chanceSunny)
            CurrentWeather = WeatherType.Sunny;
        else if (r < chanceClear + chanceSunny + chanceRainy)
            CurrentWeather = WeatherType.Rainy;
        else
            CurrentWeather = WeatherType.Windy;

        OnWeatherChanged(CurrentWeather);
    }

    void ApplyWeatherSettings()
    {
        if (rainParticles) rainParticles.Stop();
        if (windParticles) windParticles.Stop();
        if (sunnyParticles) sunnyParticles.Stop();

        switch (CurrentWeather)
        {
            case WeatherType.Rainy:
                if (rainParticles) rainParticles.Play();
                break;
            case WeatherType.Windy:
                if (windParticles) windParticles.Play();
                break;
            case WeatherType.Sunny:
                if (sunnyParticles) sunnyParticles.Play();
                break;
            default:
                break; // Clear = nothing
        }
    }

    void BlendLightingWithWeather()
    {
        if (!globalLight || !dayNightCycle || dayNightCycle.globalLight == null) return;

        // Base color from Day/Night
        Color baseColor = dayNightCycle.globalLight.color;
        float baseIntensity = dayNightCycle.globalLight.intensity;

        // Weather modifier
        Color tint = clearTint;
        float intensityMult = clearIntensity;

        switch (CurrentWeather)
        {
            case WeatherType.Clear:
                tint = clearTint;
                intensityMult = clearIntensity;
                break;
            case WeatherType.Sunny:
                tint = sunnyTint;
                intensityMult = sunnyIntensity;
                break;
            case WeatherType.Rainy:
                tint = rainyTint;
                intensityMult = rainyIntensity;
                break;
            case WeatherType.Windy:
                tint = windyTint;
                intensityMult = windyIntensity;
                break;
        }

        // Blend (slight color tint, scaled intensity)
        globalLight.color = Color.Lerp(baseColor, baseColor * tint, 0.5f);
        globalLight.intensity = baseIntensity * intensityMult;
    }

    public void SetWeather(WeatherType type, float lastChangeDay = -1f)
    {
        CurrentWeather = type;
        lastWeatherChangeDay = lastChangeDay;
        ApplyWeatherSettings();
        OnWeatherChanged(CurrentWeather);
    }

    public (WeatherType weather, float lastChangeDay) GetWeatherData()
    {
        return (CurrentWeather, lastWeatherChangeDay);
    }
}

public enum WeatherType
{
    Clear,
    Sunny,
    Rainy,
    Windy
}
