using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(menuName = "Story/Story Event")]
public class StoryEventSO : ScriptableObject
{
    public string eventID;
    public bool completed;
    public List<StoryEventActionSO> actions = new List<StoryEventActionSO>();
}
