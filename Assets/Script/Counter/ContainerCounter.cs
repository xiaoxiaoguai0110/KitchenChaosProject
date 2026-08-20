using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 食材容器柜台
public class ContainerCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    public KitchenObjectSO KitchenObjectSO => kitchenObjectSO;
    [SerializeField]private ContainerCounterVisual containerCounterVisiual;

    

    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())return;
        CreateKitchenObject(kitchenObjectSO.prefab);
        TransferKitchenObject(this, player);
        containerCounterVisiual.PlayOpen();

    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (player.IsHaveKitchenObject())
        {
            failureReason = "双手已占用，请先放下食材";
            return false;
        }

        failureReason = null;
        return true;
    }

    


}
