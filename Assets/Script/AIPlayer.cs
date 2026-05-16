using System.Collections.Generic;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private enum AIActionType
    {
        None,
        Cutting,
        Waiting,
    }

    private Player player;
    private BaseCounter[] allCounters;
    private BaseCounter targetCounter;
    private float actionCooldown;
    private bool isWalking;

    private AIActionType currentAction = AIActionType.None;
    private int cutCount;
    private int cutCountMax;
    private float waitTimer;
    private float waitDuration;

    [SerializeField] private CuttingRecipeListSO cuttingRecipeList;
    [SerializeField] private FryingRecipeListSO fryingRecipeList;

    private void Awake()
    {
        player = GetComponent<Player>();
        player.enabled = false;
    }

    private void Start()
    {
        allCounters = FindObjectsOfType<BaseCounter>();

        CapsuleCollider aiCollider = GetComponent<CapsuleCollider>();
        Player player1 = Player.GetInstance(0);
        if (player1 != null)
        {
            CapsuleCollider p1Collider = player1.GetComponent<CapsuleCollider>();
            if (aiCollider != null && p1Collider != null)
                Physics.IgnoreCollision(aiCollider, p1Collider);
        }

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
            transform.forward = Vector3.RotateTowards(transform.forward, moveDir, 720f * Mathf.Deg2Rad * Time.deltaTime, 0f);
            SetWalking(true);
        }
        else
        {
            SetWalking(false);
            transform.forward = (targetCounter.transform.position - transform.position).normalized;

            if (currentAction == AIActionType.Cutting)
            {
                targetCounter.InteractOperate(player);
                cutCount++;
                actionCooldown = 0.5f;

                if (cutCount >= cutCountMax)
                    currentAction = AIActionType.None;
            }
            else if (currentAction == AIActionType.Waiting)
            {
                waitTimer += Time.deltaTime;
                SetWalking(false);

                if (waitTimer >= waitDuration)
                {
                    targetCounter.Interact(player);

                    if (player.IsHaveKitchenObject())
                    {
                        waitTimer = 0f;
                        currentAction = AIActionType.None;
                        actionCooldown = 0.5f;
                        PickNewTarget();
                    }
                    else
                    {
                        actionCooldown = 0.3f;
                    }
                }
            }
            else
            {
                bool willCut = false;
                bool willWait = false;

                if (targetCounter is CuttingCounter && player.IsHaveKitchenObject())
                {
                    KitchenObjectSO heldSO = player.GetKitchenObject().GetKitchenObjectSO();
                    if (cuttingRecipeList != null && cuttingRecipeList.TryGetCuttingRecipe(heldSO, out CuttingRecipe recipe))
                    {
                        cutCountMax = recipe.cuttingCountMax;
                        cutCount = 0;
                        willCut = true;
                    }
                }
                else if (targetCounter is StoveCounter && player.IsHaveKitchenObject())
                {
                    KitchenObjectSO heldSO = player.GetKitchenObject().GetKitchenObjectSO();
                    if (fryingRecipeList != null && fryingRecipeList.TryGetFryingRecipe(heldSO, out FryingRecipe recipe))
                    {
                        waitDuration = recipe.fryingTime;
                        waitTimer = 0f;
                        willWait = true;
                    }
                }

                targetCounter.Interact(player);
                actionCooldown = 0.8f;

                if (willCut)
                    currentAction = AIActionType.Cutting;
                else if (willWait)
                    currentAction = AIActionType.Waiting;
                else
                    PickNewTarget();
            }
        }
    }

    private void PickNewTarget()
    {
        List<BaseCounter> availableCounters = new List<BaseCounter>();

        if (player.IsHaveKitchenObject() == false)
        {
            foreach (BaseCounter counter in allCounters)
            {
                if (counter is ContainerCounter)
                    availableCounters.Add(counter);
            }
        }
        else
        {
            if (player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _))
            {
                foreach (BaseCounter counter in allCounters)
                {
                    if (counter is DeliveryCounter)
                        availableCounters.Add(counter);
                }
            }
            else
            {
                KitchenObjectSO heldSO = player.GetKitchenObject().GetKitchenObjectSO();

                if (cuttingRecipeList.TryGetCuttingRecipe(heldSO, out _))
                {
                    foreach (BaseCounter counter in allCounters)
                        if (counter is CuttingCounter)
                            availableCounters.Add(counter);
                }
                else if (fryingRecipeList.TryGetFryingRecipe(heldSO, out _))
                {
                    foreach (BaseCounter counter in allCounters)
                        if (counter is StoveCounter)
                            availableCounters.Add(counter);
                }
                else
                {
                    foreach (BaseCounter counter in allCounters)
                        if (counter is ClearCounter)
                            availableCounters.Add(counter);
                }
            }
        }
        targetCounter = availableCounters[Random.Range(0, availableCounters.Count)];
    }

    private void SetWalking(bool walking)
    {
        isWalking = walking;
        player.SetIsWalking(walking);
    }
}
