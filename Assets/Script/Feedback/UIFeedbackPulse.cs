using System.Collections;
using UnityEngine;

public sealed class UIFeedbackPulse : MonoBehaviour
{
    private RectTransform rectTransform;
    private Coroutine animationRoutine;
    private Vector2 basePosition;
    private Vector3 baseScale;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        if (rectTransform == null)
            return;

        basePosition = rectTransform.anchoredPosition;
        baseScale = rectTransform.localScale;
    }

    public void PlayError()
    {
        if (rectTransform == null)
            return;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);
        animationRoutine = StartCoroutine(AnimateError());
    }

    private IEnumerator AnimateError()
    {
        const float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float strength = 1f - t;
            rectTransform.anchoredPosition = basePosition
                + Vector2.right * (Mathf.Sin(t * Mathf.PI * 6f) * 9f * strength);
            rectTransform.localScale = baseScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.08f);
            yield return null;
        }

        rectTransform.anchoredPosition = basePosition;
        rectTransform.localScale = baseScale;
        animationRoutine = null;
    }

    private void OnDisable()
    {
        if (rectTransform == null)
            return;

        rectTransform.anchoredPosition = basePosition;
        rectTransform.localScale = baseScale;
    }
}
