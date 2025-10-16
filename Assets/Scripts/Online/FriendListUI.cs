using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendListUI : MonoBehaviour
{
    [Header("Player Info")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text friendCodeText;
    [SerializeField] private Button copyCodeButton;

    [Header("Add Friend UI")]
    [SerializeField] private TMP_InputField friendNameInput;
    [SerializeField] private TMP_InputField friendCodeInput;
    [SerializeField] private Button addFriendButton;

    [Header("Friends List")]
    [SerializeField] private Transform friendListParent;
    [SerializeField] private FriendEntryUI friendEntryPrefab;

    private void Start()
    {
        RefreshPlayerInfo();
        RefreshFriendList();

        addFriendButton.onClick.AddListener(OnAddFriendClicked);
        copyCodeButton.onClick.AddListener(CopyFriendCodeToClipboard);
    }

    private void RefreshPlayerInfo()
    {
        var profile = FriendManager.Instance.Profile;
        playerNameText.text = profile.playerName;
        friendCodeText.text = profile.friendCode;
    }

    private void RefreshFriendList()
    {
        foreach (Transform child in friendListParent)
            Destroy(child.gameObject);

        var friends = FriendManager.Instance.GetFriends();
        foreach (var friend in friends)
        {
            var entry = Instantiate(friendEntryPrefab, friendListParent);
            entry.Setup(friend, OnRemoveFriendClicked, OnVisitFriendClicked);
        }
    }

    private void OnAddFriendClicked()
    {
        string name = friendNameInput.text.Trim();
        string code = friendCodeInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[FriendListUI] Cannot add friend: fields empty.");
            return;
        }

        if (FriendManager.Instance.AddFriend(name, code))
        {
            Debug.Log($"[FriendListUI] Added new friend: {name} ({code})");
            RefreshFriendList();
            friendNameInput.text = "";
            friendCodeInput.text = "";
        }
    }

    private void OnRemoveFriendClicked(FriendData friend)
    {
        FriendManager.Instance.RemoveFriend(friend.friendCode);
        Debug.Log($"[FriendListUI] Removed friend: {friend.playerName}");
        RefreshFriendList();
    }

    private void OnVisitFriendClicked(FriendData friend)
    {
        Debug.Log($"[FriendListUI] Searching for {friend.playerName}'s room ({friend.friendCode})");

        // Ensure the visit manager knows the friend code
        FriendVisitManager.Instance.SetFriendCode(friend.friendCode);

        // Start searching for rooms
        FriendVisitDiscovery.Instance.SearchForRooms((foundRooms) =>
        {
            if (foundRooms == null || foundRooms.Count == 0)
            {
                Debug.LogWarning($"[FriendListUI] No active rooms found for {friend.playerName}");
                return;
            }

            // Filter rooms by friend code
            var matchingRoom = foundRooms.FirstOrDefault(r => r.friendCode == friend.friendCode);

            if (matchingRoom != null)
            {
                Debug.Log($"[FriendListUI] Found matching room for {friend.playerName}: {matchingRoom.ip}");

                // Connect to the host and automatically receive the save
                FriendVisitManager.Instance.JoinHost(matchingRoom.ip);
            }
            else
            {
                Debug.LogWarning($"[FriendListUI] No matching rooms found for {friend.playerName}");
            }
        });
    }

    private void CopyFriendCodeToClipboard()
    {
        GUIUtility.systemCopyBuffer = FriendManager.Instance.Profile.friendCode;
        Debug.Log("[FriendListUI] Friend code copied to clipboard!");
    }
}
