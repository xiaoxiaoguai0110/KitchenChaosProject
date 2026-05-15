using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private Player player;
    private BaseCounter[] allCounters;
    private BaseCounter targetCounter;
    private float actionCooldown;
    private bool isWalking;

    private void Awake()
    {
        player = GetComponent<Player>();
        player.enabled = false;
    }

    private void Start()
    {
        allCounters = FindObjectsOfType<BaseCounter>();
        PickNewTarget();
    }

    private void Update()
    {
        if (GameManager.Instance.IsGamePlayingState() == false)
        {
            SetWalking(false);
            return;
        }

        if (actionCooldown > 0)
        {
            actionCooldown -= Time.deltaTime;
            SetWalking(false);
            return;
        }

        if (targetCounter == null)
        {
            PickNewTarget();
            return;
        }

        Vector3 dir = (targetCounter.transform.position - transform.position);
        dir.y = 0;

        if (dir.magnitude > 1.8f)
        {
            Vector3 moveDir = dir.normalized;
            transform.position += moveDir * 5f * Time.deltaTime;
            transform.forward = Vector3.Slerp(transform.forward, moveDir, Time.deltaTime * 10f);
            SetWalking(true);
        }
        else
        {
            SetWalking(false);
            transform.forward = (targetCounter.transform.position - transform.position).normalized;
            targetCounter.Interact(player);
            actionCooldown = 0.8f;
            PickNewTarget();
        }
    }

    private void PickNewTarget()
    {
        if (allCounters == null || allCounters.Length == 0) return;
        targetCounter = allCounters[Random.Range(0, allCounters.Length)];
    }

    private void SetWalking(bool walking)
    {
        isWalking = walking;
        player.SetIsWalking(walking);
    }
}
