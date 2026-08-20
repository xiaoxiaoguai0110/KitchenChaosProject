using UnityEngine;

public sealed class CameraFeedbackShake : MonoBehaviour
{
    private float remainingTime;
    private float amplitude;
    private Vector3 previousOffset;

    public static void Play(float shakeAmplitude, float duration)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        CameraFeedbackShake shake = mainCamera.GetComponent<CameraFeedbackShake>();
        if (shake == null)
            shake = mainCamera.gameObject.AddComponent<CameraFeedbackShake>();

        shake.amplitude = Mathf.Max(shake.amplitude, shakeAmplitude);
        shake.remainingTime = Mathf.Max(shake.remainingTime, duration);
    }

    private void LateUpdate()
    {
        // 先移除上一帧偏移，再叠加新的偏移，避免震动逐帧把相机推离原位。
        transform.localPosition -= previousOffset;
        previousOffset = Vector3.zero;

        if (remainingTime <= 0f)
            return;

        remainingTime -= Time.deltaTime;
        float fade = Mathf.Clamp01(remainingTime / 0.15f);
        Vector2 randomOffset = Random.insideUnitCircle * amplitude * fade;
        previousOffset = new Vector3(randomOffset.x, randomOffset.y, 0f);
        transform.localPosition += previousOffset;

        if (remainingTime <= 0f)
            amplitude = 0f;
    }

    private void OnDisable()
    {
        transform.localPosition -= previousOffset;
        previousOffset = Vector3.zero;
        remainingTime = 0f;
        amplitude = 0f;
    }
}
