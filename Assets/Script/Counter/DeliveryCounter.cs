using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryCounter : BaseCounter
{
    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject()&&player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plateKitchenObject))
        {
            OrderManager.Instance.DeliveryRecipe(plateKitchenObject);
            player.DestroyKitchenObject();
        }
    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (!player.IsHaveKitchenObject()
            || !player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out _))
        {
            failureReason = "请端着完成的餐盘交付";
            return false;
        }

        failureReason = null;
        return true;
    }
}
