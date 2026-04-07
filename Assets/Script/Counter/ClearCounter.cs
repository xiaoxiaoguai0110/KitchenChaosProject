using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ClearCounter : BaseCounter
{
    
    
    
    //[SerializeField] private ClearCounter transferTargetCounter;
    //[SerializeField] private bool testing = false;

    

    //private void Update()
    //{
    //    if (testing && Input.GetMouseButtonDown(0))
    //    {
    //        TransferKitchenObject(this, transferTargetCounter);
    //    }
    //}

    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {//手上有食材

            if(player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plateKitchenObject))
            {//手上有盘子
                if (IsHaveKitchenObject() == false)
                {//当前柜台 为空
                    TransferKitchenObject(player, this);
                }
                else
                {//当前柜台 不为空

                    bool isSuccess=plateKitchenObject.AddKitchenObjectSO(GetKitchenObjectSO());
                    if (isSuccess)
                    {
                        DestoryKitchenObject();
                    }
                }
            }
            else
            {//手上是普通食材
                if (IsHaveKitchenObject() == false)
                {//当前柜台 为空
                    TransferKitchenObject(player, this);
                }
                else
                {
                     if(GetKitchenObject().TryGetComponent<PlateKitchenObject>(out plateKitchenObject))
                    {
                        if (plateKitchenObject.AddKitchenObjectSO(player.GetKitchenObjectSO()))
                        {
                            player.DestoryKitchenObject();
                        }
                    }
                }
            }


        }
        else
        {//手上没食材
            if (IsHaveKitchenObject())
            {//当前柜台 不为空
                TransferKitchenObject(this, player);
            }
            else
            {

            }
        }
    }

    

    

    
}
