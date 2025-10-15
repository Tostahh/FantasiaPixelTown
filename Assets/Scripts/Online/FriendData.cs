using System;

[Serializable]
public class FriendData
{
    public string playerName;
    public string friendCode;
    public DateTime dateAdded;

    public FriendData(string name, string code)
    {
        playerName = name;
        friendCode = code;
        dateAdded = DateTime.Now;
    }
}
