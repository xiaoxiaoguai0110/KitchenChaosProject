using System;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private enum AIState
    {
        Idle,
        Moving,
        Acting,
    }

    [SerializeField] private CuttingRecipeListSO cuttingRecipeList;
    [SerializeField] private FryingRecipeListSO fryingRecipeList;

    private Player player;
    private OrderManager orderManager;
    private AIPlayerTargetSelector targetSelector;
    private AIPlayerMovement movement;
    private AIPlayerActionController actionController;

    private AIState currentState = AIState.Idle;
    private BaseCounter targetCounter;
    private float idleCooldown;
    private bool hasReceivedFirstOrder;

    private void Awake()
    {
        player = GetComponent<Player>();
        player.enabled = false;
    }

    private void Start()
    {
        BaseCounter[] allCounters = FindObjectsOfType<BaseCounter>();
        orderManager = OrderManager.Instance;
        targetSelector = new AIPlayerTargetSelector(
            player,
            allCounters,
            orderManager,
            cuttingRecipeList,
            fryingRecipeList);
        movement = new AIPlayerMovement(transform, player);
        actionController = new AIPlayerActionController(player, cuttingRecipeList, fryingRecipeList);

        IgnoreHumanPlayerCollision();

        if (orderManager != null)
            orderManager.OnRecipeSpawned += OnFirstOrderSpawned;
    }

    private void OnDestroy()
    {
        if (orderManager != null)
            orderManager.OnRecipeSpawned -= OnFirstOrderSpawned;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGamePlayingState())
        {
            movement.Stop();
            return;
        }

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
        orderManager.OnRecipeSpawned -= OnFirstOrderSpawned;
        hasReceivedFirstOrder = true;
        SelectNextTarget();
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
            ChangeToIdle(0f);
            return;
        }

        if (movement.MoveTowards(targetCounter, Time.deltaTime))
            OnReachedTarget();
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
        targetCounter = targetSelector.PickTarget();
        if (targetCounter != null)
            currentState = AIState.Moving;
    }

    private void ChangeToIdle(float cooldown)
    {
        targetCounter = null;
        idleCooldown = cooldown;
        currentState = AIState.Idle;
    }

    private void IgnoreHumanPlayerCollision()
    {
        CharacterController aiController = GetComponent<CharacterController>();
        Player humanPlayer = Player.GetInstance(0);
        CharacterController humanController = humanPlayer != null
            ? humanPlayer.GetComponent<CharacterController>()
            : null;

        if (aiController != null && humanController != null)
            Physics.IgnoreCollision(aiController, humanController);
    }
}
