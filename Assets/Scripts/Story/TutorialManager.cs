using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Progress")]
    public int currentStep = 0;

    private readonly Dictionary<int, TutorialStep> steps = new Dictionary<int, TutorialStep>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        RegisterArrows();
        UpdateArrows();
    }

    /// <summary>
    /// Register all arrows in the scene into step groups.
    /// </summary>
    private void RegisterArrows()
    {
        steps.Clear();
        TutorialArrow[] arrows = FindObjectsByType<TutorialArrow>(FindObjectsSortMode.None);
        foreach (var arrow in arrows)
        {
            if (!steps.ContainsKey(arrow.stepIndex))
                steps.Add(arrow.stepIndex, new TutorialStep());

            steps[arrow.stepIndex].AddArrow(arrow);
        }
    }

    public void AdvanceStep()
    {
        currentStep++;
        Debug.Log($"[TutorialManager] Advanced to step {currentStep}");
        UpdateArrows();
    }

    public void SetStep(int step)
    {
        currentStep = step;
        UpdateArrows();
    }

    public void UpdateArrows()
    {
        foreach (var kvp in steps)
        {
            TutorialStep step = kvp.Value;

            // Remove missing arrows and mark them as completed
            for (int i = step.arrows.Count - 1; i >= 0; i--)
            {
                if (step.arrows[i] == null)
                {
                    step.completedCount++;
                    step.arrows.RemoveAt(i);
                }
            }

            // Activate arrows only for the current step
            bool active = kvp.Key == currentStep;
            step.SetActive(active);

            // Automatically advance if all arrows are gone
            if (step.completedCount >= step.arrows.Count && kvp.Key == currentStep)
            {
                Debug.Log($"[TutorialManager] Step {kvp.Key} completed automatically!");
                AdvanceStep();
            }
        }
    }

    public void RegisterArrowOnEnable(TutorialArrow arrow)
    {
        if (!steps.ContainsKey(arrow.stepIndex))
            steps.Add(arrow.stepIndex, new TutorialStep());

        TutorialStep step = steps[arrow.stepIndex];

        // Only add if not already in the list
        if (!step.arrows.Contains(arrow))
            step.AddArrow(arrow);
    }

    /// <summary>
    /// Called by TutorialArrow when one objective is completed.
    /// </summary>
    public void NotifyArrowComplete(int stepIndex)
    {
        if (!steps.ContainsKey(stepIndex)) return;

        TutorialStep step = steps[stepIndex];

        step.completedCount++;

        Debug.Log($"[TutorialManager] Step {stepIndex} progress: {step.completedCount}/{step.arrows.Count}");

        if (step.completedCount >= step.arrows.Count)
        {
            Debug.Log($"[TutorialManager] Step {stepIndex} completed!");
            AdvanceStep();
        }
    }
}

[System.Serializable]
public class TutorialStep
{
    public List<TutorialArrow> arrows = new List<TutorialArrow>();
    public int completedCount = 0;

    public void AddArrow(TutorialArrow arrow)
    {
        arrows.Add(arrow);
    }

    public void SetActive(bool state)
    {
        foreach (var arrow in arrows)
        {
            if (arrow != null)
                arrow.SetVisible(state);
        }
    }

}
