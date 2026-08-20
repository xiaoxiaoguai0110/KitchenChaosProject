using System.Collections.Generic;
using UnityEngine;

public class OrderListUI : MonoBehaviour
{
    [SerializeField] private Transform recipeParent;
    [SerializeField] private RecipeUI recipeUITemplate;
    private OrderManager subscribedOrderManager;

    private void Start()
    {
        recipeUITemplate.gameObject.SetActive(false);
        SubscribeToOrderManager();
        UpdateUI();
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
        subscribedOrderManager.OnRecipeSpawned += OrderManager_OnRecipeSpawned;
        subscribedOrderManager.OnRecipeSuccessed += OrderManger_OnRecipeSuccessed;
    }

    private void UnsubscribeFromOrderManager()
    {
        if (subscribedOrderManager == null)
            return;

        subscribedOrderManager.OnRecipeSpawned -= OrderManager_OnRecipeSpawned;
        subscribedOrderManager.OnRecipeSuccessed -= OrderManger_OnRecipeSuccessed;
        subscribedOrderManager = null;
    }

    private void OrderManger_OnRecipeSuccessed(object sender, System.EventArgs e)
    {
        UpdateUI();
    }

    private void OrderManager_OnRecipeSpawned(object sender, System.EventArgs e)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        List<Transform> toDestroy = new List<Transform>();
        foreach (Transform child in recipeParent)
        {
            if (child != recipeUITemplate.transform)
                toDestroy.Add(child);
        }
        foreach (Transform child in toDestroy)
            Destroy(child.gameObject);

        OrderManager orderManager = subscribedOrderManager != null
            ? subscribedOrderManager
            : OrderManager.Instance;
        if (orderManager == null)
            return;

        List<RecipeSO> recipeSOList = orderManager.GetOrderList();
        foreach (RecipeSO recipeSO in recipeSOList)
        {
            // 模板保留在层级中并保持隐藏，每份真实订单都从它复制，便于统一调整卡片样式。
            RecipeUI recipeUI = Instantiate(recipeUITemplate, recipeParent);
            recipeUI.gameObject.SetActive(true);
            recipeUI.UpdateUI(recipeSO);
        }
    }

}
