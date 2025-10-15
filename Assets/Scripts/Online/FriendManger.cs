using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FriendManager : MonoBehaviour
{
    public static FriendManager Instance { get; private set; }
    public PlayerProfile Profile { get; private set; }

    private string savePath;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            savePath = Path.Combine(Application.persistentDataPath, "PlayerProfile.json");
            LoadProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // -----------------------
    // PROFILE CREATION / LOAD
    // -----------------------
    private void LoadProfile()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Profile = JsonUtility.FromJson<PlayerProfile>(json);
            Debug.Log($"[FriendManager] Loaded profile for {Profile.playerName} ({Profile.friendCode})");
        }
        else
        {
            CreateNewProfile();
        }
    }

    private void CreateNewProfile()
    {
        Profile = new PlayerProfile
        {
            playerName = $"Player_{Random.Range(1000, 9999)}",
            friendCode = FriendCodeUtility.GenerateFriendCode(),
            friends = new List<FriendData>()
        };
        SaveProfile();
        Debug.Log($"[FriendManager] Created new profile: {Profile.playerName} ({Profile.friendCode})");
    }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(Profile, true);
        File.WriteAllText(savePath, json);
    }

    // -----------------------
    // FRIEND LIST MANAGEMENT
    // -----------------------
    public bool AddFriend(string friendName, string friendCode)
    {
        if (Profile.friends.Exists(f => f.friendCode == friendCode))
        {
            Debug.LogWarning("[FriendManager] Friend already in list!");
            return false;
        }

        Profile.friends.Add(new FriendData(friendName, friendCode));
        SaveProfile();
        Debug.Log($"[FriendManager] Added friend: {friendName} ({friendCode})");
        return true;
    }

    public void RemoveFriend(string friendCode)
    {
        Profile.friends.RemoveAll(f => f.friendCode == friendCode);
        SaveProfile();
        Debug.Log($"[FriendManager] Removed friend with code: {friendCode}");
    }

    public List<FriendData> GetFriends() => Profile.friends;
}
