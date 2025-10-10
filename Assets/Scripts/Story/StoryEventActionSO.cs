using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Story/Story Event Action")]
public class StoryEventActionSO : ScriptableObject
{
    public StoryActionType actionType;

    [Header("Parameters")]
    public float delayTime;
    public string spawnID;
    public string dialogueID;
    public bool fadeIn;
    public float fadeDuration;
    public bool ShrineGlow;

    [Header("Custom Action (Optional)")]
    public CustomStoryActionSO customActionAsset;

    [NonSerialized]
    public Action customAction;
}
