using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RubbleDatabase", menuName = "Data/Rubble Database")]
public class RubbleDatabase : ScriptableObject
{
    public static RubbleDatabase Instance { get; private set; }

    [Header("All Rubble Prefabs")]
    public List<GameObject> rubblePrefabs = new List<GameObject>();

    private Dictionary<string, GameObject> lookup = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        Instance = this;
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup.Clear();
        foreach (var prefab in rubblePrefabs)
        {
            if (prefab == null) continue;

            var rubble = prefab.GetComponent<CollectibleRubble>();
            if (rubble == null)
            {
                Debug.LogWarning($"RubbleDatabase: Prefab {prefab.name} has no CollectibleRubble component.");
                continue;
            }

            string id = prefab.name;
            if (!lookup.ContainsKey(id))
                lookup.Add(id, prefab);
        }
    }

    public GameObject GetRubblePrefab(string name)
    {
        if (lookup.TryGetValue(name, out var prefab))
            return prefab;
        else
        {
            Debug.LogWarning($"RubbleDatabase: No rubble prefab found for {name}");
            return null;
        }
    }
}
