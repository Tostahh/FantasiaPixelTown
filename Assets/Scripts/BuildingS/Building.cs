using UnityEngine;
using System.Collections.Generic;
using System;

public class Building : MonoBehaviour
{
    public static Action<Building> OnBuildingUpgraded = delegate { };

    public BuildingBlueprint blueprint;
    public int CurrentTier { get; private set; } = 0;

    private GameObject currentPrefabInstance;
    [HideInInspector] public List<GameObject> spawnedNPCs = new List<GameObject>();

    public Vector3 HomePosition => transform.position + blueprint.homeOffset;

    #region Resource Logic
    public bool CanAffordPlacement()
    {
        if (blueprint.tierCosts == null || blueprint.tierCosts.Length == 0)
            return true;

        TierResourceCost cost = blueprint.tierCosts[0];
        return ResourceManager.Instance.HasResources(cost.goldCost, cost.materialCost, cost.manaCost);
    }

    public bool SpendPlacementCost()
    {
        if (blueprint.tierCosts == null || blueprint.tierCosts.Length == 0)
            return true;

        TierResourceCost cost = blueprint.tierCosts[0];
        if (!ResourceManager.Instance.HasResources(cost.goldCost, cost.materialCost, cost.manaCost))
            return false;

        ResourceManager.Instance.RemoveGold(cost.goldCost);
        ResourceManager.Instance.RemoveMaterials(cost.materialCost);
        ResourceManager.Instance.RemoveMana(cost.manaCost);
        return true;
    }

    public bool CanAffordUpgrade()
    {
        int nextTier = CurrentTier + 1;
        if (blueprint.tierCosts == null || nextTier >= blueprint.tierCosts.Length)
            return false;

        TierResourceCost cost = blueprint.tierCosts[nextTier];
        return ResourceManager.Instance.HasResources(cost.goldCost, cost.materialCost, cost.manaCost);
    }

    public bool SpendUpgradeCost()
    {
        int nextTier = CurrentTier + 1;
        if (blueprint.tierCosts == null || nextTier >= blueprint.tierCosts.Length)
            return false;

        TierResourceCost cost = blueprint.tierCosts[nextTier];
        if (!ResourceManager.Instance.HasResources(cost.goldCost, cost.materialCost, cost.manaCost))
            return false;

        ResourceManager.Instance.RemoveGold(cost.goldCost);
        ResourceManager.Instance.RemoveMaterials(cost.materialCost);
        ResourceManager.Instance.RemoveMana(cost.manaCost);
        return true;
    }
    #endregion

    public void Upgrade()
    {
        int maxTier = blueprint.isShrine ? 3 : blueprint.tierPrefabs.Length - 1;
        if (CurrentTier >= maxTier)
            return;

        // Check resources before upgrading
        if (!CanAffordUpgrade())
        {
            Debug.Log($"Not enough resources to upgrade {blueprint.buildingName}.");
            return;
        }

        // Spend resources
        if (!SpendUpgradeCost())
        {
            Debug.Log($"Failed to spend resources for upgrading {blueprint.buildingName}.");
            return;
        }

        OnBuildingUpgraded(this);

        CurrentTier++;
        LoadTier(CurrentTier);
    }

    public void UpgradeWOCost()
    {
        int maxTier = blueprint.isShrine ? 3 : blueprint.tierPrefabs.Length - 1;
        if (CurrentTier >= maxTier)
            return;

        OnBuildingUpgraded(this);

        CurrentTier++;
        LoadTier(CurrentTier);
    }

    public void LoadTier(int tier)
    {
        if (tier < 0 || tier >= blueprint.tierPrefabs.Length) return;

        BuildingManager manager = FindFirstObjectByType<BuildingManager>();

        // --- Store data before replacing ---
        Vector3 worldPos = transform.position;
        Quaternion rotation = transform.rotation;
        Transform parent = transform.parent;

        // Destroy old NPCs
        foreach (GameObject npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();

        // --- Spawn new prefab (replacement) ---
        GameObject newBuildingObj = Instantiate(blueprint.tierPrefabs[tier], worldPos, rotation, parent);
        Building newBuilding = newBuildingObj.GetComponent<Building>();
        if (newBuilding == null)
            newBuilding = newBuildingObj.AddComponent<Building>();

        // Copy important data
        newBuilding.blueprint = blueprint;
        newBuilding.CurrentTier = tier;

        // Transfer NPCs or other runtime data if needed
        newBuilding.spawnedNPCs = new List<GameObject>();

        // Spawn tier-specific NPCs
        newBuilding.SpawnTierNPCs(tier);

        // --- Update BuildingManager reference ---
        if (manager != null)
        {
            int index = manager.placedBuildings.IndexOf(this);
            if (index >= 0)
                manager.placedBuildings[index] = newBuilding;
            else
                manager.placedBuildings.Add(newBuilding);
        }

        // Destroy old building object
        Destroy(gameObject);
    }

    private void SpawnTierNPCs(int tier)
    {
        if (FindFirstObjectByType<BuildingPopupUI>().panel.activeSelf)
        {
            FindFirstObjectByType<BuildingPopupUI>().currentBuilding = this;
        }

        if (blueprint.npcPrefabsByTier == null || tier >= blueprint.npcPrefabsByTier.Length)
            return;

        TierNPCSetup setup = blueprint.npcPrefabsByTier[tier];
        if (setup == null) return;

        for (int i = 0; i < setup.npcPrefabs.Length; i++)
        {
            GameObject npcPrefab = setup.npcPrefabs[i];
            int count = setup.npcCounts[i];

            for (int j = 0; j < count; j++)
            {
                Vector3 spawnPos = HomePosition;
                GameObject npc = Instantiate(npcPrefab, spawnPos, Quaternion.identity);
                spawnedNPCs.Add(npc);

                NPC npcComp = npc.GetComponent<NPC>();
                if (npcComp != null)
                    npcComp.homePosition = transform;
            }
        }
    }

    public void Remove()
    {
        // Refund resources for the current tier
        if (blueprint != null && blueprint.tierCosts != null && CurrentTier < blueprint.tierCosts.Length)
        {
            TierResourceCost cost = blueprint.tierCosts[CurrentTier];
            ResourceManager.Instance.AddGold(cost.goldCost);
            ResourceManager.Instance.AddMaterials(cost.materialCost);
            ResourceManager.Instance.AddMana(cost.manaCost);
        }

        // Clear NPCs
        foreach (GameObject npc in spawnedNPCs)
            if (npc != null) Destroy(npc);
        spawnedNPCs.Clear();

        // Clear tiles
        BuildingManager manager = FindFirstObjectByType<BuildingManager>();
        if (manager != null)
        {
            Vector3Int originCell = manager.grid.WorldToCell(transform.position - blueprint.visualOffset);
            manager.ClearOldTiles(this, originCell);
        }

        // Destroy prefab instance
        if (currentPrefabInstance != null)
            Destroy(currentPrefabInstance);

        // Destroy the building object
        Destroy(this.gameObject);
    }

}
