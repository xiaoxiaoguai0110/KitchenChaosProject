using System.Collections.Generic;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private enum AIActionType
    {
        None,
        Cutting,
    }

    private Player player;
    private BaseCounter[] allCounters;
    private BaseCounter targetCounter;
    private float actionCooldown;
    private bool isWalking;

    private AIActionType currentAction = AIActionType.None;
    private int cutCount;
    private int cutCountMax;

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
            else
            {
                bool willCut = false;
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

                targetCounter.Interact(player);
                actionCooldown = 0.8f;

                if (willCut)
                    currentAction = AIActionType.Cutting;
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
