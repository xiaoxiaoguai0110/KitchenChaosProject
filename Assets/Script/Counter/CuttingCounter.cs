using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CuttingCounter : BaseCounter
{
    public static event EventHandler OnCut;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvent()
    {
        // 静态事件的生命周期不属于任何场景对象，因此在每次运行前主动清空。
        OnCut = null;
    }

    [SerializeField]private CuttingRecipeListSO cuttingRecipeList;
    [SerializeField]private ProgressBarUI progressBarUI;
    [SerializeField]private CuttingCounterVisual cuttingCounterVisiual;

    private int cuttingCount = 0;
    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {
            if (IsHaveKitchenObject() == false)
            {
                if (player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _))
                    return;
                cuttingCount = 0;
                TransferKitchenObject(player, this);
            }
        }
        else
        {
            if (IsHaveKitchenObject())
            {
                TransferKitchenObject(this, player);
                progressBarUI.Hide();
            }
        }
    }

    public override void InteractOperate(Player player)
    {
        if (IsHaveKitchenObject())
        {
            if (cuttingRecipeList.TryGetCuttingRecipe(GetKitchenObject().GetKitchenObjectSO(), out CuttingRecipe cuttingRecipe)) 
            {
                Cut();

                progressBarUI.UpdateProgress((float)cuttingCount / cuttingRecipe.cuttingCountMax);

                if(cuttingCount == cuttingRecipe.cuttingCountMax)
                {
                    DestroyKitchenObject();
                    CreateKitchenObject(cuttingRecipe.output.prefab);
                }

            }

            
        }
    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (player.IsHaveKitchenObject())
        {
            if (IsHaveKitchenObject())
            {
                failureReason = "切菜台上已经有食材";
                return false;
            }

            if (player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _))
            {
                failureReason = "盘子不能放在切菜台上";
                return false;
            }

            failureReason = null;
            return true;
        }

        if (!IsHaveKitchenObject())
        {
            failureReason = "切菜台是空的";
            return false;
        }

        failureReason = null;
        return true;
    }

    public override bool SupportsOperate()
    {
        return true;
    }

    public override bool CanOperate(Player player, out string failureReason)
    {
        if (!IsHaveKitchenObject())
        {
            failureReason = "请先把食材放到切菜台上";
            return false;
        }

        if (!cuttingRecipeList.TryGetCuttingRecipe(GetKitchenObjectSO(), out _))
        {
            failureReason = "这个食材不需要切";
            return false;
        }

        failureReason = null;
        return true;
    }

    private void Cut()
    {
        OnCut?.Invoke(this, EventArgs.Empty);
        cuttingCount++;
        cuttingCounterVisiual.PlayCut();
    }
}
