using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile
{
    public string playerName;
    public string friendCode;
    public List<FriendData> friends = new List<FriendData>();
}
