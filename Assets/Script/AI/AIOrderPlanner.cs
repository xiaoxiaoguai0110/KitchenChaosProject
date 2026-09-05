using System.Collections.Generic;

internal sealed class AIOrderPlanner
{
    private readonly BaseCounter[] counters;
    private readonly OrderManager orderManager;
    private readonly CuttingRecipeListSO cuttingRecipeList;
    private readonly FryingRecipeListSO fryingRecipeList;

    public AIOrderPlanner(
        BaseCounter[] counters,
        OrderManager orderManager,
        CuttingRecipeListSO cuttingRecipeList,
        FryingRecipeListSO fryingRecipeList)
    {
        this.counters = counters;
        this.orderManager = orderManager;
        this.cuttingRecipeList = cuttingRecipeList;
        this.fryingRecipeList = fryingRecipeList;
    }

    public List<KitchenObjectSO> GetMissingIngredients()
    {
        List<KitchenObjectSO> needed = GetNeededIngredients();
        List<KitchenObjectSO> existing = GetExistingIngredients();
        needed.RemoveAll(existing.Contains);
        return needed;
    }

    public List<KitchenObjectSO> GetMissingIngredients(RecipeSO order)
    {
        List<KitchenObjectSO> needed = new List<KitchenObjectSO>();
        if (order == null)
            return needed;

        foreach (KitchenObjectSO ingredient in order.kitchenObjectSOList)
            AddUnique(needed, ingredient);

        List<KitchenObjectSO> existing = GetExistingIngredients();
        needed.RemoveAll(existing.Contains);
        return needed;
    }

    public RecipeSO FindOrderNeedingWork()
    {
        // 订单列表按生成顺序保存，优先完成最早出现且仍缺食材的订单。
        foreach (RecipeSO order in orderManager.GetOrderList())
        {
            if (GetMissingIngredients(order).Count > 0)
                return order;
        }

        return null;
    }

    public KitchenObjectSO FindHighestPriorityIngredient(List<KitchenObjectSO> ingredients)
    {
        KitchenObjectSO bestIngredient = null;
        int bestPriority = int.MinValue;
        foreach (KitchenObjectSO ingredient in ingredients)
        {
            int priority = GetProcessingPriority(ingredient);
            if (priority <= bestPriority)
                continue;

            bestPriority = priority;
            bestIngredient = ingredient;
        }

        return bestIngredient;
    }

    public List<KitchenObjectSO> GetRawIngredients(List<KitchenObjectSO> ingredients)
    {
        List<KitchenObjectSO> rawIngredients = new List<KitchenObjectSO>();
        foreach (KitchenObjectSO ingredient in ingredients)
        {
            KitchenObjectSO rawIngredient = FindRawIngredient(ingredient);
            if (rawIngredient != null && !rawIngredients.Contains(rawIngredient))
                rawIngredients.Add(rawIngredient);
        }
        return rawIngredients;
    }

    public KitchenObjectSO GetRawIngredient(KitchenObjectSO ingredient)
    {
        return FindRawIngredient(ingredient);
    }

    public bool IsIngredientAvailableOrInProgress(KitchenObjectSO ingredient)
    {
        return ingredient != null && GetExistingIngredients().Contains(ingredient);
    }

    public bool IsOrderActive(RecipeSO order)
    {
        return order != null && orderManager.GetOrderList().Contains(order);
    }

    public bool CanContributeToActiveOrder(KitchenObjectSO ingredient)
    {
        List<KitchenObjectSO> possibleStages = new List<KitchenObjectSO>();
        AddIngredientAndPotentialOutputs(possibleStages, ingredient);

        foreach (RecipeSO order in orderManager.GetOrderList())
        {
            foreach (KitchenObjectSO stage in possibleStages)
            {
                if (order.kitchenObjectSOList.Contains(stage))
                    return true;
            }
        }

        return false;
    }

    public RecipeSO FindCompatibleOrder(PlateKitchenObject plate)
    {
        foreach (RecipeSO order in orderManager.GetOrderList())
        {
            if (IsPlateCompatibleWithOrder(order, plate))
                return order;
        }
        return null;
    }

    public bool IsPlateCompatibleWithOrder(RecipeSO order, PlateKitchenObject plate)
    {
        if (order == null || plate == null)
            return false;

        foreach (KitchenObjectSO plateIngredient in plate.GetKitchenObjectSOList())
        {
            if (!order.kitchenObjectSOList.Contains(plateIngredient))
                return false;
        }

        return true;
    }

    public List<KitchenObjectSO> GetMissingFromPlate(RecipeSO order, PlateKitchenObject plate)
    {
        List<KitchenObjectSO> ingredients = new List<KitchenObjectSO>();
        foreach (KitchenObjectSO ingredient in order.kitchenObjectSOList)
        {
            if (!plate.GetKitchenObjectSOList().Contains(ingredient))
                ingredients.Add(ingredient);
        }
        return ingredients;
    }

    private List<KitchenObjectSO> GetNeededIngredients()
    {
        List<KitchenObjectSO> ingredients = new List<KitchenObjectSO>();
        foreach (RecipeSO order in orderManager.GetOrderList())
        {
            foreach (KitchenObjectSO ingredient in order.kitchenObjectSOList)
            {
                if (!ingredients.Contains(ingredient))
                    ingredients.Add(ingredient);
            }
        }
        return ingredients;
    }

    private List<KitchenObjectSO> GetExistingIngredients()
    {
        List<KitchenObjectSO> ingredients = new List<KitchenObjectSO>();
        foreach (BaseCounter counter in counters)
        {
            if (counter == null || !counter.IsHaveKitchenObject())
                continue;

            AddKitchenObjectProgress(ingredients, counter.GetKitchenObject());
        }

        // 真人或 AI 手里拿着的原料也属于“正在处理”，否则双方会重复领取同一种食材。
        for (int playerIndex = 0; playerIndex < 2; playerIndex++)
        {
            Player scenePlayer = Player.GetInstance(playerIndex);
            if (scenePlayer != null && scenePlayer.IsHaveKitchenObject())
                AddKitchenObjectProgress(ingredients, scenePlayer.GetKitchenObject());
        }

        return ingredients;
    }

    private void AddKitchenObjectProgress(
        List<KitchenObjectSO> ingredients,
        KitchenObject kitchenObject)
    {
        if (kitchenObject == null)
            return;

        if (kitchenObject.TryGetComponent(out PlateKitchenObject plate))
        {
            foreach (KitchenObjectSO plateIngredient in plate.GetKitchenObjectSOList())
                AddUnique(ingredients, plateIngredient);
            return;
        }

        // 生肉正在炉子上时，订单需要的熟肉已经有人负责，因此把可加工出的阶段也计入。
        AddIngredientAndPotentialOutputs(ingredients, kitchenObject.GetKitchenObjectSO());
    }

    private void AddIngredientAndPotentialOutputs(
        List<KitchenObjectSO> ingredients,
        KitchenObjectSO firstIngredient)
    {
        if (firstIngredient == null)
            return;

        Queue<KitchenObjectSO> pending = new Queue<KitchenObjectSO>();
        HashSet<KitchenObjectSO> visited = new HashSet<KitchenObjectSO>();
        pending.Enqueue(firstIngredient);

        while (pending.Count > 0)
        {
            KitchenObjectSO ingredient = pending.Dequeue();
            if (ingredient == null || !visited.Add(ingredient))
                continue;

            AddUnique(ingredients, ingredient);

            if (cuttingRecipeList != null)
            {
                foreach (CuttingRecipe recipe in cuttingRecipeList.list)
                {
                    if (recipe.input == ingredient)
                        pending.Enqueue(recipe.output);
                }
            }

            if (fryingRecipeList != null)
            {
                foreach (FryingRecipe recipe in fryingRecipeList.list)
                {
                    if (recipe.input == ingredient)
                        pending.Enqueue(recipe.output);
                }
            }
        }
    }

    private KitchenObjectSO FindRawIngredient(KitchenObjectSO ingredient)
    {
        HashSet<KitchenObjectSO> visited = new HashSet<KitchenObjectSO>();
        KitchenObjectSO currentIngredient = ingredient;

        while (currentIngredient != null && visited.Add(currentIngredient))
        {
            foreach (BaseCounter counter in counters)
            {
                if (counter is ContainerCounter container
                    && container.KitchenObjectSO == currentIngredient)
                {
                    return currentIngredient;
                }
            }

            KitchenObjectSO previousStage = null;
            if (cuttingRecipeList != null)
            {
                foreach (CuttingRecipe recipe in cuttingRecipeList.list)
                {
                    if (recipe.output == currentIngredient)
                    {
                        previousStage = recipe.input;
                        break;
                    }
                }
            }

            if (previousStage == null && fryingRecipeList != null)
            {
                foreach (FryingRecipe recipe in fryingRecipeList.list)
                {
                    if (recipe.output == currentIngredient)
                    {
                        previousStage = recipe.input;
                        break;
                    }
                }
            }

            currentIngredient = previousStage;
        }

        return null;
    }

    private int GetProcessingPriority(KitchenObjectSO ingredient)
    {
        if (fryingRecipeList != null)
        {
            foreach (FryingRecipe recipe in fryingRecipeList.list)
            {
                if (recipe.output == ingredient)
                    return 30;
            }
        }

        if (cuttingRecipeList != null)
        {
            foreach (CuttingRecipe recipe in cuttingRecipeList.list)
            {
                if (recipe.output == ingredient)
                    return 20;
            }
        }

        return 10;
    }

    private static void AddUnique(List<KitchenObjectSO> ingredients, KitchenObjectSO ingredient)
    {
        if (!ingredients.Contains(ingredient))
            ingredients.Add(ingredient);
    }
}
