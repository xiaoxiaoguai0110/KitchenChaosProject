using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCounter : BaseCounter
{
    public static event EventHandler OnObjectTrashed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvent()
    {
        OnObjectTrashed = null;
    }
    public override void Interact(Player player)
    {
        if (player.IsHaveKitchenObject())
        {
            player.DestroyKitchenObject();
            OnObjectTrashed?.Invoke(this, EventArgs.Empty);
        }
    }

    public override bool CanInteract(Player player, out string failureReason)
    {
        if (!player.IsHaveKitchenObject())
        {
            failureReason = "手上没有可以丢弃的东西";
            return false;
        }

        failureReason = null;
        return true;
    }

}
