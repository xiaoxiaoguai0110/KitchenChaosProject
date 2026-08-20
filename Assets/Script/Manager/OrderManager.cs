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
    [Header("Scoring")]
    [SerializeField, Min(0)] private int scorePerSuccessfulOrder = 100;
    [SerializeField, Min(0)] private int failedOrderPenalty = 25;
    private List<RecipeSO> orderRecipeSOList = new List<RecipeSO>();

    private float orderTimer = 0;
    private bool isStartOrder = false;
    private int orderCount = 0;
    private int successDeliveryOrderCount = 0;
    private int failedDeliveryOrderCount = 0;
    private GameManager subscribedGameManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SubscribeToGameManager();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void SubscribeToGameManager()
    {
        if (subscribedGameManager != null || GameManager.Instance == null)
            return;

        subscribedGameManager = GameManager.Instance;
        subscribedGameManager.OnStateChanged += GameManager_OnStateChanged;
    }

    private void UnsubscribeFromGameManager()
    {
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.OnStateChanged -= GameManager_OnStateChanged;
        subscribedGameManager = null;
    }

    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        // 进入 GamePlaying 才生成订单；进入 GameOver 后立刻关闭生成开关。
        isStartOrder = subscribedGameManager != null
            && subscribedGameManager.IsGamePlayingState();
    }

    private void Update()
    {
        if (!isStartOrder
            || GameManager.Instance == null
            || !GameManager.Instance.IsGameSimulationRunning())
            return;

        OrderUpdate();
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
        // 输入事件在暂停时仍可能被触发，因此交付入口自身也必须保护游戏数据。
        if (GameManager.Instance == null || !GameManager.Instance.IsGameSimulationRunning())
            return;

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
            // 先更新统计再广播事件，订阅者收到事件时就能读取到最新数据。
            failedDeliveryOrderCount++;
            Debug.Log("上菜失败");
            OnRecipeFailed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            orderRecipeSOList.Remove(correctRecipe);
            successDeliveryOrderCount++;
            OnRecipeSuccessed?.Invoke(this, EventArgs.Empty);
            Debug.Log("上菜成功");
        }

    }

    private bool IsCorrect(RecipeSO recipe, PlateKitchenObject plateKitchenObject)
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
        isStartOrder = GameManager.Instance != null
            && GameManager.Instance.IsGamePlayingState();
    }

    public int GetSuccessDeliveryCount()
    {
        return successDeliveryOrderCount;
    }

    public int GetFinalFailedOrderCount()
    {
        // 倒计时结束时还留在订单栏里的菜，也属于本局没有完成的订单。
        return failedDeliveryOrderCount + orderRecipeSOList.Count;
    }

    public int GetFinalScore()
    {
        int score = successDeliveryOrderCount * scorePerSuccessfulOrder
            - GetFinalFailedOrderCount() * failedOrderPenalty;
        return Mathf.Max(0, score);
    }

}
