using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ClearCounter : BaseCounter
{
    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {//������ʳ�

            if(player.GetKitchenObject().TryGetComponent<PlateKitchenObject>(out PlateKitchenObject plateKitchenObject))
            {//����������
                if (IsHaveKitchenObject() == false)
                {//��ǰ��̨ Ϊ��
                    TransferKitchenObject(player, this);
                }
                else
                {//��ǰ��̨ ��Ϊ��

                    bool isSuccess=plateKitchenObject.AddKitchenObjectSO(GetKitchenObjectSO());
                    if (isSuccess)
                    {
                        DestroyKitchenObject();
                    }
                }
            }
            else
            {//��������ͨʳ��
                if (IsHaveKitchenObject() == false)
                {//��ǰ��̨ Ϊ��
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
        {//����ûʳ��
            if (IsHaveKitchenObject())
            {//��ǰ��̨ ��Ϊ��
                TransferKitchenObject(this, player);
            }
            else
            {

            }
        }
    }
}
