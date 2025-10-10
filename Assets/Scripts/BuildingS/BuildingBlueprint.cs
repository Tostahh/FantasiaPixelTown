using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class TierNPCSetup
{
    public GameObject[] npcPrefabs;
    public int[] npcCounts;
}

[System.Serializable]
public class TierResourceCost
{
    public int goldCost;
    public int materialCost;
    public int manaCost;
}

[CreateAssetMenu(menuName = "Buildings/Blueprint")]
public class BuildingBlueprint : ScriptableObject
{
    [Header("Tier Prefabs")]
    public GameObject[] tierPrefabs; // index = tier level

    public string buildingName;
    public int width = 1;
    public int height = 1;
    public TileBase obstacleTile;
    public TileBase pathTile;
    public int pathPadding = 1;
    public Vector3 visualOffset;

    [Header("NPCs")]
    public TierNPCSetup[] npcPrefabsByTier;

    [Header("NPC Home Position")]
    public Vector3 homeOffset = Vector3.zero;

    [Header("Special")]
    public bool isShrine = false;

    [Header("Resource Costs Per Tier")]
    public TierResourceCost[] tierCosts; // index matches tierPrefabs
}
