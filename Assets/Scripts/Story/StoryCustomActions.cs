using System;
using UnityEngine;
public static class StoryCustomActions
{
    public static Action GiveBlueprint(BuildingBlueprint blueprint)
    {
        return () =>
        {
            var buildUI = GameObject.FindFirstObjectByType<BuildingSelectionUI>();
            if (buildUI == null)
            {
                Debug.LogWarning("[StoryCustomActions] No BuildingSelectionUI found in scene!");
                return;
            }

            buildUI.AddBlueprint(blueprint);
            Debug.Log($"[StoryCustomActions] Player unlocked new blueprint: {blueprint.name}");
        };
    }
}
