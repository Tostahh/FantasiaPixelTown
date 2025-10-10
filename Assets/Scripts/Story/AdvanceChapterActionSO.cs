using UnityEngine;

[CreateAssetMenu(menuName = "Story/Custom Actions/Advance Chapter")]
public class AdvanceChapterActionSO : CustomStoryActionSO
{
    public int ChapterToAdvanceTo = 1;

    public override void Execute()
    {
        StoryEventManager.Instance.AdvanceChapter(ChapterToAdvanceTo);
    }
}
