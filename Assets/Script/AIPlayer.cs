using System.Collections.Generic;
using UnityEngine;

public class AIPlayer : MonoBehaviour
{
    private enum AIState
    {
        Idle,
        MovingToTarget,
        Cutting,
        Waiting,
    }

    private Player player;
    private BaseCounter[] allCounters;
    private BaseCounter targetCounter;
    private bool isWalking;

    private AIState currentState = AIState.Idle;
    private float idleCooldown;
    private bool hasReceivedFirstOrder;

    private int cutCount;
    private int cutCountMax;
    private float actionCooldown;
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

        OrderManager.Instance.OnRecipeSpawned += OnFirstOrderSpawned;
    }

    private void OnFirstOrderSpawned(object sender, System.EventArgs e)
    {
        OrderManager.Instance.OnRecipeSpawned -= OnFirstOrderSpawned;
        hasReceivedFirstOrder = true;
        PickNewTarget();
        if (targetCounter != null)
            currentState = AIState.MovingToTarget;
    }

    private void Update()
    {
        if (GameManager.Instance.IsGamePlayingState() == false)
        {
            SetWalking(false);
            return;
        }

        switch (currentState)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.MovingToTarget: UpdateMovingToTarget(); break;
            case AIState.Cutting: UpdateCutting(); break;
            case AIState.Waiting: UpdateWaiting(); break;
        }
    }

    private void ChangeState(AIState newState)
    {
        currentState = newState;
    }

    private void UpdateIdle()
    {
        if (hasReceivedFirstOrder == false)
            return;

        if (idleCooldown > 0)
        {
            idleCooldown -= Time.deltaTime;
            return;
        }

        PickNewTarget();

        if (targetCounter != null)
            ChangeState(AIState.MovingToTarget);
    }

    private void UpdateMovingToTarget()
    {
        if (targetCounter == null)
        {
            ChangeState(AIState.Idle);
            return;
        }

        if (IsTargetCounterBlocked())
        {
            targetCounter = null;
            idleCooldown = 0;
            ChangeState(AIState.Idle);
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
            OnReachedTarget();
        }
    }

    private bool IsTargetCounterBlocked()
    {
        bool aiHasObject = player.IsHaveKitchenObject();
        bool counterHasObject = targetCounter.IsHaveKitchenObject();

        if (!aiHasObject || !counterHasObject)
            return false;

        bool aiHasPlate = player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject aiPlate);
        bool counterHasPlate = targetCounter.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject counterPlate);

        if (aiHasPlate)
        {
            KitchenObjectSO counterSO = targetCounter.GetKitchenObject().GetKitchenObjectSO();
            return !aiPlate.CanAddKitchenObjectSO(counterSO);
        }

        if (counterHasPlate)
        {
            KitchenObjectSO heldSO = player.GetKitchenObject().GetKitchenObjectSO();
            return !counterPlate.CanAddKitchenObjectSO(heldSO);
        }

        return true;
    }

    private void OnReachedTarget()
    {
        SetWalking(false);
        transform.forward = (targetCounter.transform.position - transform.position).normalized;

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

        bool hadObjectBeforeInteract = player.IsHaveKitchenObject();
        targetCounter.Interact(player);
        bool hasObjectAfterInteract = player.IsHaveKitchenObject();
        bool interactionChangedSomething = (hadObjectBeforeInteract != hasObjectAfterInteract);

        if (willCut)
        {
            actionCooldown = 0.5f;
            ChangeState(AIState.Cutting);
        }
        else if (willWait)
        {
            ChangeState(AIState.Waiting);
        }
        else if (interactionChangedSomething)
        {
            targetCounter = null;
            idleCooldown = 0.15f;
            ChangeState(AIState.Idle);
        }
        else
        {
            targetCounter = null;
            idleCooldown = 0.15f;
            ChangeState(AIState.Idle);
        }
    }

    private void UpdateCutting()
    {
        SetWalking(false);

        if (actionCooldown > 0)
        {
            actionCooldown -= Time.deltaTime;
            return;
        }

        targetCounter.InteractOperate(player);
        cutCount++;

        if (cutCount >= cutCountMax)
        {
            targetCounter.Interact(player);
            targetCounter = null;
            idleCooldown = 0.15f;
            ChangeState(AIState.Idle);
        }
        else
        {
            actionCooldown = 0.5f;
        }
    }

    private void UpdateWaiting()
    {
        waitTimer += Time.deltaTime;
        SetWalking(false);

        if (waitTimer >= waitDuration)
        {
            targetCounter.Interact(player);

            if (player.IsHaveKitchenObject())
            {
                waitTimer = 0f;
                targetCounter = null;
                idleCooldown = 0.5f;
                ChangeState(AIState.Idle);
            }
            else
            {
                idleCooldown = 0.3f;
                ChangeState(AIState.Idle);
            }
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

        List<KitchenObjectSO> existingIngredients = new List<KitchenObjectSO>();
        foreach (BaseCounter counter in allCounters)
        {
            if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject())
            {
                KitchenObject obj = clearCounter.GetKitchenObject();
                if (obj.TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plate))
                {
                    foreach (KitchenObjectSO ingredient in plate.GetKitchenObjectSOList())
                    {
                        if (!existingIngredients.Contains(ingredient))
                            existingIngredients.Add(ingredient);
                    }
                }
                else
                {
                    KitchenObjectSO so = obj.GetKitchenObjectSO();
                    if (!existingIngredients.Contains(so))
                        existingIngredients.Add(so);
                }
            }
        }

        List<KitchenObjectSO> missingIngredients = new List<KitchenObjectSO>();
        foreach (KitchenObjectSO needed in neededIngredients)
        {
            if (!existingIngredients.Contains(needed))
                missingIngredients.Add(needed);
        }

        if (missingIngredients.Count > 0)
        {
            List<KitchenObjectSO> rawIngredients = new List<KitchenObjectSO>();
            foreach (KitchenObjectSO missing in missingIngredients)
            {
                bool foundRaw = false;
                foreach (BaseCounter counter in allCounters)
                {
                    if (counter is ContainerCounter containerCounter && containerCounter.KitchenObjectSO == missing)
                    {
                        rawIngredients.Add(missing);
                        foundRaw = true;
                        break;
                    }
                }
                if (foundRaw) continue;

                if (cuttingRecipeList != null)
                {
                    foreach (CuttingRecipe recipe in cuttingRecipeList.list)
                    {
                        if (recipe.output == missing)
                        {
                            rawIngredients.Add(recipe.input);
                            foundRaw = true;
                            break;
                        }
                    }
                }
                if (foundRaw) continue;

                if (fryingRecipeList != null)
                {
                    foreach (FryingRecipe recipe in fryingRecipeList.list)
                    {
                        if (recipe.output == missing)
                        {
                            rawIngredients.Add(recipe.input);
                            break;
                        }
                    }
                }
            }

            foreach (BaseCounter counter in allCounters)
            {
                if (counter is ContainerCounter containerCounter
                    && rawIngredients.Contains(containerCounter.KitchenObjectSO))
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

        if (availableCounters.Count == 0)
        {
            foreach (BaseCounter counter in allCounters)
                if (counter is ContainerCounter)
                    availableCounters.Add(counter);
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

    private void SetWalking(bool walking)
    {
        isWalking = walking;
        player.SetIsWalking(walking);
    }
}
