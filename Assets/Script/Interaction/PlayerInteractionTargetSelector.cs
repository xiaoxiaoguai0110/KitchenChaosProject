using UnityEngine;

/// <summary>
/// 收集玩家附近的柜台，并根据朝向、距离和当前选择稳定性挑出最合适的交互目标。
/// </summary>
public sealed class PlayerInteractionTargetSelector
{
    private const int MaxNearbyColliders = 24;
    private readonly Collider[] nearbyColliders = new Collider[MaxNearbyColliders];

    public BaseCounter FindBestCounter(
        Vector3 playerPosition,
        Vector3 playerForward,
        float interactionDistance,
        float minimumFacingDot,
        float facingScoreWeight,
        float currentSelectionBonus,
        LayerMask counterLayerMask,
        BaseCounter currentSelection)
    {
        int colliderCount = Physics.OverlapSphereNonAlloc(
            playerPosition,
            interactionDistance,
            nearbyColliders,
            counterLayerMask,
            QueryTriggerInteraction.Ignore);

        Vector3 flatForward = Vector3.ProjectOnPlane(playerForward, Vector3.up).normalized;
        BaseCounter bestCounter = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidateCollider = nearbyColliders[i];
            BaseCounter candidateCounter = candidateCollider != null
                ? candidateCollider.GetComponentInParent<BaseCounter>()
                : null;
            if (candidateCounter == null)
                continue;

            // bounds.ClosestPoint 兼容非凸 MeshCollider；Collider.ClosestPoint 对这类柜台会持续报错。
            Vector3 closestPoint = candidateCollider.bounds.ClosestPoint(playerPosition);
            Vector3 toCandidate = Vector3.ProjectOnPlane(closestPoint - playerPosition, Vector3.up);
            float distance = toCandidate.magnitude;

            if (distance > interactionDistance)
                continue;

            float facingDot = distance > Mathf.Epsilon
                ? Vector3.Dot(flatForward, toCandidate / distance)
                : 1f;

            if (facingDot < minimumFacingDot)
                continue;

            if (!HasClearCounterPath(
                    playerPosition,
                    candidateCounter,
                    candidateCollider,
                    counterLayerMask))
                continue;

            float score = CalculateScore(
                distance,
                interactionDistance,
                facingDot,
                facingScoreWeight,
                candidateCounter == currentSelection,
                currentSelectionBonus);

            if (score > bestScore)
            {
                bestScore = score;
                bestCounter = candidateCounter;
            }
        }

        return bestCounter;
    }

    internal static float CalculateScore(
        float distance,
        float maximumDistance,
        float facingDot,
        float facingScoreWeight,
        bool isCurrentSelection,
        float currentSelectionBonus)
    {
        float distanceScore = 1f - Mathf.Clamp01(distance / maximumDistance);
        float score = facingDot * facingScoreWeight
            + distanceScore * (1f - facingScoreWeight);

        // 相邻柜台分数很接近时保留当前选择，避免高亮在每一帧来回闪烁。
        if (isCurrentSelection)
            score += currentSelectionBonus;

        return score;
    }

    private static bool HasClearCounterPath(
        Vector3 playerPosition,
        BaseCounter candidateCounter,
        Collider candidateCollider,
        LayerMask counterLayerMask)
    {
        // 从腰部高度检查视线，防止隔着另一个柜台选中后方目标。
        Vector3 rayOrigin = playerPosition + Vector3.up * 0.5f;
        Vector3 rayTarget = candidateCollider.bounds.ClosestPoint(rayOrigin);
        Vector3 rayDirection = rayTarget - rayOrigin;
        float rayDistance = rayDirection.magnitude;

        if (rayDistance <= Mathf.Epsilon)
            return true;

        if (!Physics.Raycast(
                rayOrigin,
                rayDirection / rayDistance,
                out RaycastHit hit,
                rayDistance + 0.05f,
                counterLayerMask,
                QueryTriggerInteraction.Ignore))
            return false;

        BaseCounter hitCounter = hit.collider.GetComponentInParent<BaseCounter>();
        return hitCounter == candidateCounter;
    }
}
