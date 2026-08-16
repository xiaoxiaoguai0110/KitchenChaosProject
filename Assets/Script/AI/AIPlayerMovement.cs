using UnityEngine;

internal sealed class AIPlayerMovement
{
    private const float StopDistance = 1.8f;
    private const float MoveSpeed = 5f;
    private const float RotationSpeed = 720f;

    private readonly Transform transform;
    private readonly Player player;

    public AIPlayerMovement(Transform transform, Player player)
    {
        this.transform = transform;
        this.player = player;
    }

    public bool MoveTowards(BaseCounter counter, float deltaTime)
    {
        Vector3 direction = counter.transform.position - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= StopDistance)
            return true;

        Vector3 moveDirection = direction.normalized;
        player.Move(moveDirection * MoveSpeed * deltaTime);
        transform.forward = Vector3.RotateTowards(
            transform.forward,
            moveDirection,
            RotationSpeed * Mathf.Deg2Rad * deltaTime,
            0f);
        player.SetIsWalking(true);
        return false;
    }

    public void Face(BaseCounter counter)
    {
        Vector3 direction = counter.transform.position - transform.position;
        if (direction != Vector3.zero)
            transform.forward = direction.normalized;
    }

    public void Stop()
    {
        player.SetIsWalking(false);
    }
}
