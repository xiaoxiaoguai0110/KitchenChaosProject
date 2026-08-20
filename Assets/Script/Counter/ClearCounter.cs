using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ClearCounter : BaseCounter
{
    public override bool CanInteract(Player player, out string failureReason)
    {
        if (!player.IsHaveKitchenObject() && !IsHaveKitchenObject())
        {
            failureReason = "柜台是空的";
            return false;
        }

        if (!player.IsHaveKitchenObject() || !IsHaveKitchenObject())
        {
            failureReason = null;
            return true;
        }

        if (player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject heldPlate))
        {
            if (heldPlate.CanAddKitchenObjectSO(GetKitchenObjectSO()))
            {
                failureReason = null;
                return true;
            }

            failureReason = "这个食材不能加入当前餐盘";
            return false;
        }

        if (GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject counterPlate))
        {
            if (counterPlate.CanAddKitchenObjectSO(player.GetKitchenObjectSO()))
            {
                failureReason = null;
                return true;
            }

            failureReason = "这个食材不能加入当前餐盘";
            return false;
        }

        failureReason = "请先腾出一个位置";
        return false;
    }

    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {// 玩家手中有物品

            if(player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plateKitchenObject))
            {// 玩家拿着盘子
                if (IsHaveKitchenObject() == false)
                {// 当前柜台为空
                    TransferKitchenObject(player, this);
                }
                else
                {// 当前柜台不为空

                    bool isSuccess=plateKitchenObject.AddKitchenObjectSO(GetKitchenObjectSO());
                    if (isSuccess)
                    {
                        DestroyKitchenObject();
                    }
                }
            }
            else
            {// 玩家拿着普通食材
                if (IsHaveKitchenObject() == false)
                {// 当前柜台为空
                    TransferKitchenObject(player, this);
                }
                else
                {
                     if(GetKitchenObject().TryGetComponent<PlateKitchenObject>(out plateKitchenObject))
                    {
                        if (plateKitchenObject.AddKitchenObjectSO(player.GetKitchenObjectSO()))
                        {
                            player.DestroyKitchenObject();
                        }
                    }
                }
            }


        }
        else
        {// 玩家手中没有物品
            if (IsHaveKitchenObject())
            {// 当前柜台不为空
                TransferKitchenObject(this, player);
            }
            else
            {

            }
        }
    }
}
