using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryResultUI : MonoBehaviour
{
    private const string IS_SHOW = "IsShow";

    [SerializeField] private Animator deliverySuccessUIAnimator;
    [SerializeField] private Animator deliveryFailUIAnimator;

    // 记录当前显示的结果，下一次结果出现前先把旧提示隐藏。
    private GameObject currentActiveUI;
    private OrderManager subscribedOrderManager;

    private void Start()
    {
        SubscribeToOrderManager();
    }

    private void OnEnable()
    {
        SubscribeToOrderManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromOrderManager();
    }

    private void SubscribeToOrderManager()
    {
        if (subscribedOrderManager != null || OrderManager.Instance == null)
            return;

        subscribedOrderManager = OrderManager.Instance;
        subscribedOrderManager.OnRecipeSuccessed += OrderManager_OnRecipeSuccessed;
        subscribedOrderManager.OnRecipeFailed += OrderManager_OnRecipeFailed;
    }

    private void UnsubscribeFromOrderManager()
    {
        if (subscribedOrderManager == null)
            return;

        subscribedOrderManager.OnRecipeSuccessed -= OrderManager_OnRecipeSuccessed;
        subscribedOrderManager.OnRecipeFailed -= OrderManager_OnRecipeFailed;
        subscribedOrderManager = null;
    }

    private void OrderManager_OnRecipeFailed(object sender, System.EventArgs e)
    {
        ShowResult(deliveryFailUIAnimator.gameObject);
    }

    private void OrderManager_OnRecipeSuccessed(object sender, System.EventArgs e)
    {
        ShowResult(deliverySuccessUIAnimator.gameObject);
    }

    private void ShowResult(GameObject uiToShow)
    {
        // 先隐藏旧结果，防止成功和失败动画同时叠在一起。
        if (currentActiveUI != null)
        {
            currentActiveUI.SetActive(false);
        }

        currentActiveUI = uiToShow;
        currentActiveUI.SetActive(true);

        Animator anim = currentActiveUI.GetComponent<Animator>();

        // 同一个结果连续出现时先重置 Trigger，确保动画可以从头播放。
        anim.ResetTrigger(IS_SHOW);
        anim.SetTrigger(IS_SHOW);
    }

    // 由结果动画最后一帧的 Animation Event 调用。
    public void Hide()
    {
        if (currentActiveUI != null)
        {
            currentActiveUI.SetActive(false);
            currentActiveUI = null;
        }
    }
}
