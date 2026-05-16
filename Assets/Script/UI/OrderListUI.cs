using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrderListUI : MonoBehaviour
{
    [SerializeField]private Transform recipeParent;
    [SerializeField]private RecipeUI recipeUITemplate;

    private void Start()
    {
        recipeUITemplate.gameObject.SetActive(false);
        OrderManager.Instance.OnRecipeSpawned += OrderManager_OnRecipeSpawned;
        OrderManager.Instance.OnRecipeSuccessed += OrderManger_OnRecipeSuccessed;
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
        foreach(Transform child in recipeParent)
        {
            if(child != recipeUITemplate.transform)
                toDestroy.Add(child);
        }
        foreach(Transform child in toDestroy)
            Destroy(child.gameObject);

        List<RecipeSO> recipeSOList = OrderManager.Instance.GetOrderList();
        foreach(RecipeSO recipeSO in recipeSOList)
        {
            RecipeUI recipeUI = GameObject.Instantiate(recipeUITemplate, recipeParent);
            recipeUI.gameObject.SetActive(true);
            recipeUI.UpdateUI(recipeSO);
        }
    }
}
