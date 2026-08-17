using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class UIPopupAnimator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform panel;
    [SerializeField] private float duration = .22f;
    [SerializeField] private float startScale = .92f;

    private Coroutine showCoroutine;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    public void PlayShow()
    {
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);

        showCoroutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        if (panel != null)
            panel.localScale = Vector3.one * startScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 暂停菜单出现时 Time.timeScale 为 0，因此必须使用不受游戏暂停影响的时间。
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = 1f - Mathf.Pow(1f - normalizedTime, 3f);

            if (canvasGroup != null)
                canvasGroup.alpha = easedTime;
            if (panel != null)
                panel.localScale = Vector3.one * Mathf.Lerp(startScale, 1f, easedTime);

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (panel != null)
            panel.localScale = Vector3.one;

        showCoroutine = null;
    }

    private void OnDisable()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        // 重置显示状态，保证弹窗下一次打开不会保留中途动画的透明度或缩放。
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        if (panel != null)
            panel.localScale = Vector3.one;
    }
}
