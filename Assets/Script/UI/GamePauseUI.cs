using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField]private GameObject uiParent;
    [SerializeField]private Button resumeButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button settingButton;

    private void Start()
    {
        Hide();
        GameManager.Instance.OnGamePaused += GameManager_OnGamePaused;
        GameManager.Instance.OnGameUnpaused += GameManager_OnGameUnpaused;
        resumeButton.onClick.AddListener(() =>
        {
            GameManager.Instance.ToggleGame();
        });
        menuButton.onClick.AddListener(() => 
        {
            Loader.Load(Loader.Scene.GameMenuScene);
        });
        settingButton.onClick.AddListener(() =>
        {
            SettingsUI.Instance.Show();
        });
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
    }
    private void Hide()
    {
        uiParent.SetActive(false);
    }

}
