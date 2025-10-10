using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

            // Use the prefab sprite as the button icon (assumes prefab has a SpriteRenderer)
            if (icon != null)
            {
                SpriteRenderer sr = blueprint.tierPrefabs[0].GetComponentInChildren<SpriteRenderer>();
                if (sr != null) icon.sprite = sr.sprite;
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
}
