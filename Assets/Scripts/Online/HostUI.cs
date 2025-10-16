using UnityEngine;

public class HostUI : MonoBehaviour
{
    private void Start()
    {
        Debug.LogError("Start Log");
    }

    public void HostRoom()
    {
        // Make sure save file is up to date
        FriendVisitManager.Instance.SetFriendCode(FriendManager.Instance.Profile.friendCode);

        // Start hosting network session
        FriendVisitManager.Instance.StartHosting();

        // Broadcast this player's friend code
        FriendVisitDiscovery.Instance.StartBroadcast(FriendVisitManager.Instance.CurrentFriendCode);

        Debug.Log("[Visit] Hosting room and broadcasting friend code.");
    }

}
