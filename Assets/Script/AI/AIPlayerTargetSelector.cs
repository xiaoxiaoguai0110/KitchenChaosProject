using System.Collections.Generic;
using UnityEngine;

internal sealed class AIPlayerTargetSelector
{
    private readonly Player player;
    private readonly BaseCounter[] counters;
    private readonly OrderManager orderManager;
    private readonly CuttingRecipeListSO cuttingRecipeList;
    private readonly FryingRecipeListSO fryingRecipeList;
    private readonly AIOrderPlanner orderPlanner;

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
        List<BaseCounter> availableCounters = new List<BaseCounter>();
        if (player.IsHaveKitchenObject())
            FindTargetsForHeldObject(availableCounters);
        else
            FindTargetsForEmptyHands(availableCounters);

        // 真人正在使用或刚刚导致寻路失败的柜台会被短暂排除，
        // 避免 AI 每帧重新选择同一个繁忙目标。
        if (excludedCounter != null)
            availableCounters.RemoveAll(counter => counter == excludedCounter);

        return availableCounters.Count == 0
            ? null
            : availableCounters[Random.Range(0, availableCounters.Count)];
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

    private void FindTargetsForEmptyHands(List<BaseCounter> availableCounters)
    {
        AddMatchingPlates(availableCounters);
        if (availableCounters.Count == 0)
            AddIngredientsOrPlates(availableCounters);
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
                availableCounters.Add(counter);
            }
        }
    }

    private void AddIngredientsOrPlates(List<BaseCounter> availableCounters)
    {
        List<KitchenObjectSO> missingIngredients = orderPlanner.GetMissingIngredients();
        if (missingIngredients.Count > 0)
        {
            List<KitchenObjectSO> rawIngredients = orderPlanner.GetRawIngredients(missingIngredients);
            foreach (BaseCounter counter in counters)
            {
                if (counter is ContainerCounter container
                    && rawIngredients.Contains(container.KitchenObjectSO))
                {
                    availableCounters.Add(counter);
                }
            }
        }
        else
        {
            AddCountersOfType<ContainerCounter>(availableCounters);
        }

        if (orderManager.GetOrderList().Count > 0 && HasUnplatedFood())
            AddCountersOfType<PlatesCounter>(availableCounters);

        if (availableCounters.Count == 0)
            AddCountersOfType<ContainerCounter>(availableCounters);
    }

    private bool HasUnplatedFood()
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is ClearCounter clearCounter
                && clearCounter.IsHaveKitchenObject()
                && !clearCounter.GetKitchenObject().TryGetComponent(out PlateKitchenObject _))
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

        RecipeSO targetOrder = orderPlanner.FindCompatibleOrder(plate);
        if (targetOrder != null)
        {
            List<KitchenObjectSO> neededIngredients = orderPlanner.GetMissingFromPlate(targetOrder, plate);
            foreach (BaseCounter counter in counters)
            {
                if (counter is ClearCounter clearCounter
                    && clearCounter.IsHaveKitchenObject()
                    && neededIngredients.Contains(clearCounter.GetKitchenObject().GetKitchenObjectSO()))
                {
                    availableCounters.Add(counter);
                }
            }
        }

        if (availableCounters.Count == 0)
            AddCountersOfType<ClearCounter>(availableCounters);
    }

    private void AddTargetsForIngredient(List<BaseCounter> availableCounters)
    {
        KitchenObjectSO heldObject = player.GetKitchenObject().GetKitchenObjectSO();
        if (cuttingRecipeList != null && cuttingRecipeList.TryGetCuttingRecipe(heldObject, out _))
        {
            AddCountersOfType<CuttingCounter>(availableCounters);
            return;
        }

        if (fryingRecipeList != null && fryingRecipeList.TryGetFryingRecipe(heldObject, out _))
        {
            AddCountersOfType<StoveCounter>(availableCounters);
            return;
        }

        foreach (BaseCounter counter in counters)
        {
            if (counter is ClearCounter clearCounter
                && clearCounter.IsHaveKitchenObject()
                && clearCounter.GetKitchenObject().TryGetComponent(out PlateKitchenObject _))
            {
                availableCounters.Add(counter);
                break;
            }
        }

        if (availableCounters.Count == 0)
            AddCountersOfType<ClearCounter>(availableCounters);
    }

    private void AddCountersOfType<T>(List<BaseCounter> availableCounters) where T : BaseCounter
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is T)
                availableCounters.Add(counter);
        }
    }
}
