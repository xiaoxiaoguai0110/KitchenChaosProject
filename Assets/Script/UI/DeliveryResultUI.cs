using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeliveryResultUI : MonoBehaviour
{
    private const string IS_SHOW = "IsShow";

    [SerializeField] private Animator deliverySuccessUIAnimator;
    [SerializeField] private Animator deliveryFailUIAnimator;

    // 记录当前显示的是哪一个，方便隐藏
    private GameObject currentActiveUI;

    private void Start()
    {
        OrderManager.Instance.OnRecipeSuccessed += OrderManager_OnRecipeSuccessed;
        OrderManager.Instance.OnRecipeFailed += OrderManager_OnRecipeFailed;
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
        // 1. 如果有旧的 UI 显示，先隐藏（这一步很重要，防止重叠）
        if (currentActiveUI != null)
        {
            currentActiveUI.SetActive(false);
        }

        // 2. 激活新的 UI
        currentActiveUI = uiToShow;
        currentActiveUI.SetActive(true);

        // 3. 获取 Animator
        Animator anim = currentActiveUI.GetComponent<Animator>();

        // 4. 关键：先重置 Trigger，再设置 Trigger
        anim.ResetTrigger(IS_SHOW);
        anim.SetTrigger(IS_SHOW);
    }

    // 这个方法用来在动画播完后调用（配合 Animation Event）
    public void Hide()
    {
        if (currentActiveUI != null)
        {
            currentActiveUI.SetActive(false);
            currentActiveUI = null;
        }
    }
}