using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIPlayer : MonoBehaviour
{
    private const float HumanCounterOccupancyDistance = 1.5f;
    private const float BusyTargetRetryDelay = 0.75f;
    private const float FailedTargetRetryDelay = 1.5f;

    private enum AIState
    {
        Idle,
        Moving,
        Acting,
    }

    [SerializeField] private CuttingRecipeListSO cuttingRecipeList;
    [SerializeField] private FryingRecipeListSO fryingRecipeList;

    private Player player;
    private Player humanPlayer;
    private NavMeshAgent navMeshAgent;
    private OrderManager orderManager;
    private AIPlayerTargetSelector targetSelector;
    private AIPlayerMovement movement;
    private AIPlayerActionController actionController;

    private AIState currentState = AIState.Idle;
    private BaseCounter targetCounter;
    private BaseCounter temporarilyAvoidedCounter;
    private float idleCooldown;
    private float avoidCounterUntil;
    private bool hasReceivedFirstOrder;
    private OrderManager subscribedOrderManager;

    private void Awake()
    {
        player = GetComponent<Player>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        player.enabled = false;

        // NavMeshAgent 只负责规划路线；实际位移仍交给 CharacterController，
        // 避免两个组件在同一帧同时修改角色位置而产生抖动或穿模。
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
    }

    private void Start()
    {
        // Start 中再查找柜台和单例，因为这些对象的 Awake 此时都已经执行完毕。
        BaseCounter[] allCounters = FindObjectsOfType<BaseCounter>();
        orderManager = OrderManager.Instance;
        targetSelector = new AIPlayerTargetSelector(
            player,
            allCounters,
            orderManager,
            cuttingRecipeList,
            fryingRecipeList);
        movement = new AIPlayerMovement(transform, player, navMeshAgent);
        actionController = new AIPlayerActionController(player, cuttingRecipeList, fryingRecipeList);

        ConfigureHumanPlayerAvoidance();

        SubscribeToFirstOrder();
    }

    private void OnEnable()
    {
        SubscribeToFirstOrder();
    }

    private void OnDisable()
    {
        UnsubscribeFromFirstOrder();
        // 切换游戏模式或禁用 AI 时必须清掉旧路径，否则重新启用后可能继续追逐旧目标。
        movement?.Stop();
    }

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGamePlayingState())
        {
            movement?.Stop();
            return;
        }

        if (!GameManager.Instance.IsGameSimulationRunning())
        {
            // 暂停时保留当前目标和 NavMesh 路径，恢复后可以从原计划继续。
            player.StopMovement();
            return;
        }

        // AIPlayer 只负责调度状态；选目标、移动和操作分别交给三个独立模块。
        switch (currentState)
        {
            case AIState.Idle:
                UpdateIdle();
                break;
            case AIState.Moving:
                UpdateMoving();
                break;
            case AIState.Acting:
                UpdateAction();
                break;
        }
    }

    private void OnFirstOrderSpawned(object sender, EventArgs e)
    {
        UnsubscribeFromFirstOrder();
        hasReceivedFirstOrder = true;
        SelectNextTarget();
    }

    private void SubscribeToFirstOrder()
    {
        if (hasReceivedFirstOrder || subscribedOrderManager != null || orderManager == null)
            return;

        subscribedOrderManager = orderManager;
        subscribedOrderManager.OnRecipeSpawned += OnFirstOrderSpawned;
    }

    private void UnsubscribeFromFirstOrder()
    {
        if (subscribedOrderManager == null)
            return;

        subscribedOrderManager.OnRecipeSpawned -= OnFirstOrderSpawned;
        subscribedOrderManager = null;
    }

    private void UpdateIdle()
    {
        if (!hasReceivedFirstOrder)
            return;

        if (idleCooldown > 0f)
        {
            idleCooldown -= Time.deltaTime;
            return;
        }

        SelectNextTarget();
    }

    private void UpdateMoving()
    {
        if (targetCounter == null)
        {
            ChangeToIdle(0f);
            return;
        }

        if (targetSelector.IsBlocked(targetCounter))
        {
            AvoidCounterTemporarily(targetCounter, BusyTargetRetryDelay);
            return;
        }

        if (IsTargetOccupiedByHuman(targetCounter))
        {
            AvoidCounterTemporarily(targetCounter, BusyTargetRetryDelay);
            return;
        }

        // 移动模块返回明确结果，状态机不需要知道 NavMesh 的具体计算细节。
        AIMoveResult moveResult = movement.MoveTowards(targetCounter, Time.deltaTime);
        switch (moveResult)
        {
            case AIMoveResult.Reached:
                OnReachedTarget();
                break;
            case AIMoveResult.Failed:
                // 路径失败后回到 Idle，让目标选择器重新规划，而不是永远顶着障碍物。
                AvoidCounterTemporarily(targetCounter, FailedTargetRetryDelay);
                break;
        }
    }

    private void OnReachedTarget()
    {
        movement.Stop();
        movement.Face(targetCounter);

        if (actionController.Begin(targetCounter))
            currentState = AIState.Acting;
        else
            ChangeToIdle(0.15f);
    }

    private void UpdateAction()
    {
        movement.Stop();

        if (actionController.Tick(Time.deltaTime, out float nextIdleCooldown))
            ChangeToIdle(nextIdleCooldown);
    }

    private void SelectNextTarget()
    {
        BaseCounter excludedCounter = Time.time < avoidCounterUntil
            ? temporarilyAvoidedCounter
            : null;

        targetCounter = targetSelector.PickTarget(excludedCounter);
        if (targetCounter != null)
            currentState = AIState.Moving;
        else
            idleCooldown = 0.15f;
    }

    private void AvoidCounterTemporarily(BaseCounter counter, float duration)
    {
        temporarilyAvoidedCounter = counter;
        avoidCounterUntil = Time.time + duration;
        ChangeToIdle(0.15f);
    }

    private void ChangeToIdle(float cooldown)
    {
        movement?.Stop();
        targetCounter = null;
        idleCooldown = cooldown;
        currentState = AIState.Idle;
    }

    private bool IsTargetOccupiedByHuman(BaseCounter counter)
    {
        if (humanPlayer == null || !humanPlayer.gameObject.activeInHierarchy)
            return false;

        Vector3 humanPosition = humanPlayer.transform.position;
        Vector3 counterPosition = counter.transform.position;
        humanPosition.y = 0f;
        counterPosition.y = 0f;
        return (humanPosition - counterPosition).sqrMagnitude
            <= HumanCounterOccupancyDistance * HumanCounterOccupancyDistance;
    }

    private void ConfigureHumanPlayerAvoidance()
    {
        CharacterController aiController = GetComponent<CharacterController>();
        humanPlayer = Player.GetInstance(0);
        CharacterController humanController = humanPlayer != null
            ? humanPlayer.GetComponent<CharacterController>()
            : null;

        if (aiController != null && humanController != null)
            Physics.IgnoreCollision(aiController, humanController);

        if (humanPlayer == null || humanController == null)
            return;

        NavMeshObstacle humanObstacle = humanPlayer.GetComponent<NavMeshObstacle>();
        if (humanObstacle != null)
            return;

        // 移动中的真人由局部避障处理；停下后 Carving 会让 Agent 重新规划绕行路线。
        humanObstacle = humanPlayer.gameObject.AddComponent<NavMeshObstacle>();
        humanObstacle.shape = NavMeshObstacleShape.Capsule;
        humanObstacle.center = humanController.center;
        humanObstacle.radius = humanController.radius;
        humanObstacle.height = humanController.height;
        humanObstacle.carving = true;
        humanObstacle.carveOnlyStationary = true;
        humanObstacle.carvingMoveThreshold = 0.2f;
        humanObstacle.carvingTimeToStationary = 0.5f;
    }
}
