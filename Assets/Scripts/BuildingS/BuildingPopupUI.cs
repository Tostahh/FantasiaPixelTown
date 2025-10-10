using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuildingPopupUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject panel;
    public Button upgradeButton;
    public Button moveButton;
    public Button removeButton;
    public Button closeButton;

    [Header("Info Texts")]
    public TMP_Text nameText;
    public TMP_Text tierText;
    public TMP_Text UpgradeGoldCost;
    public TMP_Text UpgradeMaterialCost;
    public TMP_Text UpgradeManaCost;
    public TMP_Text npcCountText;

    public TMP_Text Production;
    public TMP_Text ProductionRate;

    [Header("References")]
    public BuildingManager buildingManager;

    [HideInInspector]
    public Building currentBuilding;

    private void Awake()
    {
        buildingManager = FindAnyObjectByType<BuildingManager>();

        if (panel != null)
            panel.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgrade);

        if (moveButton != null)
            moveButton.onClick.AddListener(OnMove);

        if (removeButton != null)
            removeButton.onClick.AddListener(OnRemove);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    public void Show(Building building)
    {
        currentBuilding = building;
        if (panel != null)
            panel.SetActive(true);

        UpdateInfo();
    }

    private void UpdateInfo()
    {
        if (currentBuilding == null || currentBuilding.blueprint == null)
        {
            if (nameText) nameText.text = "No Building Selected";
            if (tierText) tierText.text = "-";
            if (npcCountText) npcCountText.text = "-";
            if (UpgradeGoldCost) UpgradeGoldCost.text = "-";
            if (UpgradeMaterialCost) UpgradeMaterialCost.text = "-";
            if (UpgradeManaCost) UpgradeManaCost.text = "-";
            if (Production) Production.text = "-";
            if (ProductionRate) ProductionRate.text = "-"; 
            removeButton.interactable = false;
            moveButton.interactable = false;
            return;
        }

        // Update basic info
        if (nameText) nameText.text = currentBuilding.blueprint.buildingName;
        if (tierText) tierText.text = $"Building Tier: {currentBuilding.CurrentTier}";
        if (npcCountText) npcCountText.text = $"NPC Amount: {currentBuilding.spawnedNPCs.Count}";
        if (UpgradeGoldCost) UpgradeGoldCost.text = "Gold Upgrade Cost: " + currentBuilding.blueprint.tierCosts[currentBuilding.CurrentTier + 1].goldCost.ToString();
        if (UpgradeMaterialCost) UpgradeMaterialCost.text = "Material Upgrade Cost: " + currentBuilding.blueprint.tierCosts[currentBuilding.CurrentTier + 1].materialCost.ToString();
        if (UpgradeManaCost) UpgradeManaCost.text = "Mana Upgrade Cost: " + currentBuilding.blueprint.tierCosts[currentBuilding.CurrentTier + 1].manaCost.ToString();
        if (Production) Production.text = "Production Type: " + currentBuilding.GetComponent<BuildingProduction>().resourceType.ToString();
        if (ProductionRate) ProductionRate.text = "Production Rate: " + currentBuilding.GetComponent<BuildingProduction>().productionPerTier[currentBuilding.CurrentTier] + " / " + currentBuilding.GetComponent<BuildingProduction>().productionInterval + " Sec";
        // Determine remove button state
        bool canRemove = currentBuilding.blueprint.buildingName != "Shrine" &&
                         TutorialManager.Instance.currentStep >= 3;

        removeButton.interactable = canRemove;

        // Determine move button state
        bool canMove = TutorialManager.Instance.currentStep >= 3;
        moveButton.interactable = canMove;
    }


    private void OnUpgrade()
    {
        if (currentBuilding != null)
        {
            currentBuilding.Upgrade();
            UpdateInfo(); // refresh UI after upgrading
        }
    }

    private void OnMove()
    {
        if (currentBuilding != null && buildingManager != null)
        {
            buildingManager.BeginMove(currentBuilding);
            Close();
        }
    }

    private void OnRemove()
    {
        if (currentBuilding != null)
            currentBuilding.Remove();

        Close();
    }

    private void Close()
    {
        if (panel != null)
            panel.SetActive(false);

        currentBuilding = null;
    }
}
