using UnityEngine;
using System.Collections.Generic;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance;

    [System.Serializable]
    public class NPCData
    {
        public string id;
        public GameObject prefab;
    }

    [Header("NPC Prefabs")]
    public List<NPCData> npcPrefabs = new List<NPCData>();
    private Dictionary<string, GameObject> npcLookup = new Dictionary<string, GameObject>();

    [Header("Runtime NPCs")]
    private List<(string id, GameObject obj)> spawnedNPCs = new List<(string, GameObject)>();
    private (string id, GameObject obj)? player;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        foreach (var npc in npcPrefabs)
        {
            if (!npcLookup.ContainsKey(npc.id))
                npcLookup.Add(npc.id, npc.prefab);
        }
    }

    // --------------------------
    // SPAWNING
    // --------------------------
    public GameObject Spawn(string id, Vector3? position = null, Transform parent = null)
    {
        if (!npcLookup.ContainsKey(id))
        {
            Debug.LogWarning($"[NPCManager] No prefab found for id: {id}");
            return null;
        }

        Vector3 spawnPos = position ?? Vector3.zero;
        GameObject obj = Instantiate(npcLookup[id], spawnPos, Quaternion.identity, parent);

        if (id == "Player")
        {
            if (player?.obj != null) Destroy(player?.obj);
            player = (id, obj);
            obj.name = "Player";
        }
        else
        {
            spawnedNPCs.Add((id, obj));
        }

        return obj;
    }

    public GameObject SpawnAtPOI(string id, POIType poiType)
    {
        var poi = POIManager.GetRandomPOI(poiType);
        Vector3 spawnPos = poi != null ? poi.transform.position : Vector3.zero;

        if (poi == null)
            Debug.LogWarning($"[NPCManager] No POI found of type '{poiType}' — spawning at origin.");

        return Spawn(id, spawnPos);
    }

    // --------------------------
    // SAVE / LOAD
    // --------------------------
    public List<UniqueNPCSaveData> GetNPCSaveData()
    {
        List<UniqueNPCSaveData> data = new List<UniqueNPCSaveData>();

        foreach (var (id, obj) in spawnedNPCs)
        {
            if (obj == null) continue;

            data.Add(new UniqueNPCSaveData
            {
                npcID = id,
                position = obj.transform.position
            });
        }

        if (player?.obj != null)
        {
            data.Add(new UniqueNPCSaveData
            {
                npcID = player?.id,
                position = player?.obj.transform.position ?? Vector3.zero
            });
        }

        return data;
    }

    public void SpawnNPCsFromSave(List<UniqueNPCSaveData> data)
    {
        ClearAllNPCs();

        foreach (var d in data)
        {
            Spawn(d.npcID, d.position);
        }
    }

    public void ClearAllNPCs()
    {
        foreach (var (_, obj) in spawnedNPCs)
            if (obj != null) Destroy(obj);

        spawnedNPCs.Clear();

        if (player?.obj != null)
        {
            Destroy(player?.obj);
            player = null;
        }
    }
}
