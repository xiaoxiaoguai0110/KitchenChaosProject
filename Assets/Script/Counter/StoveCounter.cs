using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoveCounter : BaseCounter
{
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
    private void Start()
    {
        warningControl = GetComponent<WarningControl>();   
    }

    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {//手上有食材
         // 获取玩家持有的 KitchenObjectSO
            KitchenObjectSO playerHeldSO = player.GetKitchenObject().GetKitchenObjectSO();

            //添加这行，打印出玩家持有的对象名称
            Debug.Log($"玩家持有: {playerHeldSO.name}", playerHeldSO);
            if (IsHaveKitchenObject() == false)
            {//当前柜台 为空

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
                Debug.Log("没有找到配方！");
            }
        }
        else
        {//手上没食材
            if (IsHaveKitchenObject())
            {//当前柜台 不为空
                TurnToIdle();
                TransferKitchenObject(this, player);
            }
            else
            {

            }
        }
    }

    public void Update()
    {
        switch (state)
        {
            case StoveState.Idle:
                break;
            case StoveState.Frying:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer/ fryingRecipe.fryingTime);
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestoryKitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);
                    state = StoveState.Burning;

                    burningRecipeList.TryGetFryingRecipe(GetKitchenObject().GetKitchenObjectSO(), out FryingRecipe newFryingRecipe);
                    StartBurning(newFryingRecipe);
                }
                break;
            case StoveState.Burning:
                fryingTimer += Time.deltaTime;
                progressBarUI.UpdateProgress(fryingTimer / fryingRecipe.fryingTime);

                float warningTimeNormalize = 0.5f;
                if (fryingTimer / fryingRecipe.fryingTime >= warningTimeNormalize)
                {
                    warningControl.ShowWarning();
                }
                if (fryingTimer >= fryingRecipe.fryingTime)
                {
                    DestoryKitchenObject();
                    CreateKitchenObject(fryingRecipe.output.prefab);
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
        stoveCounterVisual.ShowStoveEffect();
        sound.Play();
    }

    private void StartBurning(FryingRecipe fryingRecipe)
    {
        if(fryingRecipe == null)
        {
            Debug.Log("无法获取Burning的食谱，无法进行Burning");
            TurnToIdle();
            return;
        }
        stoveCounterVisual.ShowStoveEffect();
        fryingTimer = 0;
        this.fryingRecipe = fryingRecipe;
        state = StoveState.Burning;
        sound.Play();
    }

    private void TurnToIdle()
    {
        progressBarUI.Hide();
        state = StoveState.Idle;
        stoveCounterVisual.HideStoveEffect();
        sound.Pause();
        warningControl.StopWarning();
    }

}
