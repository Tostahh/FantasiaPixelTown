using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public WorldData worldData = new WorldData();
    public ItemSaveData itemSaveData = new ItemSaveData();
    public NPCData npcData = new NPCData();
    public ProgressData progressData = new ProgressData();
    public EconomyData economyData = new EconomyData();
    public SettingsData settingsData = new SettingsData();

    public string saveVersion = "1.0.0";
}

#region ===== WORLD DATA =====
[Serializable]
public class WorldData
{
    public float timeOfDay = 6;
    public string currentWeather = "Clear";
    public float lastWeatherChangeDay = 0f;

    public List<BuildingSaveData> buildings = new List<BuildingSaveData>();
    public List<TileSaveData> tilemap = new List<TileSaveData>();
    public List<RubbleSaveData> rubbles = new List<RubbleSaveData>();
}

[Serializable]
public class BuildingSaveData
{
    public string buildingID;
    public Vector3Int position;  // <- changed from Vector2Int
    public int tier = 0;
}
[Serializable]
public class RubbleSaveData
{
    public string prefabName;
    public Vector3 position;
    public int goldReward;
    public int materialReward;
    public int manaReward;
    public bool collected;
}
[Serializable]
public class TileSaveData
{
    public Vector3Int position;  // <- changed from Vector2Int
    public string walkableTile;  // updated naming
    public string obstacleTile;
}
#endregion

#region ===== PLAYER & NPC DATA =====

[Serializable]
public class ItemSaveData
{
    public string itemID;
    public int quantity = 0;
}

[Serializable]
public class NPCData
{
    public List<UniqueNPCSaveData> uniqueNPCs = new List<UniqueNPCSaveData>();
}

[Serializable]
public class UniqueNPCSaveData
{
    public string npcID;              // e.g., "Architect", "Mayor"
    public Vector2 position;
}
#endregion

#region ===== PROGRESSION DATA =====
[Serializable]
public class ProgressData
{
    public int currentChapter = 1;
    public int currentTutorialStep = 0;
    public List<string> completedEventIDs = new List<string>();
    public List<string> unlockedBlueprints = new List<string>();
}
#endregion

#region ===== ECONOMY DATA =====
[Serializable]
public class EconomyData
{
    public int gold = 0;
    public int materials = 0;
    public int mana = 0;
}
#endregion

#region ===== SETTINGS DATA =====
[Serializable]
public class SettingsData
{
    public float musicVolume = 1f;
    public float sfxVolume = 1f;
    public float cameraZoom = 1f;
}
#endregion

#region ===== SAVE SYSTEM =====
public static class SaveSystem
{
    private static string GetSlotPath(int slot)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot{slot}.json");
    }

    public static void SaveGame(GameSaveData data, int slot)
    {
        try
        {
            string path = GetSlotPath(slot);
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(path, json);
            Debug.Log($"Game saved to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game (slot {slot}): {e}");
        }
    }

    public static GameSaveData LoadGame(int slot)
    {
        try
        {
            string path = GetSlotPath(slot);
            if (!File.Exists(path))
            {
                Debug.Log($"No save file found in slot {slot} — creating new data...");
                return new GameSaveData();
            }

            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game (slot {slot}): {e}");
            return new GameSaveData();
        }
    }

    public static void DeleteSave(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Save slot {slot} deleted.");
        }
    }

    public static bool SaveExists(int slot)
    {
        return File.Exists(GetSlotPath(slot));
    }
}

#endregion
