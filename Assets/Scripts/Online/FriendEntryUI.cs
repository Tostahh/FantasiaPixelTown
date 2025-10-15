using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class FriendEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text codeLabel;
    [SerializeField] private Button visitButton;
    [SerializeField] private Button removeButton;

    private FriendData friendData;
    private Action<FriendData> onRemove;
    private Action<FriendData> onVisit;

    public void Setup(FriendData friend, Action<FriendData> onRemoveCallback, Action<FriendData> onVisitCallback)
    {
        friendData = friend;
        onRemove = onRemoveCallback;
        onVisit = onVisitCallback;

        nameLabel.text = friend.playerName;
        codeLabel.text = friend.friendCode;

        removeButton.onClick.AddListener(() => onRemove?.Invoke(friendData));
        visitButton.onClick.AddListener(() => onVisit?.Invoke(friendData));
    }


}
