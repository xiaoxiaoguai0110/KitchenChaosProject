using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameMenuUI : MonoBehaviour
{
    [Header("Main menu")]
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button localMultiplayerButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Single player")]
    [SerializeField] private GameObject singlePlayerPanel;
    [SerializeField] private Button soloButton;
    [SerializeField] private Button aiAssistedButton;
    [SerializeField] private Button closeSinglePlayerButton;

    [Header("Settings")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private TextMeshProUGUI fullscreenButtonText;
    [SerializeField] private Button closeSettingsButton;

    private void Start()
    {
        singlePlayerButton.onClick.AddListener(ShowSinglePlayerOptions);
        localMultiplayerButton.onClick.AddListener(StartLocalMultiplayer);
        settingsButton.onClick.AddListener(ShowSettings);
        quitButton.onClick.AddListener(QuitGame);
        soloButton.onClick.AddListener(StartSoloPlayer);
        aiAssistedButton.onClick.AddListener(StartSinglePlayerWithAI);
        closeSinglePlayerButton.onClick.AddListener(HideSinglePlayerOptions);
        fullscreenButton.onClick.AddListener(ToggleFullscreen);
        closeSettingsButton.onClick.AddListener(HideSettings);

        singlePlayerPanel.SetActive(false);
        settingsPanel.SetActive(false);
        UpdateFullscreenLabel();
        EventSystem.current?.SetSelectedGameObject(singlePlayerButton.gameObject);
    }

    private void OnDestroy()
    {
        singlePlayerButton.onClick.RemoveListener(ShowSinglePlayerOptions);
        localMultiplayerButton.onClick.RemoveListener(StartLocalMultiplayer);
        settingsButton.onClick.RemoveListener(ShowSettings);
        quitButton.onClick.RemoveListener(QuitGame);
        soloButton.onClick.RemoveListener(StartSoloPlayer);
        aiAssistedButton.onClick.RemoveListener(StartSinglePlayerWithAI);
        closeSinglePlayerButton.onClick.RemoveListener(HideSinglePlayerOptions);
        fullscreenButton.onClick.RemoveListener(ToggleFullscreen);
        closeSettingsButton.onClick.RemoveListener(HideSettings);
    }

    private void ShowSinglePlayerOptions()
    {
        singlePlayerPanel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(soloButton.gameObject);
    }

    private void HideSinglePlayerOptions()
    {
        singlePlayerPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(singlePlayerButton.gameObject);
    }

    private static void StartSoloPlayer()
    {
        GameModeSelection.Select(GameMode.SinglePlayer);
        Loader.Load(Loader.Scene.GameScene);
    }

    private static void StartSinglePlayerWithAI()
    {
        GameModeSelection.Select(GameMode.SinglePlayerWithAI);
        Loader.Load(Loader.Scene.GameScene);
    }

    private static void StartLocalMultiplayer()
    {
        GameModeSelection.Select(GameMode.LocalMultiplayer);
        Loader.Load(Loader.Scene.GameScene);
    }

    private void ShowSettings()
    {
        settingsPanel.SetActive(true);
        UpdateFullscreenLabel();
        EventSystem.current?.SetSelectedGameObject(fullscreenButton.gameObject);
    }

    private void HideSettings()
    {
        settingsPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(settingsButton.gameObject);
    }

    private void ToggleFullscreen()
    {
        Screen.fullScreen = !Screen.fullScreen;
        UpdateFullscreenLabel();
    }

    private void UpdateFullscreenLabel()
    {
        fullscreenButtonText.text = Screen.fullScreen ? "切换为窗口模式" : "切换为全屏模式";
    }

    private static void QuitGame()
    {
        Application.Quit();
    }
}
