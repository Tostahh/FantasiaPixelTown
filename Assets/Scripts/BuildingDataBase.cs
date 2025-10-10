using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingDatabase", menuName = "Data/Building Database")]
public class BuildingDatabase : ScriptableObject
{
    public static BuildingDatabase Instance { get; private set; }

    [Header("All Building Blueprints")]
    public List<BuildingBlueprint> buildingBlueprints = new List<BuildingBlueprint>();

    private Dictionary<string, BuildingBlueprint> lookup = new Dictionary<string, BuildingBlueprint>();

    private void OnEnable()
    {
        Instance = this;
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup.Clear();
        foreach (var b in buildingBlueprints)
        {
            if (b != null && !lookup.ContainsKey(b.buildingName))
                lookup.Add(b.buildingName, b);
        }
    }

    public BuildingBlueprint GetBlueprintByName(string name)
    {
        if (lookup.TryGetValue(name, out var blueprint))
            return blueprint;
        else
            Debug.LogWarning($"BuildingDatabase: No blueprint found for {name}");
        return null;
    }
}
