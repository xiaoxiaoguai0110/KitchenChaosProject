using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObjectHolder : MonoBehaviour
{
    public sealed class KitchenObjectTransferEventArgs : EventArgs
    {
        public KitchenObjectHolder Holder { get; }
        public KitchenObject KitchenObject { get; }

        public KitchenObjectTransferEventArgs(KitchenObjectHolder holder, KitchenObject kitchenObject)
        {
            Holder = holder;
            KitchenObject = kitchenObject;
        }
    }

    public static event EventHandler OnDrop;
    public static event EventHandler OnPickup;
    public static event EventHandler<KitchenObjectTransferEventArgs> OnKitchenObjectDropped;
    public static event EventHandler<KitchenObjectTransferEventArgs> OnKitchenObjectPickedUp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticEvents()
    {
        // 进入新的 Play Session 时由 Unity 自动调用，即使关闭 Domain Reload 也不会保留旧订阅者。
        OnDrop = null;
        OnPickup = null;
        OnKitchenObjectDropped = null;
        OnKitchenObjectPickedUp = null;
    }

    [SerializeField] private Transform holdPoint;
    private KitchenObject kitchenObject;

    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObject.GetKitchenObjectSO();
    }

    public bool IsHaveKitchenObject()
    {
        return kitchenObject != null;
    }
    public void SetKitchenObject(KitchenObject kitchenObject) 
    {
        bool changedToNewObject = this.kitchenObject != kitchenObject && kitchenObject != null;
        this.kitchenObject = kitchenObject;

        if (kitchenObject == null)
            return;

        kitchenObject.transform.localPosition = Vector3.zero;

        // 旧事件继续供音效系统使用；带上下文的新事件让表现层知道具体要动画哪个物体。
        if (this is BaseCounter && changedToNewObject)
        {
            OnDrop?.Invoke(this, EventArgs.Empty);
            OnKitchenObjectDropped?.Invoke(
                this,
                new KitchenObjectTransferEventArgs(this, kitchenObject));
        }
        else if (this is Player && changedToNewObject)
        {
            OnPickup?.Invoke(this, EventArgs.Empty);
            OnKitchenObjectPickedUp?.Invoke(
                this,
                new KitchenObjectTransferEventArgs(this, kitchenObject));
        }
    }
    public Transform GetHoldPoint()
    {
        return holdPoint;
    }
    public void TransferKitchenObject(KitchenObjectHolder sourceHolder, KitchenObjectHolder targetHolder)
    {
        if (sourceHolder.GetKitchenObject() == null)
        {
            Debug.LogWarning("源容器中没有可转移的物品，转移失败。");
            return;
        }
        if (targetHolder.GetKitchenObject() != null)
        {
            Debug.LogWarning("目标容器已经持有物品，转移失败。");
            return;
        }
        targetHolder.AddKitchenObject(sourceHolder.GetKitchenObject());
        sourceHolder.ClearKitchenObject();
    }
    public void AddKitchenObject(KitchenObject kitchenObject)
    {
        kitchenObject.transform.SetParent(holdPoint);
        SetKitchenObject(kitchenObject);
    }

    

    public void ClearKitchenObject()
    {
        this.kitchenObject = null;
    }

    public void DestroyKitchenObject()
    {
        Destroy(kitchenObject.gameObject);
        this.kitchenObject = null;
    }
    public void CreateKitchenObject(GameObject kitchenObjectPrefab)
    {
        KitchenObject kitchenObject = GameObject.Instantiate(kitchenObjectPrefab, GetHoldPoint()).GetComponent<KitchenObject>();
        SetKitchenObject(kitchenObject);
    }
}
