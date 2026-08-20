using TMPro;
using UnityEngine;

public sealed class WorldFeedbackPopup : MonoBehaviour
{
    private const float Duration = 0.85f;

    private TextMeshPro textMesh;
    private Color baseColor;
    private Vector3 startPosition;
    private float elapsed;

    public static void Show(Vector3 position, string message, Color color)
    {
        GameObject popupObject = new GameObject("World Feedback Popup");
        popupObject.transform.position = position;

        TextMeshPro textMesh = popupObject.AddComponent<TextMeshPro>();
        textMesh.text = message;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 4.2f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.color = color;
        textMesh.sortingOrder = 80;
        textMesh.rectTransform.sizeDelta = new Vector2(12f, 3f);

        WorldFeedbackPopup popup = popupObject.AddComponent<WorldFeedbackPopup>();
        popup.textMesh = textMesh;
        popup.baseColor = color;
        popup.startPosition = position;
        popupObject.transform.localScale = Vector3.one * 0.35f;
    }

    private void LateUpdate()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Duration);

        transform.position = startPosition + Vector3.up * Mathf.Lerp(0f, 0.8f, t);
        float scale = 0.35f * (1f + Mathf.Sin(t * Mathf.PI) * 0.25f);
        transform.localScale = Vector3.one * scale;

        Color color = baseColor;
        color.a = 1f - Mathf.SmoothStep(0f, 1f, t);
        textMesh.color = color;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);

        if (t >= 1f)
            Destroy(gameObject);
    }
}
