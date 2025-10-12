using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

/// <summary>
/// Central Save/Load manager for Fantasia Pixel Town.
/// Attach this to a persistent GameObject (like GameManager).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static Action GameLoaded = delegate { };

    public static SaveManager Instance;
    public int activeSlot = 0;

    [Header("Scene References")]
    public BuildingManager buildingManager;
    public NPCManager npcManager;
    public DayNightCycle dayNightCycle;
    public WeatherSystem weatherSystem;
    public StoryEventManager storyManager;
    public TutorialManager tutorialManager;
    public ResourceManager resourceManager;

    public RubbleDatabase RDB;
    public BuildingDatabase BDB;
    public TileDatabase TDB;

    public GameSaveData CurrentSave { get; set; }

    private void Awake()
    {
        buildingManager = FindFirstObjectByType<BuildingManager>();
        npcManager = FindFirstObjectByType<NPCManager>();
        dayNightCycle = FindFirstObjectByType<DayNightCycle>();
        weatherSystem = FindFirstObjectByType<WeatherSystem>();
        storyManager = FindFirstObjectByType<StoryEventManager>();
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        resourceManager = FindFirstObjectByType<ResourceManager>();

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        buildingManager.InitializeStartingMap();
    }

    #region ===== SAVE LOGIC =====
    public void SaveGame()
    {
        if (CurrentSave == null)
            CurrentSave = new GameSaveData();

        if (activeSlot < 1 || activeSlot > 3)
        {
            return;
        }


        // --- WORLD ---
        var world = CurrentSave.worldData;
        world.timeOfDay = dayNightCycle.CurrentHour;
        var (weatherType, lastChangeDay) = weatherSystem.GetWeatherData();
        world.currentWeather = weatherType.ToString();
        world.lastWeatherChangeDay = lastChangeDay;
        world.buildings = buildingManager.GetBuildingSaveData();
        world.rubbles = buildingManager.GetRubbleSaveData();
        world.tilemap = buildingManager.GetTileSaveData();

        // --- NPCs ---
        var npcData = CurrentSave.npcData;
        npcData.uniqueNPCs = npcManager.GetNPCSaveData();

        // --- PROGRESS ---
        var progress = CurrentSave.progressData;
        progress.currentChapter = storyManager.ChapterNumber;
        progress.completedEventIDs = storyManager.GetCompletedEventIDs();
        progress.currentTutorialStep = tutorialManager.currentStep;
        progress.unlockedBlueprints = FindFirstObjectByType<BuildingSelectionUI>().GetUnlockedBlueprintNames();

        // --- ECONOMY ---
        var economy = CurrentSave.economyData;
        economy.gold = resourceManager.gold;
        economy.materials = resourceManager.materials;
        economy.mana = resourceManager.mana;

        SaveSystem.SaveGame(CurrentSave, activeSlot);
    }
    #endregion

    #region ===== LOAD LOGIC =====
    public void LoadGame(int slot)
    {
        activeSlot = slot;
        CurrentSave = SaveSystem.LoadGame(activeSlot);

        if (tutorialManager != null)
        {
            tutorialManager.BeginLoad();
        }

        // --- WORLD ---
        if (dayNightCycle != null)
        {
            dayNightCycle.SetTime(CurrentSave.worldData.timeOfDay);
        }

        if (weatherSystem != null)
        {
            if (Enum.TryParse(CurrentSave.worldData.currentWeather, out WeatherType savedWeather))
            {
                float lastChangeDay = CurrentSave.worldData.lastWeatherChangeDay;
                weatherSystem.SetWeather(savedWeather, lastChangeDay);
            }
            else
            {
                weatherSystem.SetWeather(WeatherType.Clear);
            }
        }

        if (buildingManager != null)
        {
            bool hasSavedBuildings = CurrentSave.worldData.buildings != null && CurrentSave.worldData.buildings.Count > 0;
            if (hasSavedBuildings)
            {
                buildingManager.ClearAllBuildings();
                buildingManager.SpawnBuildingsFromSave(CurrentSave.worldData.buildings);
            }

            bool hasSavedRubble = CurrentSave.worldData.rubbles != null && CurrentSave.worldData.rubbles.Count > 0;
            if (hasSavedRubble)
            {
                buildingManager.ClearAllRubble();
                buildingManager.SpawnRubbleFromSave(CurrentSave.worldData.rubbles);
            }

            // Always load tile data
            buildingManager.LoadTileData(CurrentSave.worldData.tilemap);

        }

        // --- NPCs ---
        if (npcManager != null)
        {
            npcManager.ClearAllNPCs();
            npcManager.SpawnNPCsFromSave(CurrentSave.npcData.uniqueNPCs);
        }

        // --- PROGRESS ---
        if (storyManager != null)
        {
            storyManager.ChapterNumber = CurrentSave.progressData.currentChapter;
            storyManager.SetCompletedEvents(CurrentSave.progressData.completedEventIDs);
            storyManager.RestorePendingTriggers();
        }

        if (tutorialManager != null)
        {
            tutorialManager.SetStep(CurrentSave.progressData.currentTutorialStep);
            tutorialManager.EndLoad();   // now safe to update arrow visuals
        }

        FindFirstObjectByType<BuildingSelectionUI>().LoadUnlockedBlueprints(CurrentSave.progressData.unlockedBlueprints);

        // --- ECONOMY ---
        if (resourceManager != null)
        {
            resourceManager.gold = CurrentSave.economyData.gold;
            resourceManager.materials = CurrentSave.economyData.materials;
            resourceManager.mana = CurrentSave.economyData.mana;
        }

        Debug.Log("Game loaded successfully!");

        StartCoroutine(NotifyGameLoaded());
    }

    private IEnumerator NotifyGameLoaded()
    {
        yield return null; // wait a frame
        GameLoaded();
    }
    #endregion

    public void DeleteSave(int slot)
    {
        SaveSystem.DeleteSave(slot);
        if (activeSlot == slot)
            CurrentSave = new GameSaveData();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
