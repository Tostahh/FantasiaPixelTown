using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class BuildingManager : MonoBehaviour
{
    public static Action<Building> OnBuildingPlaced = delegate { };

    [Header("References")]
    public Tilemap walkableTilemap;
    public Tilemap obstacleTilemap;
    public Tile GrassTile;
    public Grid grid;

    [Header("Prefabs")]
    //public List<CollectibleRubble> rubblePrefabs; // assign all rubble types in Inspector

    [Header("Preview Settings")]
    public Color validColor = new Color(0f, 1f, 0f, 0.5f);
    public Color invalidColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("Building Data")]
    public BuildingBlueprint currentBlueprint;
    private GameObject previewObject;
    private SpriteRenderer previewRenderer;
    private Building buildingToMove;
    private Vector3 originalPosition;

    private InputSystem_Actions input;
    private Camera mainCam;

    [Header("Placement State")]
    public bool IsPlacingBuilding { get; private set; } = false;

    public List<Building> placedBuildings = new();
    public List<CollectibleRubble> placedRubble = new();

    private void Awake()
    {
        input = new InputSystem_Actions();
        mainCam = Camera.main;
    }

    private void OnEnable()
    {
        input.Enable();
        input.Player.Move.performed += OnPlaceAttempt;
    }

    private void OnDisable()
    {
        input.Player.Move.performed -= OnPlaceAttempt;
        input.Disable();
    }

    private void Update()
    {
        if (!IsPlacingBuilding || currentBlueprint == null) return;

        Vector3 mouseWorld = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseWorld.z = 0f;

        Vector3Int cellPos = grid.WorldToCell(mouseWorld);
        Vector3 bottomLeftWorld = grid.GetCellCenterWorld(cellPos);

        if (previewObject == null)
            CreatePreviewObject();

        previewObject.transform.position = bottomLeftWorld;

        bool valid = CanPlaceBuilding(cellPos) && CanAffordPlacement();
        if (previewRenderer != null)
            previewRenderer.color = valid ? validColor : invalidColor;
    }
    public void BeginPlacement(BuildingBlueprint blueprint)
    {
        if (blueprint == null)
        {
            Debug.LogWarning("[BuildingManager] BeginPlacement called with null blueprint.");
            return;
        }

        currentBlueprint = blueprint;

        if (previewObject != null)
            Destroy(previewObject);

        previewObject = Instantiate(currentBlueprint.tierPrefabs[0]);
        var ps = previewObject.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();

        previewRenderer = previewObject.GetComponentInChildren<SpriteRenderer>();
        IsPlacingBuilding = true;
    }
    private void CreatePreviewObject()
    {
        previewObject = Instantiate(currentBlueprint.tierPrefabs[0]);
        var prod = previewObject.GetComponent<BuildingProduction>();
        if (prod != null) prod.enabled = false;

        var ps = previewObject.GetComponentInChildren<ParticleSystem>();
        if (ps != null) ps.Stop();

        previewRenderer = previewObject.GetComponentInChildren<SpriteRenderer>();
    }

    private void OnPlaceAttempt(InputAction.CallbackContext ctx)
    {
        if (!IsPlacingBuilding || currentBlueprint == null || previewObject == null) return;

        Vector3Int cellPos = grid.WorldToCell(previewObject.transform.position - currentBlueprint.visualOffset);
        if (!CanPlaceBuilding(cellPos)) return;

        if (buildingToMove != null)
            FinalizeMove(cellPos);
        else
        {
            if (!CanAffordPlacement())
            {
                Debug.Log($"Not enough resources to place {currentBlueprint.buildingName}");
                return;
            }

            SpendPlacementResources();
            PlaceBuilding(cellPos);
        }
    }

    #region Resource Checks
    private bool CanAffordPlacement()
    {
        if (currentBlueprint?.tierCosts == null || currentBlueprint.tierCosts.Length == 0)
            return true;

        var cost = currentBlueprint.tierCosts[0];
        return ResourceManager.Instance.HasResources(cost.goldCost, cost.materialCost, cost.manaCost);
    }

    private void SpendPlacementResources()
    {
        if (currentBlueprint?.tierCosts == null || currentBlueprint.tierCosts.Length == 0)
            return;

        var cost = currentBlueprint.tierCosts[0];
        ResourceManager.Instance.RemoveGold(cost.goldCost);
        ResourceManager.Instance.RemoveMaterials(cost.materialCost);
        ResourceManager.Instance.RemoveMana(cost.manaCost);
    }
    #endregion

    private bool CanPlaceBuilding(Vector3Int originCell)
    {
        int w = currentBlueprint.width;
        int h = currentBlueprint.height;
        int p = currentBlueprint.pathPadding;

        for (int x = -p; x < w + p; x++)
        {
            for (int y = -p; y < h + p; y++)
            {
                Vector3Int checkPos = new(originCell.x + x, originCell.y + y, originCell.z);
                if (walkableTilemap.GetTile(checkPos) == null) return false;
                if (obstacleTilemap.HasTile(checkPos)) return false;
            }
        }

        return true;
    }

    private void PlaceBuilding(Vector3Int originCell)
    {
        Vector3 worldPos = grid.GetCellCenterWorld(originCell) + currentBlueprint.visualOffset;
        GameObject obj = Instantiate(currentBlueprint.tierPrefabs[0], worldPos, Quaternion.identity);

        Building b = obj.GetComponent<Building>() ?? obj.AddComponent<Building>();
        b.blueprint = currentBlueprint;
        b.LoadTier(0);
        placedBuildings.Add(b);

        ApplyTiles(originCell, currentBlueprint.width, currentBlueprint.height, currentBlueprint.pathPadding);

        OnBuildingPlaced(b);
        Destroy(previewObject);
        previewObject = null;
        currentBlueprint = null;
        IsPlacingBuilding = false;
    }

    private void ApplyTiles(Vector3Int origin, int w, int h, int padding)
    {
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Vector3Int tilePos = new(origin.x + x, origin.y + y, origin.z);
                walkableTilemap.SetTile(tilePos, null);
                obstacleTilemap.SetTile(tilePos, currentBlueprint.obstacleTile);
            }
        }

        for (int x = -padding; x < w + padding; x++)
        {
            for (int y = -padding; y < h + padding; y++)
            {
                if (x >= 0 && x < w && y >= 0 && y < h) continue;
                Vector3Int tilePos = new(origin.x + x, origin.y + y, origin.z);
                obstacleTilemap.SetTile(tilePos, null);
                walkableTilemap.SetTile(tilePos, currentBlueprint.pathTile);
            }
        }
    }

    #region Move Functions
    public void BeginMove(Building building)
    {
        if (building == null) return;

        buildingToMove = building;
        currentBlueprint = building.blueprint;
        originalPosition = building.transform.position;

        if (previewObject != null) Destroy(previewObject);
        previewObject = Instantiate(currentBlueprint.tierPrefabs[building.CurrentTier]);
        previewRenderer = previewObject.GetComponentInChildren<SpriteRenderer>();

        IsPlacingBuilding = true;
    }

    private void FinalizeMove(Vector3Int newOriginCell)
    {
        if (buildingToMove == null || currentBlueprint == null) return;

        Vector3 worldPos = grid.GetCellCenterWorld(newOriginCell) + currentBlueprint.visualOffset;

        ClearOldTiles(buildingToMove, grid.WorldToCell(originalPosition - currentBlueprint.visualOffset));

        buildingToMove.transform.position = worldPos;

        Destroy(previewObject);
        previewObject = null;
        currentBlueprint = null;
        buildingToMove = null;
        IsPlacingBuilding = false;
    }

    public void ClearOldTiles(Building building, Vector3Int originCell)
    {
        int w = building.blueprint.width;
        int h = building.blueprint.height;
        int p = building.blueprint.pathPadding;

        for (int x = -p; x < w + p; x++)
        {
            for (int y = -p; y < h + p; y++)
            {
                Vector3Int tilePos = new(originCell.x + x, originCell.y + y, originCell.z);
                obstacleTilemap.SetTile(tilePos, null);
                walkableTilemap.SetTile(tilePos, GrassTile);
            }
        }
    }
    #endregion

    #region Save/Load
    public List<BuildingSaveData> GetBuildingSaveData()
    {
        var data = new List<BuildingSaveData>();
        foreach (var b in placedBuildings)
        {
            if (b == null || b.blueprint == null) continue;

            data.Add(new BuildingSaveData
            {
                buildingID = b.blueprint.buildingName,
                tier = b.CurrentTier,
                position = grid.WorldToCell(b.transform.position - b.blueprint.visualOffset)
            });
        }
        return data;
    }

    public List<TileSaveData> GetTileSaveData()
    {
        var data = new List<TileSaveData>();
        BoundsInt bounds = walkableTilemap.cellBounds;

        foreach (var pos in bounds.allPositionsWithin)
        {
            TileBase walkable = walkableTilemap.GetTile(pos);
            TileBase obstacle = obstacleTilemap.GetTile(pos);

            if (walkable == null && obstacle == null) continue;

            data.Add(new TileSaveData
            {
                position = new Vector3Int(pos.x, pos.y, 0),
                walkableTile = walkable != null ? walkable.name : null,
                obstacleTile = obstacle != null ? obstacle.name : null
            });
        }
        return data;
    }

    public List<RubbleSaveData> GetRubbleSaveData()
    {
        var data = new List<RubbleSaveData>();
        foreach (var rubble in placedRubble)
        {
            if (rubble == null) continue;

            data.Add(new RubbleSaveData
            {
                prefabName = rubble.gameObject.name.Replace("(Clone)", "").Trim(),
                position = rubble.transform.position,
                goldReward = rubble.goldReward,
                materialReward = rubble.materialReward,
                manaReward = rubble.manaReward,
                collected = !rubble.gameObject.activeSelf
            });
        }
        return data;
    }

    public void SpawnBuildingsFromSave(List<BuildingSaveData> data)
    {
        ClearAllBuildings(); // safe to clear now
        foreach (var d in data)
        {
            var blueprint = BuildingDatabase.Instance.GetBlueprintByName(d.buildingID);
            if (blueprint == null) continue;

            Vector3 worldPos = grid.GetCellCenterWorld(d.position) + blueprint.visualOffset;
            GameObject obj = Instantiate(blueprint.tierPrefabs[d.tier], worldPos, Quaternion.identity);
            Building b = obj.GetComponent<Building>();
            b.blueprint = blueprint;
            b.LoadTier(d.tier);
            placedBuildings.Add(b);
        }
    }

    public void LoadTileData(List<TileSaveData> data)
    {
        if (data == null || data.Count == 0)
        {
            Debug.Log("No saved tile data found — keeping starting tilemap.");
            return;
        }

        walkableTilemap.ClearAllTiles();
        obstacleTilemap.ClearAllTiles();

        foreach (var t in data)
        {
            Vector3Int pos = new(t.position.x, t.position.y, 0);
            walkableTilemap.SetTile(pos, string.IsNullOrEmpty(t.walkableTile) ? null : TileDatabase.Instance.GetTileByName(t.walkableTile));
            obstacleTilemap.SetTile(pos, string.IsNullOrEmpty(t.obstacleTile) ? null : TileDatabase.Instance.GetTileByName(t.obstacleTile));
        }
    }

    public void SpawnRubbleFromSave(List<RubbleSaveData> data)
    {
        ClearAllRubble(); // clear first to prevent duplicates

        foreach (var d in data)
        {
            if (d.collected) continue;

            GameObject prefab = RubbleDatabase.Instance.GetRubblePrefab(d.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"[BuildingManager] No prefab found for rubble: {d.prefabName}");
                continue;
            }

            GameObject obj = Instantiate(prefab, d.position, Quaternion.identity);
            var rubble = obj.GetComponent<CollectibleRubble>();

            rubble.goldReward = d.goldReward;
            rubble.materialReward = d.materialReward;
            rubble.manaReward = d.manaReward;

            placedRubble.Add(rubble);
        }

        Debug.Log($"[BuildingManager] Loaded {placedRubble.Count} rubble objects from save.");
    }

    public void ClearAllBuildings()
    {
        foreach (var b in placedBuildings)
            if (b != null) Destroy(b.gameObject);
        placedBuildings.Clear();
    }

    public void ClearAllRubble()
    {
        // Only destroy rubble that still exists in the world (not already collected)
        for (int i = placedRubble.Count - 1; i >= 0; i--)
        {
            var r = placedRubble[i];
            if (r == null || !r.gameObject.activeSelf)
            {
                // Already collected or null, just remove from list
                placedRubble.RemoveAt(i);
                continue;
            }

            Destroy(r.gameObject);
            placedRubble.RemoveAt(i);
        }
    }
    #endregion

    #region Initialization
    public void InitializeStartingMap()
    {
        // Add pre-placed buildings
        Building[] prePlacedBuildings = FindObjectsOfType<Building>();
        foreach (var b in prePlacedBuildings)
            if (!placedBuildings.Contains(b))
                placedBuildings.Add(b);

        // Add pre-placed rubble
        CollectibleRubble[] rubbleObjects = FindObjectsOfType<CollectibleRubble>();
        foreach (var r in rubbleObjects)
            if (!placedRubble.Contains(r))
                placedRubble.Add(r);

        Debug.Log($"[BuildingManager] Initialized map: {prePlacedBuildings.Length} buildings, {rubbleObjects.Length} rubble objects.");
    }

    #endregion
}
