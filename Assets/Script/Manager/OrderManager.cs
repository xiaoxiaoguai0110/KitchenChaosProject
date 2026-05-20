using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class OrderManager : MonoBehaviour
{
    public static OrderManager Instance { get; private set; }

    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeSuccessed;
    public event EventHandler OnRecipeFailed;

    [SerializeField] private RecipeListSO recipeSOList;
    [SerializeField] private int orderMaxCount = 5;
    [SerializeField] private float orderRate = 2;
    private List<RecipeSO> orderRecipeSOList = new List<RecipeSO>();

    private float orderTimer = 0;
    private bool isStartOrder = false;
    private int orderCount = 0;
    private int successDeliveryOrderCount = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsGamePlayingState()) 
        {
            StartSpawnOrder();
        }
    }

    private void Update()
    {
        if (isStartOrder)
        {
            OrderUpdate();
        }
    }

    private void OrderUpdate()
    {
        orderTimer += Time.deltaTime;
        if (orderTimer >= orderRate) 
        { 
            orderTimer = 0;
            OrderANewRecipe();
        }
        
    }

    private void OrderANewRecipe()
    {
        if(orderRecipeSOList.Count >= orderMaxCount)
        {
            return;
        }
        orderCount++;
        int index = UnityEngine.Random.Range(0, recipeSOList.recipeSOList.Count);
        orderRecipeSOList.Add(recipeSOList.recipeSOList[index]);
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    public void DeliveryRecipe(PlateKitchenObject plateKitchenObject)
    {
        RecipeSO correctRecipe = null;
        foreach(RecipeSO recipe in orderRecipeSOList)
        {
            if (IsCorrect(recipe, plateKitchenObject))
            {
                correctRecipe = recipe;break;
            }
        }

        if(correctRecipe == null)
        {
            print("�ϲ�ʧ��");
            OnRecipeFailed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            orderRecipeSOList.Remove(correctRecipe);
            OnRecipeSuccessed?.Invoke(this, EventArgs.Empty);
            successDeliveryOrderCount++;
            print("�ϲ˳ɹ�");
        }

    }

    private bool IsCorrect(RecipeSO recipe,PlateKitchenObject plateKitchenObject)
    {
        List<KitchenObjectSO> list1 = recipe.kitchenObjectSOList;
        List<KitchenObjectSO> list2 = plateKitchenObject.GetKitchenObjectSOList();
        if(list1.Count != list2.Count)
        {
            return false;
        } 

        foreach(KitchenObjectSO item in list1)
        {
            if(list2.Contains(item) == false)
            {
                return false;
            }
        }

        return true;
    }

    public bool IsPlateMatchingAnyOrder(PlateKitchenObject plateKitchenObject)
    {
        foreach (RecipeSO recipe in orderRecipeSOList)
        {
            if (IsCorrect(recipe, plateKitchenObject))
                return true;
        }
        return false;
    }

    public List<RecipeSO> GetOrderList()
    {
        return orderRecipeSOList;
    }

    public void StartSpawnOrder()
    {
        isStartOrder = true;
    }

    public int GetSuccessDeliveryCount()
    {
        return successDeliveryOrderCount;
    }

}
