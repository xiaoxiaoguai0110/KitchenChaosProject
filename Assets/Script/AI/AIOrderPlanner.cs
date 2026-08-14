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

    public RecipeSO FindCompatibleOrder(PlateKitchenObject plate)
    {
        foreach (RecipeSO order in orderManager.GetOrderList())
        {
            bool canMatch = true;
            foreach (KitchenObjectSO plateIngredient in plate.GetKitchenObjectSOList())
            {
                if (!order.kitchenObjectSOList.Contains(plateIngredient))
                {
                    canMatch = false;
                    break;
                }
            }

            if (canMatch)
                return order;
        }
        return null;
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
            if (!(counter is ClearCounter clearCounter) || !clearCounter.IsHaveKitchenObject())
                continue;

            KitchenObject kitchenObject = clearCounter.GetKitchenObject();
            if (kitchenObject.TryGetComponent(out PlateKitchenObject plate))
            {
                foreach (KitchenObjectSO ingredient in plate.GetKitchenObjectSOList())
                    AddUnique(ingredients, ingredient);
            }
            else
            {
                AddUnique(ingredients, kitchenObject.GetKitchenObjectSO());
            }
        }
        return ingredients;
    }

    private KitchenObjectSO FindRawIngredient(KitchenObjectSO ingredient)
    {
        foreach (BaseCounter counter in counters)
        {
            if (counter is ContainerCounter container && container.KitchenObjectSO == ingredient)
                return ingredient;
        }

        if (cuttingRecipeList != null)
        {
            foreach (CuttingRecipe recipe in cuttingRecipeList.list)
            {
                if (recipe.output == ingredient)
                    return recipe.input;
            }
        }

        if (fryingRecipeList != null)
        {
            foreach (FryingRecipe recipe in fryingRecipeList.list)
            {
                if (recipe.output == ingredient)
                    return recipe.input;
            }
        }

        return null;
    }

    private static void AddUnique(List<KitchenObjectSO> ingredients, KitchenObjectSO ingredient)
    {
        if (!ingredients.Contains(ingredient))
            ingredients.Add(ingredient);
    }
}
