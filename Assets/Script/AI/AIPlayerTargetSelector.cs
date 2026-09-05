using System.Collections.Generic;
using UnityEngine;

internal sealed class AIPlayerTargetSelector
{
    private const float DeliveryPriority = 1100f;
    private const float ReadyPlatePriority = 1000f;
    private const float ProcessingPriority = 900f;
    private const float IngredientPickupPriority = 850f;
    private const float PlateMergePriority = 800f;
    private const float PlatePickupPriority = 700f;
    private const float IngredientSourcePriority = 650f;
    private const float EmptyCounterPriority = 400f;
    private const float ReservedTaskBonus = 100f;
    private const float DistancePenaltyPerMeter = 4f;

    private readonly Player player;
    private readonly BaseCounter[] counters;
    private readonly OrderManager orderManager;
    private readonly CuttingRecipeListSO cuttingRecipeList;
    private readonly FryingRecipeListSO fryingRecipeList;
    private readonly AIOrderPlanner orderPlanner;

    private RecipeSO reservedOrder;
    private KitchenObjectSO reservedIngredient;

    public AIPlayerTargetSelector(
        Player player,
        BaseCounter[] counters,
        OrderManager orderManager,
        CuttingRecipeListSO cuttingRecipeList,
        FryingRecipeListSO fryingRecipeList)
    {
        this.player = player;
        this.counters = counters;
        this.orderManager = orderManager;
        this.cuttingRecipeList = cuttingRecipeList;
        this.fryingRecipeList = fryingRecipeList;
        orderPlanner = new AIOrderPlanner(counters, orderManager, cuttingRecipeList, fryingRecipeList);
    }

    public BaseCounter PickTarget(BaseCounter excludedCounter = null)
    {
        RefreshReservation();
        List<BaseCounter> availableCounters = BuildCandidateList();

        // 真人正在使用或刚刚导致寻路失败的柜台会被短暂排除，
        // 避免 AI 每帧重新选择同一个繁忙目标。
        if (excludedCounter != null)
            availableCounters.RemoveAll(counter => counter == excludedCounter);

        if (availableCounters.Count == 0 && !player.IsHaveKitchenObject() && reservedIngredient != null)
        {
            // 预约食材找不到来源时释放任务，再尝试一次其他订单，避免 AI 永久等待无效目标。
            ClearReservation();
            availableCounters = BuildCandidateList();
            if (excludedCounter != null)
                availableCounters.RemoveAll(counter => counter == excludedCounter);
        }

        return SelectHighestScoringTarget(availableCounters);
    }

    public bool ShouldAbandonTarget(BaseCounter targetCounter)
    {
        if (targetCounter is not ContainerCounter
            || player.IsHaveKitchenObject()
            || reservedIngredient == null)
        {
            return false;
        }

        if (!orderPlanner.IsIngredientAvailableOrInProgress(reservedIngredient))
            return false;

        // AI 走向原料箱期间，若真人已经领取或开始加工同一食材，
        // 立即释放预约并重新规划，避免两人做出重复原料。
        ClearReservation();
        return true;
    }

    public bool IsBlocked(BaseCounter targetCounter)
    {
        if (!player.IsHaveKitchenObject() || !targetCounter.IsHaveKitchenObject())
            return false;

        bool playerHasPlate = player.GetKitchenObject().TryGetComponent(out PlateKitchenObject playerPlate);
        bool counterHasPlate = targetCounter.GetKitchenObject().TryGetComponent(out PlateKitchenObject counterPlate);

        if (playerHasPlate)
            return !playerPlate.CanAddKitchenObjectSO(targetCounter.GetKitchenObject().GetKitchenObjectSO());
        if (counterHasPlate)
            return !counterPlate.CanAddKitchenObjectSO(player.GetKitchenObject().GetKitchenObjectSO());
        return true;
    }

    private List<BaseCounter> BuildCandidateList()
    {
        List<BaseCounter> availableCounters = new List<BaseCounter>();
        if (player.IsHaveKitchenObject())
            FindTargetsForHeldObject(availableCounters);
        else
            FindTargetsForEmptyHands(availableCounters);
        return availableCounters;
    }

    private void FindTargetsForEmptyHands(List<BaseCounter> availableCounters)
    {
        AddMatchingPlates(availableCounters);
        if (availableCounters.Count > 0)
            return;

        // 已经放在柜台上的半成品优先继续加工，不再重新领取一份相同原料。
        AddProcessableLooseIngredients(availableCounters);
        if (availableCounters.Count > 0)
            return;

        if (EnsureReservation())
        {
            KitchenObjectSO rawIngredient = orderPlanner.GetRawIngredient(reservedIngredient);
            foreach (BaseCounter counter in counters)
            {
                if (counter is ContainerCounter container
                    && container.KitchenObjectSO == rawIngredient)
                {
                    AddCounterUnique(availableCounters, counter);
                }
            }

            if (availableCounters.Count > 0)
                return;
        }

        if (orderManager.GetOrderList().Count > 0 && HasUnplatedFood())
            AddCountersOfType<PlatesCounter>(availableCounters);
    }

    private void AddMatchingPlates(List<BaseCounter> availableCounters)
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is ClearCounter clearCounter
                && clearCounter.IsHaveKitchenObject()
                && clearCounter.GetKitchenObject().TryGetComponent(out PlateKitchenObject plate)
                && plate.GetKitchenObjectSOList().Count > 0
                && orderManager.IsPlateMatchingAnyOrder(plate))
            {
                AddCounterUnique(availableCounters, counter);
            }
        }
    }

    private void AddProcessableLooseIngredients(List<BaseCounter> availableCounters)
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is not ClearCounter clearCounter || !clearCounter.IsHaveKitchenObject())
                continue;

            KitchenObject kitchenObject = clearCounter.GetKitchenObject();
            KitchenObjectSO ingredient = kitchenObject.GetKitchenObjectSO();
            if (!kitchenObject.TryGetComponent(out PlateKitchenObject _)
                && CanProcess(ingredient)
                && orderPlanner.CanContributeToActiveOrder(ingredient))
            {
                AddCounterUnique(availableCounters, counter);
            }
        }
    }

    private bool HasUnplatedFood()
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is not ClearCounter clearCounter || !clearCounter.IsHaveKitchenObject())
                continue;

            KitchenObject kitchenObject = clearCounter.GetKitchenObject();
            KitchenObjectSO ingredient = kitchenObject.GetKitchenObjectSO();
            if (!kitchenObject.TryGetComponent(out PlateKitchenObject _)
                && !CanProcess(ingredient)
                && orderPlanner.CanContributeToActiveOrder(ingredient))
            {
                return true;
            }
        }
        return false;
    }

    private void FindTargetsForHeldObject(List<BaseCounter> availableCounters)
    {
        if (player.GetKitchenObject().TryGetComponent(out PlateKitchenObject plate))
            AddTargetsForPlate(availableCounters, plate);
        else
            AddTargetsForIngredient(availableCounters);
    }

    private void AddTargetsForPlate(List<BaseCounter> availableCounters, PlateKitchenObject plate)
    {
        if (plate.GetKitchenObjectSOList().Count > 0 && orderManager.IsPlateMatchingAnyOrder(plate))
        {
            AddCountersOfType<DeliveryCounter>(availableCounters);
            return;
        }

        RecipeSO targetOrder = reservedOrder;
        if (!orderPlanner.IsPlateCompatibleWithOrder(targetOrder, plate))
            targetOrder = orderPlanner.FindCompatibleOrder(plate);

        if (targetOrder != null)
        {
            List<KitchenObjectSO> neededIngredients = orderPlanner.GetMissingFromPlate(targetOrder, plate);
            foreach (BaseCounter counter in counters)
            {
                if (counter is ClearCounter clearCounter
                    && clearCounter.IsHaveKitchenObject()
                    && neededIngredients.Contains(clearCounter.GetKitchenObject().GetKitchenObjectSO()))
                {
                    AddCounterUnique(availableCounters, counter);
                }
            }
        }

        if (availableCounters.Count == 0)
            AddEmptyClearCounters(availableCounters);
    }

    private void AddTargetsForIngredient(List<BaseCounter> availableCounters)
    {
        KitchenObjectSO heldObject = player.GetKitchenObject().GetKitchenObjectSO();
        if (cuttingRecipeList != null && cuttingRecipeList.TryGetCuttingRecipe(heldObject, out _))
        {
            AddEmptyCountersOfType<CuttingCounter>(availableCounters);
            return;
        }

        if (fryingRecipeList != null && fryingRecipeList.TryGetFryingRecipe(heldObject, out _))
        {
            AddEmptyCountersOfType<StoveCounter>(availableCounters);
            return;
        }

        foreach (BaseCounter counter in counters)
        {
            if (counter is ClearCounter clearCounter
                && clearCounter.IsHaveKitchenObject()
                && clearCounter.GetKitchenObject().TryGetComponent(out PlateKitchenObject plate)
                && plate.CanAddKitchenObjectSO(heldObject))
            {
                AddCounterUnique(availableCounters, counter);
            }
        }

        if (availableCounters.Count == 0)
            AddEmptyClearCounters(availableCounters);
    }

    private bool EnsureReservation()
    {
        if (reservedOrder != null && reservedIngredient != null)
            return true;

        RecipeSO order = orderPlanner.FindOrderNeedingWork();
        if (order == null)
            return false;

        KitchenObjectSO ingredient = orderPlanner.FindHighestPriorityIngredient(
            orderPlanner.GetMissingIngredients(order));
        if (ingredient == null)
            return false;

        // 预约保存“为哪张订单做哪种最终食材”，让 AI 在多次移动/加工之间保持同一目标。
        reservedOrder = order;
        reservedIngredient = ingredient;
        return true;
    }

    private void RefreshReservation()
    {
        if (reservedOrder == null || reservedIngredient == null)
            return;

        bool orderEnded = !orderPlanner.IsOrderActive(reservedOrder);
        bool workAlreadyCovered = !player.IsHaveKitchenObject()
            && orderPlanner.IsIngredientAvailableOrInProgress(reservedIngredient);
        if (orderEnded || workAlreadyCovered)
            ClearReservation();
    }

    private void ClearReservation()
    {
        reservedOrder = null;
        reservedIngredient = null;
    }

    private bool CanProcess(KitchenObjectSO ingredient)
    {
        return (cuttingRecipeList != null && cuttingRecipeList.TryGetCuttingRecipe(ingredient, out _))
            || (fryingRecipeList != null && fryingRecipeList.TryGetFryingRecipe(ingredient, out _));
    }

    private BaseCounter SelectHighestScoringTarget(List<BaseCounter> availableCounters)
    {
        BaseCounter bestCounter = null;
        float bestScore = float.NegativeInfinity;
        foreach (BaseCounter counter in availableCounters)
        {
            float score = GetTargetScore(counter);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestCounter = counter;
        }
        return bestCounter;
    }

    private float GetTargetScore(BaseCounter counter)
    {
        float score = EmptyCounterPriority;
        if (counter is DeliveryCounter)
            score = DeliveryPriority;
        else if (counter is CuttingCounter || counter is StoveCounter)
            score = ProcessingPriority;
        else if (counter is PlatesCounter)
            score = PlatePickupPriority;
        else if (counter is ContainerCounter container)
        {
            score = IngredientSourcePriority;
            if (reservedIngredient != null
                && container.KitchenObjectSO == orderPlanner.GetRawIngredient(reservedIngredient))
            {
                score += ReservedTaskBonus;
            }
        }
        else if (counter is ClearCounter clearCounter && clearCounter.IsHaveKitchenObject())
        {
            KitchenObject kitchenObject = clearCounter.GetKitchenObject();
            if (kitchenObject.TryGetComponent(out PlateKitchenObject plate))
            {
                score = orderManager.IsPlateMatchingAnyOrder(plate)
                    ? ReadyPlatePriority
                    : PlateMergePriority;
            }
            else
            {
                score = IngredientPickupPriority;
            }
        }

        // 同类目标才按路程微调；高价值行为（交付、加工）不会被最近的空柜台抢走。
        Vector3 offset = counter.transform.position - player.transform.position;
        offset.y = 0f;
        return score - offset.magnitude * DistancePenaltyPerMeter;
    }

    private void AddEmptyClearCounters(List<BaseCounter> availableCounters)
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is ClearCounter && !counter.IsHaveKitchenObject())
                AddCounterUnique(availableCounters, counter);
        }
    }

    private void AddCountersOfType<T>(List<BaseCounter> availableCounters) where T : BaseCounter
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is T)
                AddCounterUnique(availableCounters, counter);
        }
    }

    private void AddEmptyCountersOfType<T>(List<BaseCounter> availableCounters) where T : BaseCounter
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is T && !counter.IsHaveKitchenObject())
                AddCounterUnique(availableCounters, counter);
        }
    }

    private static void AddCounterUnique(List<BaseCounter> availableCounters, BaseCounter counter)
    {
        if (counter != null && !availableCounters.Contains(counter))
            availableCounters.Add(counter);
    }
}
