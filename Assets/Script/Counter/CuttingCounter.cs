using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CuttingCounter : BaseCounter
{
    public static event EventHandler OnCut;

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

    private void Cut()
    {
        OnCut?.Invoke(this, EventArgs.Empty);
        cuttingCount++;
        cuttingCounterVisiual.PlayCut();
    }
    public static void ClearStaticData()
    {
        OnCut = null;
    }

}
