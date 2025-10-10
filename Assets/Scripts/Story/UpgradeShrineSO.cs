using UnityEngine;

[CreateAssetMenu(menuName = "Story/Custom Actions/Upgrade Shrine")]
public class UpgradeShrineSO : CustomStoryActionSO
{
    public override void Execute()
    {
        FindFirstObjectByType<ShrineController>().GetComponent<Building>().UpgradeWOCost();
    }
}
