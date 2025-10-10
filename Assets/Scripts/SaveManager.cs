using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Central Save/Load manager for Fantasia Pixel Town.
/// Attach this to a persistent GameObject (like GameManager).
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Scene References")]
    public BuildingManager buildingManager;
    public NPCManager npcManager;
    public DayNightCycle dayNightCycle;
    public StoryEventManager storyManager;
    public TutorialManager tutorialManager;
    public ResourceManager resourceManager;

    public RubbleDatabase RDB;
    public BuildingDatabase BDB;
    public TileDatabase TDB;

    public GameSaveData CurrentSave { get; private set; }

    private void Awake()
    {
        buildingManager = FindFirstObjectByType<BuildingManager>();
        npcManager = FindFirstObjectByType<NPCManager>();
        dayNightCycle = FindFirstObjectByType<DayNightCycle>();
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
    }

    private void Start()
    {
        buildingManager.InitializeStartingMap();
        LoadGame();
    }

    #region ===== SAVE LOGIC =====
    public void SaveGame()
    {
        if (CurrentSave == null)
            CurrentSave = new GameSaveData();

        // --- WORLD ---
        var world = CurrentSave.worldData;
        world.timeOfDay = dayNightCycle.CurrentHour;
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

        SaveSystem.SaveGame(CurrentSave);
    }
    #endregion

    #region ===== LOAD LOGIC =====
    public void LoadGame()
    {
        CurrentSave = SaveSystem.LoadGame();

        if (tutorialManager != null)
        {
            tutorialManager.BeginLoad();
        }

            // --- WORLD ---
            if (dayNightCycle != null)
        {
            dayNightCycle.SetTime(CurrentSave.worldData.timeOfDay);
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
    }
    #endregion

    public void DeleteSave()
    {
        SaveSystem.DeleteSave();
        CurrentSave = new GameSaveData();
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
