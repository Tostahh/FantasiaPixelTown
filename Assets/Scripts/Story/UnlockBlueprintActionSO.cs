using UnityEngine;

[CreateAssetMenu(menuName = "Story/Custom Actions/Unlock Blueprint")]
public class UnlockBlueprintActionSO : CustomStoryActionSO
{
    public BuildingBlueprint blueprint;

    public override void Execute()
    {
        var buildUI = GameObject.FindFirstObjectByType<BuildingSelectionUI>();
        if (buildUI == null)
        {
            Debug.LogWarning("[UnlockBlueprintAction] No BuildingSelectionUI found in scene!");
            return;
        }

        buildUI.AddBlueprint(blueprint);
        Debug.Log($"[UnlockBlueprintAction] Unlocked new blueprint: {blueprint.name}");
    }
}
