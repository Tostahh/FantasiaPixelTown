using UnityEngine;

public class ChapterProgressionManager : MonoBehaviour
{
    public static ChapterProgressionManager Instance;

    public Animator ChapterFlash;

    [Header("Chapter 1 Settings")]
    public string innName = "Inn";
    public string blacksmithName = "Blacksmith";

    [Header("Chapter 2 Settings")]
    public string b;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        StoryEventManager.OnChapterAdvance += CompleteChapter1;
    }

    private void Start()
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
    }

    private void OnDisable()
    {
        Building.OnBuildingUpgraded -= HandleBuildingUpgrade;
        BuildingManager.OnBuildingPlaced += HandleBuildingPlaced;
        StoryEventManager.Instance.OnEventCompleted -= HandleStoryEventCompleted;
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
}
