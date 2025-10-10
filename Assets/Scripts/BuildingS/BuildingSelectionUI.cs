using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class BuildingSelectionUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;      // Prefab with Button + Image
    public Transform buttonContainer;    // Parent transform for buttons

    [Header("Buildings")]
    public List<BuildingBlueprint> buildingOptions;

    private BuildingManager buildingManager;

    void Awake()
    {
        buildingManager = FindFirstObjectByType<BuildingManager>();
        GenerateButtons();
    }

    public void AddBlueprint(BuildingBlueprint newBlueprint)
    {
        if (buildingOptions.Contains(newBlueprint))
        {
            Debug.Log($"[BuildingSelectionUI] Blueprint '{newBlueprint.name}' already unlocked.");
            return;
        }

        buildingOptions.Add(newBlueprint);
        Debug.Log($"[BuildingSelectionUI] Added new blueprint: {newBlueprint.name}");

        // Optionally regenerate buttons to make it appear immediately
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        GenerateButtons();
    }

    void GenerateButtons()
    {
        foreach (var blueprint in buildingOptions)
        {
            GameObject btnObj = Instantiate(buttonPrefab, buttonContainer);
            Button btn = btnObj.GetComponent<Button>();
            Image icon = btnObj.GetComponent<Image>();
            TextMeshProUGUI cost = btnObj.GetComponentInChildren<TextMeshProUGUI>();

            // Use the prefab sprite as the button icon (assumes prefab has a SpriteRenderer)
            if (icon != null)
            {
                SpriteRenderer sr = blueprint.tierPrefabs[0].GetComponentInChildren<SpriteRenderer>();
                if (sr != null) icon.sprite = sr.sprite;
            }
            if(cost != null)
            {
                cost.text = blueprint.tierCosts[0].goldCost.ToString() + " : " + blueprint.tierCosts[0].materialCost.ToString() + " : " + blueprint.tierCosts[0].manaCost.ToString();
            }

            btn.onClick.AddListener(() => SelectBuilding(blueprint));
        }
    }

    void SelectBuilding(BuildingBlueprint blueprint)
    {
        if (buildingManager != null)
        {
            buildingManager.BeginPlacement(blueprint);
        }
    }

    public List<string> GetUnlockedBlueprintNames()
    {
        List<string> names = new List<string>();
        foreach (var blueprint in buildingOptions)
        {
            if (blueprint != null)
                names.Add(blueprint.name);
        }

        Debug.Log($"[BuildingSelectionUI] Saving {names.Count} unlocked blueprints.");
        return names;
    }

    public void LoadUnlockedBlueprints(List<string> blueprintNames)
    {
        buildingOptions.Clear();

        foreach (var name in blueprintNames)
        {
            BuildingBlueprint bp = BuildingDatabase.Instance.GetBlueprintByName(name);
            if (bp != null)
            {
                buildingOptions.Add(bp);
            }
            else
            {
                Debug.LogWarning($"[BuildingSelectionUI] Could not find blueprint '{name}' in database.");
            }
        }

        // Rebuild UI
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        GenerateButtons();

        Debug.Log($"[BuildingSelectionUI] Loaded {buildingOptions.Count} blueprints.");
    }


}
