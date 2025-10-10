using UnityEngine;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [Header("Tutorial Progress")]
    public int currentStep = 0;

    private readonly Dictionary<int, TutorialStep> steps = new Dictionary<int, TutorialStep>();

    private bool initialized;

    public bool IsLoading { get; private set; } = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    private void Start()
    {
        if (initialized) return;

        RegisterArrows();
        UpdateArrows();

        initialized = true;
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

            var step = steps[arrow.stepIndex];

            if (!step.arrows.Contains(arrow))
                step.AddArrow(arrow);
        }

        // Reset progress counts since arrows are rebuilt
        foreach (var step in steps.Values)
            step.completedCount = 0;
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
            int stepNum = kvp.Key;
            TutorialStep step = kvp.Value;

            // Remove any null arrows
            step.arrows.RemoveAll(a => a == null);

            // Only arrows for the current step are visible
            bool active = stepNum == currentStep;
            step.SetActive(active);
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
        // Ignore arrows from past steps
        if (stepIndex < currentStep) return;

        if (!steps.ContainsKey(stepIndex)) return;

        TutorialStep step = steps[stepIndex];

        step.completedCount++;

        Debug.Log($"[TutorialManager] Step {stepIndex} progress: {step.completedCount}/{step.arrows.Count}");

        // Only advance if this is the current step
        if (stepIndex == currentStep && step.completedCount >= step.arrows.Count)
        {
            Debug.Log($"[TutorialManager] Step {stepIndex} completed!");
            AdvanceStep();
        }
    }


    public void BeginLoad()
    {
        IsLoading = true;
    }

    public void EndLoad()
    {
        IsLoading = false;
        UpdateArrows(); // refresh arrow states after loading
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
