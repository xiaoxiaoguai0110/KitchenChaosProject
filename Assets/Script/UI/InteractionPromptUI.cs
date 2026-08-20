using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    private const float FeedbackDuration = 1.35f;

    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private CanvasGroup feedbackCanvasGroup;

    private Player player;
    private GameInput subscribedInput;
    private UIFeedbackPulse feedbackPulse;
    private float feedbackTimer;

    private void Awake()
    {
        if (feedbackPanel == null)
            return;

        feedbackPulse = feedbackPanel.GetComponent<UIFeedbackPulse>();
        if (feedbackPulse == null)
            feedbackPulse = feedbackPanel.AddComponent<UIFeedbackPulse>();
    }

    private void Start()
    {
        TryBindPlayer();
        HideAll();
        RefreshPrompt();
    }

    private void OnEnable()
    {
        TryBindPlayer();
        TryBindInput();
    }

    private void OnDisable()
    {
        UnbindPlayer();
        UnbindInput();
    }

    private void Update()
    {
        if (player == null)
        {
            TryBindPlayer();
            return;
        }

        if (subscribedInput == null)
            TryBindInput();

        bool simulationRunning = GameManager.Instance != null
            && GameManager.Instance.IsGameSimulationRunning();
        if (!simulationRunning)
        {
            promptPanel.SetActive(false);
            feedbackPanel.SetActive(false);
            return;
        }

        if (feedbackTimer <= 0f)
            return;

        feedbackTimer -= Time.unscaledDeltaTime;
        float fadeStart = FeedbackDuration * 0.35f;
        feedbackCanvasGroup.alpha = feedbackTimer < fadeStart
            ? feedbackTimer / fadeStart
            : 1f;

        if (feedbackTimer <= 0f)
            feedbackPanel.SetActive(false);
    }

    private void TryBindPlayer()
    {
        Player resolvedPlayer = Player.GetInstance(playerIndex);
        if (resolvedPlayer == null || resolvedPlayer == player)
            return;

        UnbindPlayer();
        player = resolvedPlayer;
        player.OnSelectedCounterChanged += Player_OnSelectedCounterChanged;
        player.OnInteractionFeedback += Player_OnInteractionFeedback;
        RefreshPrompt();
    }

    private void UnbindPlayer()
    {
        if (player == null)
            return;

        player.OnSelectedCounterChanged -= Player_OnSelectedCounterChanged;
        player.OnInteractionFeedback -= Player_OnInteractionFeedback;
        player = null;
    }

    private void TryBindInput()
    {
        if (subscribedInput != null || GameInput.Instance == null)
            return;

        subscribedInput = GameInput.Instance;
        subscribedInput.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;
    }

    private void UnbindInput()
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
        if (e.PlayerIndex == playerIndex)
            RefreshPrompt();
    }

    private void Player_OnSelectedCounterChanged(
        object sender,
        Player.SelectedCounterChangedEventArgs e)
    {
        RefreshPrompt();
    }

    private void Player_OnInteractionFeedback(
        object sender,
        Player.InteractionFeedbackEventArgs e)
    {
        feedbackText.text = e.Message;
        feedbackCanvasGroup.alpha = 1f;
        feedbackTimer = FeedbackDuration;
        feedbackPanel.SetActive(true);
        feedbackPulse?.PlayError();
    }

    private void RefreshPrompt()
    {
        BaseCounter counter = player != null ? player.GetSelectedCounter() : null;
        if (counter == null || GameInput.Instance == null)
        {
            promptPanel.SetActive(false);
            return;
        }

        string interactKey = GameInput.Instance.GetBindingDisplayString(
            GameInput.BindingType.Interact,
            playerIndex);
        string message = $"[{interactKey}] 交互";

        if (counter.SupportsOperate())
        {
            string operateKey = GameInput.Instance.GetBindingDisplayString(
                GameInput.BindingType.Operate,
                playerIndex);
            message += $"    [{operateKey}] 操作";
        }

        promptText.text = message;
        promptPanel.SetActive(true);
    }

    private void HideAll()
    {
        promptPanel.SetActive(false);
        feedbackPanel.SetActive(false);
    }
}
