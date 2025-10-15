using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TileDatabase", menuName = "Data/Tile Database")]
public class TileDatabase : ScriptableObject
{
    public static TileDatabase Instance { get; private set; }

    [Header("All Game Tiles")]
    public List<TileBase> tiles = new List<TileBase>();

    private Dictionary<string, TileBase> lookup = new Dictionary<string, TileBase>();

    private void OnEnable()
    {
        Instance = this;
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup.Clear();
        foreach (var t in tiles)
        {
            if (t != null && !lookup.ContainsKey(t.name))
                lookup.Add(t.name, t);
        }
    }

    public TileBase GetTileByName(string name)
    {
        if (lookup.TryGetValue(name, out var tile))
            return tile;
        else
            Debug.LogWarning($"TileDatabase: No tile found for {name}");
        return null;
    }
}
