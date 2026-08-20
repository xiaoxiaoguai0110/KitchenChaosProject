using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class InteractionFeedbackHub : MonoBehaviour
{
    private static InteractionFeedbackHub instance;

    private readonly List<StoveCounter> subscribedStoves = new List<StoveCounter>();
    private OrderManager subscribedOrderManager;
    private DeliveryCounter deliveryCounter;
    private bool staticEventsSubscribed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateRuntimeHub()
    {
        if (instance != null)
            return;

        InteractionFeedbackHub existingHub = FindObjectOfType<InteractionFeedbackHub>();
        if (existingHub != null)
        {
            instance = existingHub;
            return;
        }

        GameObject hubObject = new GameObject("[Interaction Feedback Hub]");
        instance = hubObject.AddComponent<InteractionFeedbackHub>();
        DontDestroyOnLoad(hubObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SubscribeStaticEvents();
        SceneManager.sceneLoaded += SceneManager_OnSceneLoaded;
        BindSceneObjects();
    }

    private void OnDisable()
    {
        UnsubscribeStaticEvents();
        SceneManager.sceneLoaded -= SceneManager_OnSceneLoaded;
        UnbindSceneObjects();
    }

    private void SceneManager_OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneObjects();
    }

    private void SubscribeStaticEvents()
    {
        if (staticEventsSubscribed)
            return;

        KitchenObjectHolder.OnKitchenObjectPickedUp += KitchenObjectHolder_OnKitchenObjectPickedUp;
        KitchenObjectHolder.OnKitchenObjectDropped += KitchenObjectHolder_OnKitchenObjectDropped;
        CuttingCounter.OnCut += CuttingCounter_OnCut;
        staticEventsSubscribed = true;
    }

    private void UnsubscribeStaticEvents()
    {
        if (!staticEventsSubscribed)
            return;

        KitchenObjectHolder.OnKitchenObjectPickedUp -= KitchenObjectHolder_OnKitchenObjectPickedUp;
        KitchenObjectHolder.OnKitchenObjectDropped -= KitchenObjectHolder_OnKitchenObjectDropped;
        CuttingCounter.OnCut -= CuttingCounter_OnCut;
        staticEventsSubscribed = false;
    }

    private void BindSceneObjects()
    {
        UnbindSceneObjects();

        subscribedOrderManager = OrderManager.Instance;
        if (subscribedOrderManager != null)
        {
            subscribedOrderManager.OnRecipeSuccessed += OrderManager_OnRecipeSuccessed;
            subscribedOrderManager.OnRecipeFailed += OrderManager_OnRecipeFailed;
        }

        deliveryCounter = FindObjectOfType<DeliveryCounter>();
        StoveCounter[] stoves = FindObjectsOfType<StoveCounter>();
        foreach (StoveCounter stove in stoves)
        {
            stove.OnFeedback += StoveCounter_OnFeedback;
            subscribedStoves.Add(stove);
        }
    }

    private void UnbindSceneObjects()
    {
        if (subscribedOrderManager != null)
        {
            subscribedOrderManager.OnRecipeSuccessed -= OrderManager_OnRecipeSuccessed;
            subscribedOrderManager.OnRecipeFailed -= OrderManager_OnRecipeFailed;
            subscribedOrderManager = null;
        }

        foreach (StoveCounter stove in subscribedStoves)
        {
            if (stove != null)
                stove.OnFeedback -= StoveCounter_OnFeedback;
        }
        subscribedStoves.Clear();
        deliveryCounter = null;
    }

    private void KitchenObjectHolder_OnKitchenObjectPickedUp(
        object sender,
        KitchenObjectHolder.KitchenObjectTransferEventArgs e)
    {
        KitchenObjectFeedback.Play(e.KitchenObject, KitchenObjectFeedback.FeedbackType.Pickup);
    }

    private void KitchenObjectHolder_OnKitchenObjectDropped(
        object sender,
        KitchenObjectHolder.KitchenObjectTransferEventArgs e)
    {
        KitchenObjectFeedback.Play(e.KitchenObject, KitchenObjectFeedback.FeedbackType.Drop);
        FeedbackParticleFactory.PlayBurst(
            e.Holder.GetHoldPoint().position + Vector3.up * 0.08f,
            new Color(1f, 0.72f, 0.28f),
            5,
            0.08f);
    }

    private void CuttingCounter_OnCut(object sender, EventArgs e)
    {
        if (!(sender is CuttingCounter counter))
            return;

        Vector3 effectPosition = counter.GetHoldPoint().position + Vector3.up * 0.25f;
        FeedbackParticleFactory.PlayBurst(effectPosition, new Color(1f, 0.45f, 0.12f), 9);
        CameraFeedbackShake.Play(0.025f, 0.09f);
    }

    private void StoveCounter_OnFeedback(object sender, StoveCounter.StoveFeedbackEventArgs e)
    {
        if (!(sender is StoveCounter stove))
            return;

        Vector3 popupPosition = stove.GetHoldPoint().position + Vector3.up * 1.15f;
        switch (e.FeedbackType)
        {
            case StoveCounter.StoveFeedbackType.CookingCompleted:
                ShowWorldFeedback(popupPosition, "READY!", new Color(0.4f, 1f, 0.35f), 16, 0.025f);
                break;
            case StoveCounter.StoveFeedbackType.BurnWarning:
                ShowWorldFeedback(popupPosition, "WARNING!", new Color(1f, 0.55f, 0.08f), 10, 0.035f);
                break;
            case StoveCounter.StoveFeedbackType.Burned:
                ShowWorldFeedback(popupPosition, "BURNT!", new Color(1f, 0.18f, 0.1f), 18, 0.05f);
                break;
        }
    }

    private void OrderManager_OnRecipeSuccessed(object sender, EventArgs e)
    {
        Vector3 position = GetDeliveryFeedbackPosition();
        ShowWorldFeedback(position, "+1 ORDER!", new Color(0.35f, 1f, 0.45f), 22, 0.035f);
    }

    private void OrderManager_OnRecipeFailed(object sender, EventArgs e)
    {
        Vector3 position = GetDeliveryFeedbackPosition();
        ShowWorldFeedback(position, "WRONG ORDER", new Color(1f, 0.2f, 0.12f), 10, 0.05f);
    }

    private Vector3 GetDeliveryFeedbackPosition()
    {
        if (deliveryCounter != null)
        {
            // 送餐台只负责销毁餐盘，不会真正持有 KitchenObject，因此 Prefab 没有配置 holdPoint。
            return deliveryCounter.transform.position + Vector3.up * 2f;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null
            ? mainCamera.transform.position + mainCamera.transform.forward * 8f
            : Vector3.zero;
    }

    private static void ShowWorldFeedback(
        Vector3 position,
        string message,
        Color color,
        int particleCount,
        float shakeAmplitude)
    {
        WorldFeedbackPopup.Show(position, message, color);
        FeedbackParticleFactory.PlayBurst(position - Vector3.up * 0.25f, color, particleCount);
        CameraFeedbackShake.Play(shakeAmplitude, 0.14f);
    }
}
