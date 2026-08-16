internal sealed class AIPlayerActionController
{
    private enum ActionType
    {
        None,
        Cutting,
        WaitingForStove,
    }

    private const float CutInterval = 0.5f;

    private readonly Player player;
    private readonly CuttingRecipeListSO cuttingRecipeList;
    private readonly FryingRecipeListSO fryingRecipeList;

    private ActionType currentAction;
    private BaseCounter counter;
    private int cutCount;
    private int cutCountMax;
    private float timer;
    private float waitDuration;

    public AIPlayerActionController(
        Player player,
        CuttingRecipeListSO cuttingRecipeList,
        FryingRecipeListSO fryingRecipeList)
    {
        this.player = player;
        this.cuttingRecipeList = cuttingRecipeList;
        this.fryingRecipeList = fryingRecipeList;
    }

    public bool Begin(BaseCounter targetCounter)
    {
        counter = targetCounter;
        currentAction = DetermineAction(targetCounter);
        targetCounter.Interact(player);
        return currentAction != ActionType.None;
    }

    public bool Tick(float deltaTime, out float idleCooldown)
    {
        idleCooldown = 0.15f;
        if (counter == null)
            return true;

        switch (currentAction)
        {
            case ActionType.Cutting:
                return TickCutting(deltaTime);
            case ActionType.WaitingForStove:
                return TickStove(deltaTime, out idleCooldown);
            default:
                return true;
        }
    }

    private ActionType DetermineAction(BaseCounter targetCounter)
    {
        timer = 0f;
        if (!player.IsHaveKitchenObject())
            return ActionType.None;

        KitchenObjectSO heldObject = player.GetKitchenObject().GetKitchenObjectSO();
        if (targetCounter is CuttingCounter
            && cuttingRecipeList != null
            && cuttingRecipeList.TryGetCuttingRecipe(heldObject, out CuttingRecipe cuttingRecipe))
        {
            cutCount = 0;
            cutCountMax = cuttingRecipe.cuttingCountMax;
            timer = CutInterval;
            return ActionType.Cutting;
        }

        if (targetCounter is StoveCounter
            && fryingRecipeList != null
            && fryingRecipeList.TryGetFryingRecipe(heldObject, out FryingRecipe fryingRecipe))
        {
            waitDuration = fryingRecipe.fryingTime;
            return ActionType.WaitingForStove;
        }

        return ActionType.None;
    }

    private bool TickCutting(float deltaTime)
    {
        if (timer > 0f)
        {
            timer -= deltaTime;
            return false;
        }

        counter.InteractOperate(player);
        cutCount++;
        if (cutCount < cutCountMax)
        {
            timer = CutInterval;
            return false;
        }

        counter.Interact(player);
        return true;
    }

    private bool TickStove(float deltaTime, out float idleCooldown)
    {
        idleCooldown = 0f;
        timer += deltaTime;
        if (timer < waitDuration)
            return false;

        counter.Interact(player);
        idleCooldown = player.IsHaveKitchenObject() ? 0.5f : 0.3f;
        return true;
    }
}
