using UnityEngine;
using UnityEngine.AI;

internal enum AIMoveResult
{
    Moving, // 正在移动，或者仍在等待 Unity 完成路径计算。
    Reached, // 已进入柜台交互距离，可以切换到 Acting 状态。
    Failed, // 起点或终点无效、路径不完整，需要重新选择目标。
}

internal sealed class AIPlayerMovement
{
    private const float InteractionDistance = 1.8f;
    private const float ApproachDistance = 1.55f;
    private const float DestinationSampleRadius = 0.5f;
    private const float AgentPlacementSampleRadius = 1f;
    private const float MoveSpeed = 5f;
    private const float RotationSpeed = 720f;
    private const float MinimumMoveSpeed = 0.01f;
    private const float MinimumProgressDistance = 0.08f;
    private const float StuckDuration = 2f;
    private const float TargetTimeout = 12f;
    private const float AlternateDestinationDistance = 0.35f;
    private const int MaxRepathAttempts = 2;

    private static readonly float[] CandidateAngles =
    {
        0f,
        45f,
        -45f,
        90f,
        -90f,
        135f,
        -135f,
        180f,
    };

    private readonly Transform transform;
    private readonly Player player;
    private readonly NavMeshAgent agent;

    private BaseCounter currentCounter;
    private BaseCounter lastWarnedCounter;
    private Vector3 currentDestination;
    private Vector3 progressSamplePosition;
    private float noProgressTime;
    private float targetElapsedTime;
    private int repathAttempts;
    private bool warnedAgentOffNavMesh;

    public AIPlayerMovement(Transform transform, Player player, NavMeshAgent agent)
    {
        this.transform = transform;
        this.player = player;
        this.agent = agent;

        // desiredVelocity 的单位是“米/秒”，要与原有 AI 移动速度保持一致。
        agent.speed = MoveSpeed;
    }

    public AIMoveResult MoveTowards(BaseCounter counter, float deltaTime)
    {
        // 每帧按“检查 Agent → 规划路径 → 读取速度 → 实际移动”的顺序执行。
        if (counter == null || !EnsureAgentIsOnNavMesh())
        {
            Stop();
            return AIMoveResult.Failed;
        }

        if (GetPlanarDistance(transform.position, counter.transform.position) <= InteractionDistance)
            return AIMoveResult.Reached;

        if (currentCounter != counter)
        {
            if (!TryPlanPath(counter, false))
            {
                Stop();
                WarnPathFailureOnce(counter);
                return AIMoveResult.Failed;
            }

            currentCounter = counter;
            progressSamplePosition = transform.position;
            noProgressTime = 0f;
            targetElapsedTime = 0f;
            repathAttempts = 0;
        }

        targetElapsedTime += deltaTime;
        if (targetElapsedTime >= TargetTimeout)
        {
            Stop();
            WarnPathFailureOnce(counter, "到达目标超时");
            return AIMoveResult.Failed;
        }

        // SetDestination 后 Unity 可能需要一帧计算路线，等待期间不能把“暂无路径”误判为失败。
        if (agent.pathPending)
        {
            player.StopMovement();
            return AIMoveResult.Moving;
        }

        // Partial 表示只能走到半路，Invalid 表示完全无法规划；两者都不能继续执行柜台操作。
        if (!agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            if (TryReplanPath(counter))
                return AIMoveResult.Moving;

            Stop();
            WarnPathFailureOnce(counter, "路径失效且重规划失败");
            return AIMoveResult.Failed;
        }

        // desiredVelocity 已包含路径拐弯、速度和局部避障结果，但它不会替我们移动 CharacterController。
        Vector3 velocity = agent.desiredVelocity;
        velocity.y = 0f;

        bool hasMovementIntent = agent.remainingDistance > 0.2f
            || velocity.sqrMagnitude > MinimumMoveSpeed * MinimumMoveSpeed;
        if (UpdateStuckTimer(deltaTime, hasMovementIntent))
        {
            if (TryReplanPath(counter))
                return AIMoveResult.Moving;

            Stop();
            WarnPathFailureOnce(counter, "连续无有效位移，已达到重规划上限");
            return AIMoveResult.Failed;
        }

        if (velocity.sqrMagnitude <= MinimumMoveSpeed * MinimumMoveSpeed)
        {
            player.StopMovement();
            return AIMoveResult.Moving;
        }

        Vector3 actualVelocity = player.MoveTowardsVelocity(velocity, deltaTime);

        // CharacterController 移动了 Transform 后，要把真实位置同步回 Agent 的内部模拟位置，
        // 否则路径规划位置会和画面中的角色位置逐帧分离。
        agent.nextPosition = transform.position;

        Vector3 moveDirection = actualVelocity.normalized;
        transform.forward = Vector3.RotateTowards(
            transform.forward,
            moveDirection,
            RotationSpeed * Mathf.Deg2Rad * deltaTime,
            0f);
        return AIMoveResult.Moving;
    }

    public void Face(BaseCounter counter)
    {
        Vector3 direction = counter.transform.position - transform.position;
        direction.y = 0f;
        if (direction != Vector3.zero)
            transform.forward = direction.normalized;
    }

    public void Stop()
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            // ResetPath 不只是“停下”，还防止下次启用 AI 时继续追逐已经过期的柜台。
            agent.ResetPath();
            agent.nextPosition = transform.position;
        }

        currentCounter = null;
        currentDestination = default;
        progressSamplePosition = transform.position;
        noProgressTime = 0f;
        targetElapsedTime = 0f;
        repathAttempts = 0;
        player.StopMovement();
    }

    private bool EnsureAgentIsOnNavMesh()
    {
        if (agent == null || !agent.enabled)
            return false;

        if (agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
            return true;
        }

        // 场景出生点可能因高度或烘焙误差略微离开 NavMesh，先投影到最近的可行走点。
        if (NavMesh.SamplePosition(
                transform.position,
                out NavMeshHit hit,
                AgentPlacementSampleRadius,
                agent.areaMask)
            && agent.Warp(hit.position))
        {
            warnedAgentOffNavMesh = false;
            return true;
        }

        if (!warnedAgentOffNavMesh)
        {
            Debug.LogWarning("AIPlayerMovement: AI 不在 NavMesh 上，请检查出生点、Agent Type 和烘焙数据。");
            warnedAgentOffNavMesh = true;
        }

        return false;
    }

    private bool TryPlanPath(BaseCounter counter, bool preferAlternateDestination)
    {
        Vector3 counterPosition = counter.transform.position;
        Vector3 primaryDirection = transform.position - counterPosition;
        primaryDirection.y = 0f;
        if (primaryDirection.sqrMagnitude < 0.001f)
            primaryDirection = -counter.transform.forward;
        primaryDirection.Normalize();

        Vector3 bestDestination = default;
        float bestPathLength = float.PositiveInfinity;
        bool foundCompletePath = false;
        Vector3 fallbackDestination = default;
        float fallbackPathLength = float.PositiveInfinity;
        bool foundFallbackPath = false;

        // 柜台中心属于障碍物，不能直接作为终点。围绕柜台尝试多个站位，
        // 再选择一条完整且最短的路线，能兼容靠墙柜台和相邻柜台。
        foreach (float angle in CandidateAngles)
        {
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * primaryDirection;
            Vector3 candidate = counterPosition + direction * ApproachDistance;
            candidate.y = transform.position.y;

            if (!NavMesh.SamplePosition(
                    candidate,
                    out NavMeshHit hit,
                    DestinationSampleRadius,
                    agent.areaMask))
            {
                continue;
            }

            if (GetPlanarDistance(hit.position, counterPosition) > InteractionDistance)
                continue;

            // CalculatePath 在这里同步验证候选点，只有能完整到达的候选站位才参与比较。
            NavMeshPath candidatePath = new NavMeshPath();
            if (!agent.CalculatePath(hit.position, candidatePath)
                || candidatePath.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            // corners 是路线的各个拐点，逐段相加才能比较真实路线长度，而不是直线距离。
            float pathLength = GetPathLength(candidatePath);

            bool isPreviousDestination = preferAlternateDestination
                && GetPlanarDistance(hit.position, currentDestination) < AlternateDestinationDistance;
            if (isPreviousDestination)
            {
                if (pathLength < fallbackPathLength)
                {
                    fallbackPathLength = pathLength;
                    fallbackDestination = hit.position;
                    foundFallbackPath = true;
                }
                continue;
            }

            if (pathLength >= bestPathLength)
                continue;

            bestPathLength = pathLength;
            bestDestination = hit.position;
            foundCompletePath = true;
        }

        // 周围没有其他合法站位时仍允许使用原终点，避免只有单侧可交互的柜台彻底不可达。
        if (!foundCompletePath && foundFallbackPath)
        {
            bestDestination = fallbackDestination;
            foundCompletePath = true;
        }

        if (!foundCompletePath || !agent.SetDestination(bestDestination))
            return false;

        currentDestination = bestDestination;
        lastWarnedCounter = null;
        return true;
    }

    private bool UpdateStuckTimer(float deltaTime, bool hasMovementIntent)
    {
        float movedDistance = GetPlanarDistance(transform.position, progressSamplePosition);
        if (movedDistance >= MinimumProgressDistance)
        {
            progressSamplePosition = transform.position;
            noProgressTime = 0f;
            return false;
        }

        if (hasMovementIntent)
            noProgressTime += deltaTime;
        else
            noProgressTime = 0f;

        return noProgressTime >= StuckDuration;
    }

    private bool TryReplanPath(BaseCounter counter)
    {
        if (repathAttempts >= MaxRepathAttempts)
            return false;

        repathAttempts++;
        agent.ResetPath();
        if (!TryPlanPath(counter, true))
            return false;

        // 重新规划只重置“卡住计时”，目标总超时继续累计，防止无限重试。
        progressSamplePosition = transform.position;
        noProgressTime = 0f;
        player.StopMovement();
        return true;
    }

    private void WarnPathFailureOnce(BaseCounter counter, string reason = null)
    {
        if (lastWarnedCounter == counter)
            return;

        string detail = string.IsNullOrEmpty(reason) ? "无法找到完整 NavMesh 路径" : reason;
        Debug.LogWarning($"AIPlayerMovement: 柜台 {counter.name} {detail}，将重新选择目标。");
        lastWarnedCounter = counter;
    }

    private static float GetPlanarDistance(Vector3 from, Vector3 to)
    {
        from.y = 0f;
        to.y = 0f;
        return Vector3.Distance(from, to);
    }

    private static float GetPathLength(NavMeshPath path)
    {
        float length = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            length += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return length;
    }
}
