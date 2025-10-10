using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [Header("Fade Settings")]
    public Image fadeOverlay;
    public float defaultDuration = 1f;

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (StoryEventManager.Instance.events[0].completed)
        {
            fadeOverlay.gameObject.SetActive(false);
        }
    }

    public IEnumerator Fade(bool fadeIn, float duration = -1f)
    {
        if (fadeOverlay == null) yield break;
        if (duration <= 0f) duration = defaultDuration;

        fadeOverlay.gameObject.SetActive(true);

        float t = 0f;
        Color color = fadeOverlay.color;
        float startAlpha = fadeIn ? 1f : 0f;
        float endAlpha = fadeIn ? 0f : 1f;

        while (t < duration)
        {
            t += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t / duration);
            fadeOverlay.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeOverlay.color = color;

        if (fadeIn)
            fadeOverlay.gameObject.SetActive(false);
    }
}
