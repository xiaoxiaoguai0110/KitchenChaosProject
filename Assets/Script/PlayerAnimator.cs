using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    private const string IS_WALKING = "IsWalking";

    [SerializeField] private Player player;
    [Header("Animation Feel")]
    [SerializeField, Range(0.5f, 1.5f)] private float minimumWalkPlaybackSpeed = 0.82f;
    [SerializeField, Range(0.5f, 2f)] private float maximumWalkPlaybackSpeed = 1.12f;
    [SerializeField, Min(0f)] private float playbackBlendSpeed = 7f;

    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            Debug.LogError("PlayerAnimator: Missing Animator component!");
        if (player == null)
            Debug.LogError("PlayerAnimator: Player reference not assigned!");
    }

    private void Update()
    {
        if (anim == null || player == null)
            return;

        anim.SetBool(IS_WALKING, player.IsWalking);

        // 当前控制器只有 Idle/Walk 两个状态，因此用播放速度匹配实际移动速度，
        // 让起步和刹停阶段不会出现“脚很快但身体移动很慢”的滑步感。
        float targetPlaybackSpeed = player.IsWalking
            ? Mathf.Lerp(minimumWalkPlaybackSpeed, maximumWalkPlaybackSpeed, player.MovementSpeedNormalized)
            : 1f;
        anim.speed = Mathf.MoveTowards(
            anim.speed,
            targetPlaybackSpeed,
            playbackBlendSpeed * Time.deltaTime);
    }
}
