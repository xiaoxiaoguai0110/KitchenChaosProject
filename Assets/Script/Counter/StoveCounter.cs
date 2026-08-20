using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter
{
    public enum StoveFeedbackType
    {
        CookingCompleted,
        BurnWarning,
        Burned
    }

    public sealed class StoveFeedbackEventArgs : System.EventArgs
    {
        public StoveFeedbackType FeedbackType { get; }

        public StoveFeedbackEventArgs(StoveFeedbackType feedbackType)
        {
            FeedbackType = feedbackType;
        }
    }

    public event System.EventHandler<StoveFeedbackEventArgs> OnFeedback;

    [SerializeField]private FryingRecipeListSO fryingRecipeList;
    [SerializeField] private FryingRecipeListSO burningRecipeList;
    [SerializeField]private StoveCounterVisual stoveCounterVisual;
    [SerializeField]private ProgressBarUI progressBarUI;

    [SerializeField]private AudioSource sound;

    public enum StoveState
    {
        Idle,
        Frying,
        Burning
    }

    private FryingRecipe fryingRecipe;
    private float fryingTimer = 0;
    private StoveState state = StoveState.Idle;
    private WarningControl warningControl;
    private bool burnWarningRaised;
    private void Start()
    {
        warningControl = GetComponent<WarningControl>();   
    }

    public override void Interact(Player player)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

        if (player.IsHaveKitchenObject())
        {// 如果玩家有食物
            // 获取玩家持有的 KitchenObjectSO
            KitchenObjectSO playerHeldSO = player.GetKitchenObject().GetKitchenObjectSO();

            // 测试输出，打印玩家持有的对象名称
            Debug.Log($"玩家持有: {playerHeldSO.name}", playerHeldSO);
            if (IsHaveKitchenObject() == false)
            {// 当前灶台为空

                if (fryingRecipeList.TryGetFryingRecipe(player.GetKitchenObject().GetKitchenObjectSO(), out FryingRecipe fryingRecipe))
                {
                    TransferKitchenObject(player, this);
                    StartFrying(fryingRecipe);
                }
                else if(burningRecipeList.TryGetFryingRecipe(player.GetKitchenObject().GetKitchenObjectSO(), out FryingRecipe burningRecipe))
                {
                    TransferKitchenObject(player, this);
                    StartBurning(burningRecipe);
                }
                else
                {

                }
                
            }
            else
            {
                Debug.Log("没有找到配方");
            }
        }
        else
        {// 玩家没有食物
            if (IsHaveKitchenObject())
            {// 当前灶台不为空
                TurnToIdle();
                TransferKitchenObject(this, player);
            }
            else
            {

            }
        }
    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (player.IsHaveKitchenObject())
        {
            if (IsHaveKitchenObject())
            {
                failureReason = "炉灶上已经有食材";
                return false;
            }

            KitchenObjectSO heldObject = player.GetKitchenObjectSO();
            bool canFry = fryingRecipeList.TryGetFryingRecipe(heldObject, out _);
            bool canBurn = burningRecipeList.TryGetFryingRecipe(heldObject, out _);
            if (!canFry && !canBurn)
            {
                failureReason = "这个食材不能放到炉灶上";
                return false;
            }

            failureReason = null;
            return true;
        }

        if (!IsHaveKitchenObject())
        {
            failureReason = "炉灶是空的";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void Update()
    {
        // 炉灶是后台计时系统，必须显式服从游戏状态，不能只依赖 Time.timeScale。
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

        switch (state)
        {
            case StoveState.Idle:
                break;
            case StoveState.Frying:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer/ fryingRecipe.fryingTime);
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestroyKitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);
                    RaiseFeedback(StoveFeedbackType.CookingCompleted);

                    burningRecipeList.TryGetFryingRecipe(GetKitchenObject().GetKitchenObjectSO(), out FryingRecipe newFryingRecipe);
                    StartBurning(newFryingRecipe);
                }
                break;
            case StoveState.Burning:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer / fryingRecipe.fryingTime);

                float warningTimeNormalize = 0.5f;
                if (!burnWarningRaised && fryingTimer / fryingRecipe.fryingTime >= warningTimeNormalize)
                {
                    burnWarningRaised = true;
                    warningControl.ShowWarning();
                    RaiseFeedback(StoveFeedbackType.BurnWarning);
                }
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestroyKitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);
                    RaiseFeedback(StoveFeedbackType.Burned);
                    TurnToIdle();
                }
                
                break;
            default:
                break;
        }
    }

    private void StartFrying(FryingRecipe fryingRecipe)
    {
        fryingTimer = 0;
        this.fryingRecipe = fryingRecipe;
        state = StoveState.Frying;
        burnWarningRaised = false;
        stoveCounterVisual.ShowStoveEffect();
        sound.Play();
    }

    private void StartBurning(FryingRecipe fryingRecipe)
    {
        if(fryingRecipe == null)
        {
            Debug.Log("无法获取 Burning 的食谱，无法开始 Burning");
            TurnToIdle();
            return;
        }
        stoveCounterVisual.ShowStoveEffect();
        fryingTimer = 0;
        this.fryingRecipe = fryingRecipe;
        state = StoveState.Burning;
        burnWarningRaised = false;
        sound.Play();
    }

    private void TurnToIdle()
    {
        progressBarUI.Hide();
        state = StoveState.Idle;
        burnWarningRaised = false;
        stoveCounterVisual.HideStoveEffect();
        sound.Pause();
        warningControl.StopWarning();
    }

    private void RaiseFeedback(StoveFeedbackType feedbackType)
    {
        OnFeedback?.Invoke(this, new StoveFeedbackEventArgs(feedbackType));
    }

}
