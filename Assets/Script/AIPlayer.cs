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
            HandleActionAtCounter();
        }
    }

    private void PickNewTarget()
    {
        List<BaseCounter> availableCounters = new List<BaseCounter>();

        if (player.IsHaveKitchenObject() == false)
        {
            PickTargetWhenEmptyHanded(availableCounters);
        }
        else
        {
            PickTargetWhenHoldingObject(availableCounters);
        }

        if (availableCounters.Count == 0)
        {
            targetCounter = null;
            return;
        }

        targetCounter = availableCounters[Random.Range(0, availableCounters.Count)];
    }

    private void PickTargetWhenEmptyHanded(List<BaseCounter> availableCounters)
    {
        FindCountersWithPlatedFood(availableCounters);

        if (availableCounters.Count == 0)
        {
            PickIngredientsOrPlates(availableCounters);
        }
    }

    private void FindCountersWithPlatedFood(List<BaseCounter> availableCounters)
    {
        foreach (BaseCounter counter in allCounters)
        {
            if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject()
                && clearCounter.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plateOnCounter)
                && plateOnCounter.GetKitchenObjectSOList().Count > 0
                && OrderManager.Instance.IsPlateMatchingAnyOrder(plateOnCounter))
            {
                availableCounters.Add(counter);
            }
        }
    }

    private void PickIngredientsOrPlates(List<BaseCounter> availableCounters)
    {
        List<KitchenObjectSO> neededIngredients = new List<KitchenObjectSO>();
        foreach (RecipeSO order in OrderManager.Instance.GetOrderList())
        {
            foreach (KitchenObjectSO ingredient in order.kitchenObjectSOList)
            {
                if (!neededIngredients.Contains(ingredient))
                    neededIngredients.Add(ingredient);
            }
        }

        if (neededIngredients.Count > 0)
        {
            foreach (BaseCounter counter in allCounters)
            {
                if (counter is ContainerCounter containerCounter
                    && neededIngredients.Contains(containerCounter.KitchenObjectSO))
                {
                    availableCounters.Add(counter);
                }
            }
        }
        else
        {
            foreach (BaseCounter counter in allCounters)
                if (counter is ContainerCounter)
                    availableCounters.Add(counter);
        }

        if (OrderManager.Instance.GetOrderList().Count > 0)
        {
            bool hasFoodToPlate = false;
            foreach (BaseCounter counter in allCounters)
            {
                if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject()
                    && clearCounter.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _) == false)
                {
                    hasFoodToPlate = true;
                    break;
                }
            }

            if (hasFoodToPlate)
            {
                foreach (BaseCounter counter in allCounters)
                    if (counter is PlatesCounter)
                        availableCounters.Add(counter);
            }
        }
    }

    private void PickTargetWhenHoldingObject(List<BaseCounter> availableCounters)
    {
        if (player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plate))
        {
            PickTargetForHeldPlate(availableCounters, plate);
        }
        else
        {
            PickTargetForHeldIngredient(availableCounters);
        }
    }

    private void PickTargetForHeldPlate(List<BaseCounter> availableCounters, PlateKitchenObject plate)
    {
        if (plate.GetKitchenObjectSOList().Count > 0
            && OrderManager.Instance.IsPlateMatchingAnyOrder(plate))
        {
            foreach (BaseCounter counter in allCounters)
                if (counter is DeliveryCounter)
                    availableCounters.Add(counter);
        }
        else
        {
            foreach (BaseCounter counter in allCounters)
            {
                if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject())
                    availableCounters.Add(counter);
            }

            if (availableCounters.Count == 0)
            {
                foreach (BaseCounter counter in allCounters)
                    if (counter is ClearCounter)
                        availableCounters.Add(counter);
            }
        }
    }

    private void PickTargetForHeldIngredient(List<BaseCounter> availableCounters)
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
            {
                if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject()
                    && clearCounter.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _))
                {
                    availableCounters.Add(counter);
                    break;
                }
            }

            if (availableCounters.Count == 0)
            {
                foreach (BaseCounter counter in allCounters)
                    if (counter is ClearCounter)
                        availableCounters.Add(counter);
            }
        }
    }

    private void HandleActionAtCounter()
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

    private void SetWalking(bool walking)
    {
        isWalking = walking;
        player.SetIsWalking(walking);
    }
}
