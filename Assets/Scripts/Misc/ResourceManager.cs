using UnityEngine;
using System;

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    [Header("Resources")]
    public int gold = 0;
    public int materials = 0;
    public int mana = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region Resource Modification
    public void AddGold(int amount)
    {
        gold += amount;
    }

    public bool RemoveGold(int amount)
    {
        if (gold < amount) return false;
        gold -= amount;
        return true;
    }

    public void AddMaterials(int amount)
    {
        materials += amount;
    }

    public bool RemoveMaterials(int amount)
    {
        if (materials < amount) return false;
        materials -= amount;
        return true;
    }

    public void AddMana(int amount)
    {
        mana += amount;
    }

    public bool RemoveMana(int amount)
    {
        if (mana < amount) return false;
        mana -= amount;
        return true;
    }
    #endregion

    #region Resource Checks
    public bool HasGold(int amount) => gold >= amount;
    public bool HasMaterials(int amount) => materials >= amount;
    public bool HasMana(int amount) => mana >= amount;

    public bool HasResources(int goldAmount, int materialsAmount, int manaAmount)
    {
        return HasGold(goldAmount) && HasMaterials(materialsAmount) && HasMana(manaAmount);
    }
    #endregion
}
