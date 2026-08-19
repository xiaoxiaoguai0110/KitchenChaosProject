using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private GameObject uiParent;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingButton;
    [SerializeField] private UIPopupAnimator popupAnimator;
    private GameManager subscribedGameManager;

    private void Start()
    {
        Hide();
        SubscribeToGameManager();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        resumeButton.onClick.AddListener(ResumeGame);
        menuButton.onClick.AddListener(ReturnToMenu);
        settingButton.onClick.AddListener(OpenSettings);
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        resumeButton.onClick.RemoveListener(ResumeGame);
        menuButton.onClick.RemoveListener(ReturnToMenu);
        settingButton.onClick.RemoveListener(OpenSettings);
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
        resumeButton.Select();
    }

    private void Hide()
    {
        uiParent.SetActive(false);
    }

    private void ResumeGame()
    {
        GameManager.Instance.ToggleGame();
    }

    private void ReturnToMenu()
    {
        Loader.Load(Loader.Scene.GameMenuScene);
    }

    private void OpenSettings()
    {
        SettingsUI.Instance.Show();
    }

}
