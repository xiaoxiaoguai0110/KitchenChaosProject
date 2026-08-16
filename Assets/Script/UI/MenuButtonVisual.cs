using UnityEngine;
using UnityEngine.EventSystems;

public sealed class MenuButtonVisual : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler,
    ISelectHandler,
    IDeselectHandler
{
    private const float HoverScale = 1.035f;
    private const float PressedScale = 0.975f;
    private const float AnimationSpeed = 14f;

    private RectTransform rectTransform;
    private float targetScale = 1f;
    private bool isHovered;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
    }

    private void Update()
    {
        float scale = Mathf.Lerp(rectTransform.localScale.x, targetScale, Time.unscaledDeltaTime * AnimationSpeed);
        rectTransform.localScale = new Vector3(scale, scale, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        targetScale = HoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        targetScale = 1f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = PressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = isHovered ? HoverScale : 1f;
    }

    public void OnSelect(BaseEventData eventData)
    {
        targetScale = HoverScale;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!isHovered)
            targetScale = 1f;
    }
}
