using UnityEngine;

public class ChapterProgressionManager : MonoBehaviour
{
    public static ChapterProgressionManager Instance;

    public Animator ChapterFlash;

    [Header("Chapter 1 Settings")]
    public string innName = "Inn";
    public string blacksmithName = "Blacksmith";

    [Header("Chapter 2 Settings")]
    public string TownName;
    public string houseName = "House";
    public string guardpostName = "GuardPost";
    public string MarketName = "Market";
    public int HousesBuilt = 0;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnGameLoaded()
    {
        // Only play Chapter 1 start if the "PlayerEnters" event hasn't already completed
        var playerEntersEvent = StoryEventManager.Instance.events.Find(e => e.eventID == "PlayerEnters");

        if (StoryEventManager.Instance.ChapterNumber == 1 && playerEntersEvent != null && !playerEntersEvent.completed)
        {
            Chapter1Start();
            StoryEventManager.Instance.Trigger("PlayerEnters");
        }
    }
    private void OnEnable()
    {
        // Subscribe using delegate
        Building.OnBuildingUpgraded += HandleBuildingUpgrade;
        BuildingManager.OnBuildingPlaced += HandleBuildingPlaced;
        StoryEventManager.Instance.OnEventCompleted += HandleStoryEventCompleted;
        StoryEventManager.OnChapterAdvance += CompleteChapter1;
        StoryEventManager.OnChapterAdvance += CompleteChapter2;

        SaveManager.GameLoaded += OnGameLoaded;
    }

    private void OnDisable()
    {
        Building.OnBuildingUpgraded -= HandleBuildingUpgrade;
        BuildingManager.OnBuildingPlaced -= HandleBuildingPlaced;
        StoryEventManager.Instance.OnEventCompleted -= HandleStoryEventCompleted;
        StoryEventManager.OnChapterAdvance -= CompleteChapter1;
        StoryEventManager.OnChapterAdvance -= CompleteChapter2;

        SaveManager.GameLoaded -= OnGameLoaded;
    }

    private void HandleBuildingUpgrade(Building building)
    {
        if (building.blueprint.buildingName == innName)
            StoryEventManager.Instance.Trigger("InnRebuilt");
    }

    private void HandleBuildingPlaced(Building building)
    {
        if (building.blueprint.buildingName == blacksmithName)
            StoryEventManager.Instance.Trigger("BlackSmithRebuilt");

        if (building.blueprint.buildingName == houseName)
            HousesBuilt++;

        if (building.blueprint.buildingName == guardpostName)
        {
            StoryEventManager.Instance.Trigger("LetsGoShopping");
        }

        if (building.blueprint.buildingName == MarketName)
        {
            StoryEventManager.Instance.Trigger("Reconstruction");
        }

        if(HousesBuilt >= 5)
        {
            StoryEventManager.Instance.Trigger("TheGuardsReturn");
        }
    }

    private void HandleStoryEventCompleted(string eventID)
    {
        switch (eventID)
        {
            case "InnRebuilt":
                StoryEventManager.Instance.Trigger("ArchitectArrival");
                break;

            case "BlackSmithRebuilt":
                StoryEventManager.Instance.Trigger("ShrineUpgrade");
                break;
            case "ShrineUpgrade":
                StoryEventManager.Instance.Trigger("TalkOfTheTown");
                break;
            case "Reconstruction":
                StoryEventManager.Instance.Trigger("EndChapter2");
                break;
            case "EndChapter2":
                StoryEventManager.Instance.Trigger("StartChapter3");
                break;
            default:
                break;
        }
    }

    private void Chapter1Start()
    {
        if (!StoryEventManager.Instance.events[0].completed)
        {
            ChapterFlash.SetTrigger("PlayC1");
        }
    }

    private void CompleteChapter1(int ChapterNumb)
    {
        if (ChapterNumb == 2)
        {
            Debug.Log("[ChapterProgressionManager] Chapter 1 Complete! Unlocking next chapter...");

            ChapterFlash.SetTrigger("PlayC2");
        }
    }

    private void CompleteChapter2(int ChapterNumb)
    {
        if(ChapterNumb == 3)
        {
            Debug.Log("[ChapterProgressionManager] Chapter 2 Complete! Unlocking next chapter...");

            ChapterFlash.SetTrigger("PlayC3");
        }
    }
}
