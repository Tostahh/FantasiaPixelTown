using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class StoryEventManager : MonoBehaviour
{
    public static Action<int> OnChapterAdvance = delegate { };
    public static StoryEventManager Instance;

    public int ChapterNumber;

    [Header("Registered Events")]
    public List<StoryEventSO> events = new List<StoryEventSO>();
    private Dictionary<string, StoryEventSO> eventLookup = new Dictionary<string, StoryEventSO>();

    public event Action<string> OnEventStarted;
    public event Action<string> OnEventCompleted;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Build quick lookup dictionary
        foreach (var e in events)
        {
            if (!eventLookup.ContainsKey(e.eventID))
                eventLookup.Add(e.eventID, e);
        }
    }

    /// <summary>
    /// Trigger a story event by its ID. Executes all actions.
    /// </summary>
    public void Trigger(string eventID)
    {
        if (!eventLookup.ContainsKey(eventID))
        {
            Debug.LogWarning($"[StoryEventManager] No event found for ID: {eventID}");
            return;
        }

        StoryEventSO storyEvent = eventLookup[eventID];
        if (storyEvent.completed)
        {
            Debug.Log($"[StoryEventManager] Event '{eventID}' already completed.");
            return;
        }

        Debug.Log($"[StoryEventManager] Triggering '{eventID}'...");
        OnEventStarted?.Invoke(eventID);

        StartCoroutine(RunEvent(storyEvent));
    }

    public void AdvanceChapter(int c)
    {
        ChapterNumber = c;
        OnChapterAdvance(c);
    }

    private IEnumerator RunEvent(StoryEventSO storyEvent)
    {
        foreach (var action in storyEvent.actions)
        {
            yield return ExecuteAction(action);
        }

        storyEvent.completed = true;
        OnEventCompleted?.Invoke(storyEvent.eventID);
        Debug.Log($"[StoryEventManager] Event '{storyEvent.eventID}' complete!");
    }

    private IEnumerator ExecuteAction(StoryEventActionSO action)
    {
        switch (action.actionType)
        {
            case StoryActionType.Delay:
                yield return new WaitForSeconds(action.delayTime);
                break;
            case StoryActionType.SpawnNPC:
                NPCManager.Instance.Spawn(action.spawnID);
                break;
            case StoryActionType.PlayDialogue:
                yield return DialogueSystem.Instance.StartDialogueCoroutine(action.dialogueID);
                break;
            case StoryActionType.FadeScreen:
                yield return ScreenFader.Instance.Fade(action.fadeIn, action.fadeDuration);
                break;
            case StoryActionType.TriggerEffect:
                FindFirstObjectByType<ShrineController>()?.PlayGlowEffect();
                break;
            case StoryActionType.Custom:
                if (action.customActionAsset != null)
                    action.customActionAsset.Execute();
                else
                    action.customAction?.Invoke();
                break;
        }
    }

    // ------------------------------
    //  SAVE / LOAD HELPERS
    // ------------------------------

    public List<string> GetCompletedEventIDs()
    {
        List<string> completedIDs = new List<string>();
        foreach (var e in events)
        {
            if (e != null && e.completed)
                completedIDs.Add(e.eventID);
        }
        return completedIDs;
    }

    public void SetCompletedEvents(List<string> ids)
    {
        if (ids == null || ids.Count == 0) return;

        foreach (var e in events)
        {
            if (e == null) continue;
            e.completed = ids.Contains(e.eventID);
        }

        Debug.Log($"[StoryEventManager] Restored {ids.Count} completed events from save.");
    }

    /// <summary>
    /// Restores triggers for all events whose prerequisites are completed
    /// but themselves are not yet completed.
    /// </summary>
    public void RestorePendingTriggers()
    {
        foreach (var e in events)
        {
            if (e.completed) continue;

            if (!string.IsNullOrEmpty(e.prerequisiteEventID) && eventLookup.ContainsKey(e.prerequisiteEventID))
            {
                var prereqEvent = eventLookup[e.prerequisiteEventID];
                if (prereqEvent.completed)
                {
                    Debug.Log($"[StoryEventManager] Restoring trigger for pending event '{e.eventID}'...");
                    Trigger(e.eventID);
                }
            }
        }
    }
}

public enum StoryActionType
{
    Delay,
    SpawnNPC,
    PlayDialogue,
    FadeScreen,
    TriggerEffect,
    Custom
}
