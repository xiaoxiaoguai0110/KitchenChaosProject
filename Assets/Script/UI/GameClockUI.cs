using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameClockUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject uiParent;
    [SerializeField] private Image progressImage;
    [SerializeField] private TextMeshProUGUI timeText;

    [Header("Warning Feedback")]
    [Tooltip("剩余时间低于该值时，时钟会切换成警告色并开始轻微跳动。")]
    [SerializeField] private float warningThreshold = 10f;
    [SerializeField] private Color normalColor = new Color32(242, 91, 24, 255);
    [SerializeField] private Color warningColor = new Color32(224, 55, 45, 255);
    [SerializeField] private float warningPulseSpeed = 7f;
    [SerializeField] private float warningPulseAmount = .08f;

    private Vector3 timeTextBaseScale;
    private GameManager subscribedGameManager;

    private void Awake()
    {
        timeTextBaseScale = timeText.transform.localScale;
    }

    private void Start()
    {
        SubscribeToGameManager();
        Hide();
    }

    private void OnEnable()
    {
        SubscribeToGameManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromGameManager();
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
        if (GameManager.Instance.IsGamePlayingState())
            Show();
        else
            Hide();
    }

    private void Update()
    {
        if (!GameManager.Instance.IsGamePlayingState())
            return;

        float remainingTime = Mathf.Max(0, GameManager.Instance.GetGamePlayingTimer());
        progressImage.fillAmount = GameManager.Instance.GetGamePlayingTimerNormalized();
        timeText.text = Mathf.CeilToInt(remainingTime).ToString();

        bool isWarning = remainingTime <= warningThreshold;
        Color currentColor = isWarning ? warningColor : normalColor;
        progressImage.color = currentColor;
        timeText.color = isWarning ? warningColor : Color.white;

        // 最后十秒使用缩放脉冲，而不是每帧修改位置；这样既醒目，也不会破坏右上角锚点。
        float pulse = isWarning
            ? 1f + Mathf.Sin(Time.unscaledTime * warningPulseSpeed) * warningPulseAmount
            : 1f;
        timeText.transform.localScale = timeTextBaseScale * pulse;
    }

    private void Show()
    {
        uiParent.SetActive(true);
    }
    private void Hide()
    {
        timeText.transform.localScale = timeTextBaseScale;
        uiParent.SetActive(false);
    }

}
