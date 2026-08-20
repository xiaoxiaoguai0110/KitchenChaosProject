using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject uiParent;
    [SerializeField] private TextMeshProUGUI upKeyText;
    [SerializeField] private TextMeshProUGUI downKeyText;
    [SerializeField] private TextMeshProUGUI leftKeyText;
    [SerializeField] private TextMeshProUGUI rightKeyText;
    [SerializeField] private TextMeshProUGUI interactKeyText;
    [SerializeField] private TextMeshProUGUI operateKeyText;
    [SerializeField] private TextMeshProUGUI pauseKeyText;
    private GameManager subscribedGameManager;
    private GameInput subscribedInput;

    private void Start()
    {
        SubscribeToGameManager();
        Show();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
        SubscribeToInput();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
        UnsubscribeFromInput();
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

    private void SubscribeToInput()
    {
        if (subscribedInput != null || GameInput.Instance == null)
            return;

        subscribedInput = GameInput.Instance;
        subscribedInput.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
    }

    private void UnsubscribeFromInput()
    {
        if (subscribedInput == null)
            return;

        subscribedInput.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;
        subscribedInput = null;
    }

    private void GameInput_OnBindingDisplayChanged(
        object sender,
        GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex == 0 && uiParent.activeSelf)
            UpdateVisual();
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsWaitingToStartState())
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void UpdateVisual()
    {
        upKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Up);
        downKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Down);
        leftKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Left);
        rightKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Right);
        interactKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Interact);
        operateKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Operate);
        pauseKeyText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Pause);
    }

    private void Show()
    {
        UpdateVisual();
        uiParent.SetActive(true);
    }
    private void Hide()
    {
        uiParent.SetActive(false);
    }

}
