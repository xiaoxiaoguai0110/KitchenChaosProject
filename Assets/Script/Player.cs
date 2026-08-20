using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Player : KitchenObjectHolder
{
    public sealed class SelectedCounterChangedEventArgs : System.EventArgs
    {
        public BaseCounter SelectedCounter { get; }

        public SelectedCounterChangedEventArgs(BaseCounter selectedCounter)
        {
            SelectedCounter = selectedCounter;
        }
    }

    public sealed class InteractionFeedbackEventArgs : System.EventArgs
    {
        public string Message { get; }

        public InteractionFeedbackEventArgs(string message)
        {
            Message = message;
        }
    }

    public event System.EventHandler<SelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public event System.EventHandler<InteractionFeedbackEventArgs> OnInteractionFeedback;

    private const int MaxPlayers = 2;
    private static readonly Player[] s_Instances = new Player[MaxPlayers];

    /// <summary>玩家 1 的快捷入口；其他玩家请通过 GetInstance(index) 获取。</summary>
    public static Player Instance => s_Instances[0];
    public int PlayerIndex => playerIndex;

    [SerializeField] private int playerIndex;
    [Header("Movement Feel")]
    [SerializeField, Min(0.1f)] private float moveSpeed = 5f;
    [SerializeField, Min(0f)] private float acceleration = 22f;
    [SerializeField, Min(0f)] private float deceleration = 30f;
    [SerializeField, Min(0f)] private float rotateSpeed = 10f;
    [Header("Grounding")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedVerticalSpeed = -2f;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private LayerMask counterLayerMask;
    [Header("Interaction Detection")]
    [SerializeField, Min(0.5f)] private float interactionDistance = 2.25f;
    [SerializeField, Range(-1f, 1f)] private float minimumInteractionFacingDot = 0.15f;
    [SerializeField, Range(0f, 1f)] private float facingScoreWeight = 0.65f;
    [SerializeField, Min(0f)] private float currentSelectionBonus = 0.12f;
    private bool isWalking = false;
    private BaseCounter selectedCounter;
    private GameInput subscribedInput;
    private CharacterController characterController;
    private PlayerInteractionTargetSelector interactionTargetSelector;
    private Vector3 currentPlanarVelocity;
    private float verticalVelocity;

    private GameInput ResolvedInput => gameInput != null ? gameInput : GameInput.Instance;

    public static Player GetInstance(int index)
    {
        if (index < 0 || index >= MaxPlayers)
            return null;
        return s_Instances[index];
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        // 支持关闭 Domain Reload 的快速进入 Play Mode：旧场景角色不能残留在静态数组中。
        for (int i = 0; i < s_Instances.Length; i++)
            s_Instances[i] = null;
    }

    void Start()
    {
        SubscribeToInput();
        if (subscribedInput == null)
        {
            Debug.LogError("Player: Inspector 未指定 GameInput，场景中也找不到 GameInput 单例。");
        }
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        interactionTargetSelector = new PlayerInteractionTargetSelector();

        if (playerIndex < 0 || playerIndex >= MaxPlayers)
        {
            Debug.LogError($"Player: playerIndex {playerIndex} 超出合法范围，应为 0 到 {MaxPlayers - 1}。");
            return;
        }
        if (s_Instances[playerIndex] != null)
            Debug.LogWarning($"Player: 场景中存在重复的 playerIndex {playerIndex}。");
        s_Instances[playerIndex] = this;
    }

    private void OnEnable()
    {
        SubscribeToInput();
    }

    private void OnDisable()
    {
        // 玩家被禁用或场景结束时释放选择权，避免柜台高亮残留。
        SetSelectedCounter(null);
        isWalking = false;
        currentPlanarVelocity = Vector3.zero;
        verticalVelocity = 0f;
        UnsubscribeFromInput();
    }

    private void OnDestroy()
    {
        if (playerIndex >= 0 && playerIndex < MaxPlayers && s_Instances[playerIndex] == this)
            s_Instances[playerIndex] = null;
    }

    private void SubscribeToInput()
    {
        if (subscribedInput != null)
            return;

        subscribedInput = ResolvedInput;
        if (subscribedInput == null)
            return;

        subscribedInput.OnInteractAction += GameInput_OnInteractAction;
        subscribedInput.OnOperateAction += GameInput_OnOperateAction;
    }

    private void UnsubscribeFromInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnInteractAction -= GameInput_OnInteractAction;
        subscribedInput.OnOperateAction -= GameInput_OnOperateAction;
        subscribedInput = null;
    }

    private void GameInput_OnOperateAction(object sender, GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex != playerIndex)
            return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;
        if (selectedCounter == null)
        {
            ShowInteractionFailure("附近没有可操作的柜台");
            return;
        }

        if (!selectedCounter.CanOperate(this, out string failureReason))
        {
            ShowInteractionFailure(failureReason);
            return;
        }

        selectedCounter.InteractOperate(this);
        RefreshSelectedCounterPrompt();
    }

    private void GameInput_OnInteractAction(object sender, GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex != playerIndex)
            return;
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;
        if (selectedCounter == null)
        {
            ShowInteractionFailure("附近没有可交互的柜台");
            return;
        }

        if (!selectedCounter.CanInteract(this, out string failureReason))
        {
            ShowInteractionFailure(failureReason);
            return;
        }

        selectedCounter.Interact(this);
        RefreshSelectedCounterPrompt();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
        {
            StopMovement();
            return;
        }

        HandleMovement();
        HandleInteraction();
    }
    public bool IsWalking
    {
        get
        {
            return isWalking;
        }
    }

    public float MovementSpeedNormalized => moveSpeed > 0f
        ? Mathf.Clamp01(currentPlanarVelocity.magnitude / moveSpeed)
        : 0f;

    public Vector3 MoveTowardsVelocity(Vector3 desiredVelocity, float deltaTime)
    {
        desiredVelocity.y = 0f;
        currentPlanarVelocity = CalculateSmoothedVelocity(
            currentPlanarVelocity,
            desiredVelocity,
            acceleration,
            deceleration,
            deltaTime);

        // 松开输入后仍会短暂减速，所以动画和脚步声应依据真实速度，而不是按键状态。
        isWalking = currentPlanarVelocity.sqrMagnitude > 0.01f;
        Move(CalculateFrameMotion(currentPlanarVelocity, 1f, deltaTime));
        return currentPlanarVelocity;
    }

    public void StopMovement()
    {
        currentPlanarVelocity = Vector3.zero;
        isWalking = false;
    }

    private void Move(Vector3 motion)
    {
        // CharacterController 不会像 Rigidbody 一样自动受到重力。
        // 真人和 AI 都通过这个入口移动，因此在这里统一贴地，避免走下台阶后悬空。
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = groundedVerticalSpeed;
        else
            verticalVelocity += gravity * Time.deltaTime;

        motion.y += verticalVelocity * Time.deltaTime;
        CollisionFlags collisionFlags = characterController.Move(motion);

        if ((collisionFlags & CollisionFlags.Below) != 0 && verticalVelocity < 0f)
            verticalVelocity = groundedVerticalSpeed;
    }

    private void HandleMovement()
    {
        GameInput input = ResolvedInput;
        if (input == null)
            return;

        Vector3 direction = input.GetMovementDirectionNormalized(playerIndex);
        Vector3 actualVelocity = MoveTowardsVelocity(direction * moveSpeed, Time.deltaTime);
        if (actualVelocity.sqrMagnitude > 0.01f)
        {
            // 指数插值让转向响应在 30/60/120 FPS 下保持接近，不会因帧率改变而忽快忽慢。
            float rotationLerp = CalculateRotationLerpFactor(rotateSpeed, Time.deltaTime);
            transform.forward = Vector3.Slerp(
                transform.forward,
                actualVelocity.normalized,
                rotationLerp);
        }
    }

    internal static Vector3 CalculateFrameMotion(Vector3 direction, float speed, float deltaTime)
    {
        // 速度按“每秒”定义，乘 deltaTime 后每秒总位移不依赖帧率。
        return direction * speed * deltaTime;
    }

    internal static Vector3 CalculateSmoothedVelocity(
        Vector3 currentVelocity,
        Vector3 desiredVelocity,
        float acceleration,
        float deceleration,
        float deltaTime)
    {
        bool isSlowingDown = desiredVelocity.sqrMagnitude < currentVelocity.sqrMagnitude
            || Vector3.Dot(currentVelocity, desiredVelocity) < 0f;
        float changeRate = isSlowingDown ? deceleration : acceleration;
        return Vector3.MoveTowards(currentVelocity, desiredVelocity, changeRate * deltaTime);
    }

    internal static float CalculateRotationLerpFactor(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-sharpness * deltaTime);
    }

    private void HandleInteraction()
    {
        // 不再依赖一条细射线：先收集附近柜台，再综合朝向和距离评分。
        // 这样玩家斜站、贴近柜台或快速转身时，目标选择会更稳定。
        BaseCounter bestCounter = interactionTargetSelector.FindBestCounter(
            transform.position,
            transform.forward,
            interactionDistance,
            minimumInteractionFacingDot,
            facingScoreWeight,
            currentSelectionBonus,
            counterLayerMask,
            selectedCounter);

        SetSelectedCounter(bestCounter);
    }
    public void SetSelectedCounter(BaseCounter counter)
    {
        if (counter != selectedCounter)
        {
            selectedCounter?.CancelSelect(this);
            counter?.SelectCounter(this);
            this.selectedCounter = counter;
            RefreshSelectedCounterPrompt();
        }
        
    }

    public BaseCounter GetSelectedCounter()
    {
        return selectedCounter;
    }

    private void RefreshSelectedCounterPrompt()
    {
        OnSelectedCounterChanged?.Invoke(
            this,
            new SelectedCounterChangedEventArgs(selectedCounter));
    }

    private void ShowInteractionFailure(string message)
    {
        OnInteractionFeedback?.Invoke(
            this,
            new InteractionFeedbackEventArgs(message));
    }
}
