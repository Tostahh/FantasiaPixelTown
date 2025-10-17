using UnityEngine;

[CreateAssetMenu(menuName = "Story/Custom Actions/Advance Tutorial")]
public class AdvanceTutorialSO : CustomStoryActionSO
{
    public override void Execute()
    {
        FindFirstObjectByType<TutorialManager>().AdvanceStep();
    }
}
