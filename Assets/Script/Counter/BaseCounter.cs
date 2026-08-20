using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCounter : KitchenObjectHolder
{
    [SerializeField] private GameObject selectedCounter;
    private readonly HashSet<Player> selectingPlayers = new HashSet<Player>();
    public virtual void Interact(Player player)
    {
        Debug.LogWarning("交互没有重写");
    }

    public virtual void InteractOperate(Player player)
    {

    }

    /// <summary>
    /// 在真正执行交互前检查条件。规则由具体柜台拥有，Player 不需要了解配方或柜台内部状态。
    /// </summary>
    public virtual bool CanInteract(Player player, out string failureReason)
    {
        failureReason = null;
        return true;
    }

    public virtual bool SupportsOperate()
    {
        return false;
    }

    public virtual bool CanOperate(Player player, out string failureReason)
    {
        failureReason = "这个柜台不能操作";
        return false;
    }

    public void SelectCounter(Player player)
    {
        if (player == null)
            return;

        selectingPlayers.Add(player);
        UpdateSelectionVisual();
    }

    public void CancelSelect(Player player)
    {
        if (player == null)
            return;

        selectingPlayers.Remove(player);
        UpdateSelectionVisual();
    }

    private void OnDisable()
    {
        selectingPlayers.Clear();
        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        // 两名玩家可以同时选中同一柜台；只有最后一人移开后才关闭高亮。
        if (selectedCounter != null)
            selectedCounter.SetActive(selectingPlayers.Count > 0);
    }

    
} 
