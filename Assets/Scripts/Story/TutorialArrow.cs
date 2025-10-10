using UnityEngine;
using System.Collections;
using UnityEngine.UI; // for Image support

public class TutorialArrow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Arrow Settings")]
    public Vector3 offset = new Vector3(0, 1f, 0);
    public float pulseSpeed = 2f;
    public float pulseScale = 1.2f;
    public bool followTarget = true;

    [Header("Tutorial Step")]
    public int stepIndex;

    private Vector3 baseScale;
    private bool isPulsing;
    private bool completed;

    private RectTransform rectTransform;
    private RectTransform targetRect;

    private SpriteRenderer spriteRenderer;
    private Image uiImage;

    private void Awake()
    {
        baseScale = transform.localScale;
        rectTransform = GetComponent<RectTransform>();
        targetRect = target != null ? target.GetComponent<RectTransform>() : null;

        // Cache whichever visual component exists
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        uiImage = GetComponentInChildren<Image>();
    }

    private void OnEnable()
    {
        TutorialManager.Instance?.RegisterArrowOnEnable(this);

        // Only pulse and show if we're not loading
        if (!TutorialManager.Instance.IsLoading)
        {
            if (!completed && !isPulsing)
                StartCoroutine(PulseRoutine());
        }

        SetVisible(stepIndex == TutorialManager.Instance.currentStep);
    }



    private void OnDisable()
    {
        StopAllCoroutines();
        isPulsing = false;
    }

    private void Update()
    {
        if (target == null) return;

        if (followTarget)
        {
            if (rectTransform != null) // Arrow is UI
            {
                Vector3 screenPos = targetRect != null
                    ? targetRect.position // Target is UI
                    : Camera.main.WorldToScreenPoint(target.position); // Target is world
                rectTransform.position = screenPos + offset;
            }
            else // Arrow is world object
            {
                Vector3 worldPos = target.position + offset;
                transform.position = worldPos;
            }
        }
    }

    private IEnumerator PulseRoutine()
    {
        isPulsing = true;
        while (isPulsing)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            transform.localScale = Vector3.Lerp(baseScale, baseScale * pulseScale, t);
            yield return null;
        }
    }

    /// <summary>
    /// Called by the TutorialManager to show or hide the arrow visuals.
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;

        if (uiImage != null)
            uiImage.enabled = visible;
    }

    /// <summary>
    /// Call this when the player interacts with the object the arrow is pointing to.
    /// </summary>
    public void CompleteArrow()
    {
        if (completed) return;

        completed = true;
        TutorialManager.Instance.NotifyArrowComplete(stepIndex);

        // Just hide visuals instead of disabling the whole GameObject
        SetVisible(false);
    }

    private void OnDestroy()
    {
        if (Application.isPlaying)
            CompleteArrow();
    }
}
