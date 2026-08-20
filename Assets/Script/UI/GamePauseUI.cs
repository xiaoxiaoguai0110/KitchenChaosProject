using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private GameObject uiParent;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private UIPopupAnimator popupAnimator;
    private GameManager subscribedGameManager;

    private void Awake()
    {
        EnsureRestartButton();
    }

    private void Start()
    {
        Hide();
        SubscribeToGameManager();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        resumeButton?.onClick.AddListener(ResumeGame);
        restartButton?.onClick.AddListener(RestartGame);
        menuButton?.onClick.AddListener(ReturnToMenu);
        settingButton?.onClick.AddListener(OpenSettings);
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        resumeButton?.onClick.RemoveListener(ResumeGame);
        restartButton?.onClick.RemoveListener(RestartGame);
        menuButton?.onClick.RemoveListener(ReturnToMenu);
        settingButton?.onClick.RemoveListener(OpenSettings);
    }

    private void SubscribeToGameManager()
    {
        if (subscribedGameManager != null || GameManager.Instance == null)
            return;

        subscribedGameManager = GameManager.Instance;
        subscribedGameManager.OnGamePaused += GameManager_OnGamePaused;
        subscribedGameManager.OnGameUnpaused += GameManager_OnGameUnpaused;
    }

    private void UnsubscribeFromGameManager()
    {
        if (subscribedGameManager == null)
            return;

        subscribedGameManager.OnGamePaused -= GameManager_OnGamePaused;
        subscribedGameManager.OnGameUnpaused -= GameManager_OnGameUnpaused;
        subscribedGameManager = null;
    }

    private void GameManager_OnGameUnpaused(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGamePaused(object sender, System.EventArgs e)
    {
        Show();
    }

    private void Show()
    {
        uiParent.SetActive(true);
        popupAnimator?.PlayShow();
        resumeButton?.Select();
    }

    private void Hide()
    {
        uiParent.SetActive(false);
    }

    private void ResumeGame()
    {
        GameManager.Instance.ToggleGame();
    }

    private void RestartGame()
    {
        Loader.Load(Loader.Scene.GameScene);
    }

    private void ReturnToMenu()
    {
        Loader.Load(Loader.Scene.GameMenuScene);
    }

    private void OpenSettings()
    {
        SettingsUI.Instance.Show();
    }

    private void EnsureRestartButton()
    {
        if (restartButton != null || menuButton == null)
            return;

        // 兼容旧场景：复制现有按钮，便不需要学习者重新在 Inspector 拖引用。
        restartButton = Instantiate(menuButton, menuButton.transform.parent);
        restartButton.name = "RestartButton";
        restartButton.onClick = new Button.ButtonClickedEvent();

        TextMeshProUGUI label = restartButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
            label.text = "重新开始";

        RectTransform resumeRect = resumeButton?.transform as RectTransform;
        RectTransform restartRect = restartButton.transform as RectTransform;
        RectTransform settingRect = settingButton?.transform as RectTransform;
        RectTransform menuRect = menuButton.transform as RectTransform;

        if (resumeRect != null) resumeRect.anchoredPosition = new Vector2(0f, 95f);
        if (restartRect != null) restartRect.anchoredPosition = new Vector2(0f, 0f);
        if (settingRect != null) settingRect.anchoredPosition = new Vector2(0f, -95f);
        if (menuRect != null) menuRect.anchoredPosition = new Vector2(0f, -190f);
    }

}
