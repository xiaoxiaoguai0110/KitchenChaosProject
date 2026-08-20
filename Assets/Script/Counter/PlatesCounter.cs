using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounter : BaseCounter
{
    [SerializeField]private KitchenObjectSO plateSO;
    [SerializeField]private int spawRate = 3;
    [SerializeField]private int plateCountMax = 5;

    private List<KitchenObject> platesList = new List<KitchenObject>();

    private float timer = 0;

    private void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

        if(platesList.Count < plateCountMax)
        {
            timer += Time.deltaTime;
        }
        
        if(timer > spawRate && platesList.Count < plateCountMax)
        {
            timer = 0;
            SpawnPlate();
        }

    }

    public override void Interact(Player player)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

        if (player.IsHaveKitchenObject() == false)
        {// 玩家手中没有物品
            if(platesList.Count > 0)
            {
                player.AddKitchenObject(platesList[platesList.Count - 1]);
                platesList.RemoveAt(platesList.Count - 1);
            }
        }
    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (player.IsHaveKitchenObject())
        {
            failureReason = "双手已占用，不能拿盘子";
            return false;
        }

        if (platesList.Count == 0)
        {
            failureReason = "盘子还没有准备好";
            return false;
        }

        failureReason = null;
        return true;
    }

    public void SpawnPlate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

        if(platesList.Count >= plateCountMax)
        {
            timer = 0;
            return;
        }
        KitchenObject kitchenObject = GameObject.Instantiate(plateSO.prefab, GetHoldPoint()).GetComponent<KitchenObject>();

        kitchenObject.transform.localPosition = Vector3.zero + Vector3.up * 0.1f * platesList.Count;

        platesList.Add(kitchenObject);
    }

}
