using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    [Header("Footstep Feel")]
    [SerializeField, Min(0.05f)] private float slowStepInterval = 0.34f;
    [SerializeField, Min(0.05f)] private float fastStepInterval = 0.2f;
    [SerializeField, Range(0f, 1f)] private float slowStepVolume = 0.18f;
    [SerializeField, Range(0f, 1f)] private float fastStepVolume = 0.3f;

    private Player player;
    private float stepSoundTimer;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Update()
    {
        if (player == null || !player.IsWalking || SoundManager.Instance == null)
        {
            stepSoundTimer = 0f;
            return;
        }

        float speedRatio = player.MovementSpeedNormalized;
        float stepInterval = Mathf.Lerp(slowStepInterval, fastStepInterval, speedRatio);
        stepSoundTimer += Time.deltaTime;
        if (stepSoundTimer < stepInterval)
            return;

        // 速度越快，脚步间隔越短、音量略高；停止时计时器会清零，避免原地补播一步。
        stepSoundTimer -= stepInterval;
        float volume = Mathf.Lerp(slowStepVolume, fastStepVolume, speedRatio);
        SoundManager.Instance.PlayStepSound(volume);
    }
}
