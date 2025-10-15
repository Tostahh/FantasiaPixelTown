using UnityEngine;

public class HostUI : MonoBehaviour
{
    public void HostRoom()
    {
        // Make sure save file is up to date
        FriendVisitManager.Instance.SetCurrentSaveFromManager();
        FriendVisitManager.Instance.SetFriendCode(FriendManager.Instance.Profile.friendCode);

        // Start hosting network session
        FriendVisitManager.Instance.StartHosting();

        // Broadcast this player's friend code
        FriendVisitDiscovery.Instance.StartBroadcast(FriendVisitManager.Instance.CurrentFriendCode);

        Debug.Log("[Visit] Hosting room and broadcasting friend code.");
    }

}
