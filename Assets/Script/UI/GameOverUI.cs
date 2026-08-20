using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameObject uiParent;
    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private UIPopupAnimator popupAnimator;
    private GameManager subscribedGameManager;

    private void Awake()
    {
        ConfigureSummaryText();
    }

    private void Start()
    {
        Hide();
        SubscribeToGameManager();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        restartButton?.onClick.AddListener(RestartGame);
        menuButton?.onClick.AddListener(ReturnToMenu);
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        restartButton?.onClick.RemoveListener(RestartGame);
        menuButton?.onClick.RemoveListener(ReturnToMenu);
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

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsGameOverState()) 
        {
            Show();
        }
    }

    private void Show()
    {
        if (OrderManager.Instance != null && numberText != null)
        {
            numberText.text =
                $"成功订单  {OrderManager.Instance.GetSuccessDeliveryCount()}\n" +
                $"失败订单  {OrderManager.Instance.GetFinalFailedOrderCount()}\n" +
                $"最终分数  {OrderManager.Instance.GetFinalScore()}";
        }

        uiParent.SetActive(true);
        popupAnimator?.PlayShow();
        restartButton?.Select();
    }

    private void Hide()
    {
        uiParent.SetActive(false);
    }

    private void RestartGame()
    {
        Loader.Load(Loader.Scene.GameScene);
    }

    private void ReturnToMenu()
    {
        Loader.Load(Loader.Scene.GameMenuScene);
    }

    private void ConfigureSummaryText()
    {
        if (numberText == null)
            return;

        numberText.fontSize = 38f;
        numberText.enableWordWrapping = false;
        numberText.alignment = TextAlignmentOptions.Center;
        numberText.rectTransform.sizeDelta = new Vector2(480f, 170f);

        // 旧界面的标题是“成功完成订单”，现在改为三项统计的总标题。
        if (uiParent == null)
            return;

        foreach (TextMeshProUGUI text in uiParent.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.gameObject.name == "LabelText")
            {
                text.text = "本局统计";
                break;
            }
        }
    }

}
