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

    private void Start()
    {
        Hide();
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        restartButton?.onClick.AddListener(RestartGame);
        menuButton?.onClick.AddListener(ReturnToMenu);
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
        numberText.text = OrderManager.Instance.GetSuccessDeliveryCount().ToString();
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

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;

        restartButton?.onClick.RemoveListener(RestartGame);
        menuButton?.onClick.RemoveListener(ReturnToMenu);
    }
}
