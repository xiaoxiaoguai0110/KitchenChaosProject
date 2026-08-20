using System.Collections;
using UnityEngine;

public sealed class KitchenObjectFeedback : MonoBehaviour
{
    public enum FeedbackType
    {
        Pickup,
        Drop
    }

    private Coroutine animationRoutine;
    private Vector3 baseLocalScale;

    private void Awake()
    {
        baseLocalScale = transform.localScale;
    }

    public static void Play(KitchenObject kitchenObject, FeedbackType feedbackType)
    {
        if (kitchenObject == null)
            return;

        KitchenObjectFeedback feedback = kitchenObject.GetComponent<KitchenObjectFeedback>();
        if (feedback == null)
            feedback = kitchenObject.gameObject.AddComponent<KitchenObjectFeedback>();

        feedback.Play(feedbackType);
    }

    private void Play(FeedbackType feedbackType)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        transform.localScale = baseLocalScale;
        transform.localPosition = Vector3.zero;
        animationRoutine = StartCoroutine(Animate(feedbackType));
    }

    private IEnumerator Animate(FeedbackType feedbackType)
    {
        float duration = feedbackType == FeedbackType.Pickup ? 0.2f : 0.24f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float bounce = Mathf.Sin(t * Mathf.PI);

            if (feedbackType == FeedbackType.Pickup)
            {
                float scale = Mathf.Lerp(0.72f, 1f, t) + bounce * 0.16f;
                transform.localScale = baseLocalScale * scale;
                transform.localPosition = Vector3.up * (bounce * 0.12f);
            }
            else
            {
                float squash = bounce * 0.2f;
                transform.localScale = Vector3.Scale(
                    baseLocalScale,
                    new Vector3(1f + squash, 1f - squash, 1f + squash));
                transform.localPosition = Vector3.up * (bounce * 0.08f);
            }

            yield return null;
        }

        transform.localScale = baseLocalScale;
        transform.localPosition = Vector3.zero;
        animationRoutine = null;
    }

    private void OnDisable()
    {
        transform.localScale = baseLocalScale;
        transform.localPosition = Vector3.zero;
    }
}
