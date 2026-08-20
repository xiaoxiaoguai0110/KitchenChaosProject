using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject uiParent;
    [SerializeField] private GameObject rebindingHint;
    [SerializeField] private UIPopupAnimator popupAnimator;

    [Header("Audio")]
    [SerializeField] private Button soundButton;
    [SerializeField] private TextMeshProUGUI soundButtonText;
    [SerializeField] private Button musicButton;
    [SerializeField] private TextMeshProUGUI MusicButtonText;

    [Header("Navigation")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button playerSelectButton;
    [SerializeField] private TextMeshProUGUI playerSelectButtonText;

    [Header("Binding Buttons")]
    [SerializeField] private Button upKeyButton;
    [SerializeField] private Button downKeyButton;
    [SerializeField] private Button leftKeyButton;
    [SerializeField] private Button rightKeyButton;
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private Button operateKeyButton;
    [SerializeField] private Button pauseKeyButton;

    [Header("Binding Labels")]
    [SerializeField] private TextMeshProUGUI upKeyButtonText;
    [SerializeField] private TextMeshProUGUI downKeyButtonText;
    [SerializeField] private TextMeshProUGUI leftKeyButtonText;
    [SerializeField] private TextMeshProUGUI rightKeyButtonText;
    [SerializeField] private TextMeshProUGUI interactKeyButtonText;
    [SerializeField] private TextMeshProUGUI operateKeyButtonText;
    [SerializeField] private TextMeshProUGUI pauseKeyButtonText;

    private int selectedPlayerIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
        EnsurePlayerSelectButton();
    }

    private void Start()
    {
        Hide();
        rebindingHint.SetActive(false);
        UpdateVisual();
    }

    private void OnEnable()
    {
        soundButton.onClick.AddListener(ChangeSoundVolume);
        musicButton.onClick.AddListener(ChangeMusicVolume);
        closeButton.onClick.AddListener(Hide);
        playerSelectButton?.onClick.AddListener(SwitchBindingPlayer);

        if (GameInput.Instance != null)
            GameInput.Instance.OnBindingDisplayChanged += GameInput_OnBindingDisplayChanged;

        upKeyButton.onClick.AddListener(RebindUp);
        downKeyButton.onClick.AddListener(RebindDown);
        leftKeyButton.onClick.AddListener(RebindLeft);
        rightKeyButton.onClick.AddListener(RebindRight);
        interactKeyButton.onClick.AddListener(RebindInteract);
        operateKeyButton.onClick.AddListener(RebindOperate);
        pauseKeyButton.onClick.AddListener(RebindPause);
    }

    private void OnDisable()
    {
        soundButton.onClick.RemoveListener(ChangeSoundVolume);
        musicButton.onClick.RemoveListener(ChangeMusicVolume);
        closeButton.onClick.RemoveListener(Hide);
        playerSelectButton?.onClick.RemoveListener(SwitchBindingPlayer);

        if (GameInput.Instance != null)
            GameInput.Instance.OnBindingDisplayChanged -= GameInput_OnBindingDisplayChanged;

        upKeyButton.onClick.RemoveListener(RebindUp);
        downKeyButton.onClick.RemoveListener(RebindDown);
        leftKeyButton.onClick.RemoveListener(RebindLeft);
        rightKeyButton.onClick.RemoveListener(RebindRight);
        interactKeyButton.onClick.RemoveListener(RebindInteract);
        operateKeyButton.onClick.RemoveListener(RebindOperate);
        pauseKeyButton.onClick.RemoveListener(RebindPause);
    }

    public void Show()
    {
        UpdateVisual();
        uiParent.SetActive(true);
        popupAnimator?.PlayShow();
        soundButton.Select();
    }

    private void Hide()
    {
        rebindingHint.SetActive(false);
        uiParent.SetActive(false);
    }

    private void ChangeSoundVolume()
    {
        SoundManager.Instance.ChangeVolume();
        UpdateVisual();
    }

    private void ChangeMusicVolume()
    {
        MusicManager.Instance.ChangeVolume();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // 管理器内部保存的是 0~10，界面换算成百分比后更符合玩家习惯。
        soundButtonText.text = $"音效音量   {SoundManager.Instance.GetVolume() * 10}%";
        MusicButtonText.text = $"音乐音量   {MusicManager.Instance.GetVolume() * 10}%";

        if (GameInput.Instance == null)
            return;

        if (playerSelectButtonText != null)
            playerSelectButtonText.text = $"当前：玩家 {selectedPlayerIndex + 1}";

        upKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Up, selectedPlayerIndex);
        downKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Down, selectedPlayerIndex);
        leftKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Left, selectedPlayerIndex);
        rightKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Right, selectedPlayerIndex);
        interactKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Interact, selectedPlayerIndex);
        operateKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Operate, selectedPlayerIndex);
        pauseKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Pause, selectedPlayerIndex);
    }

    private void RebindUp() => Rebind(GameInput.BindingType.Up);
    private void RebindDown() => Rebind(GameInput.BindingType.Down);
    private void RebindLeft() => Rebind(GameInput.BindingType.Left);
    private void RebindRight() => Rebind(GameInput.BindingType.Right);
    private void RebindInteract() => Rebind(GameInput.BindingType.Interact);
    private void RebindOperate() => Rebind(GameInput.BindingType.Operate);
    private void RebindPause() => Rebind(GameInput.BindingType.Pause);

    private void Rebind(GameInput.BindingType bindingType)
    {
        rebindingHint.SetActive(true);

        // 重绑定是异步流程：收到新按键后再关闭提示并刷新当前绑定。
        GameInput.Instance.ReBinding(bindingType, selectedPlayerIndex, () =>
        {
            rebindingHint.SetActive(false);
            UpdateVisual();
        });
    }

    private void SwitchBindingPlayer()
    {
        selectedPlayerIndex = selectedPlayerIndex == 0 ? 1 : 0;
        UpdateVisual();
    }

    private void GameInput_OnBindingDisplayChanged(
        object sender,
        GameInput.PlayerActionEventArgs e)
    {
        if (e.PlayerIndex == selectedPlayerIndex && uiParent.activeSelf)
            UpdateVisual();
    }

    private void EnsurePlayerSelectButton()
    {
        if (playerSelectButton != null || upKeyButton == null)
            return;

        // 兼容旧场景：复制一个已有按键按钮作为“玩家切换”，无需重新拖 Inspector。
        playerSelectButton = Instantiate(upKeyButton, upKeyButton.transform.parent);
        playerSelectButton.name = "PlayerSelectButton";
        playerSelectButton.onClick = new Button.ButtonClickedEvent();
        playerSelectButtonText = playerSelectButton.GetComponentInChildren<TextMeshProUGUI>(true);

        RectTransform rect = playerSelectButton.transform as RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(145f, 112f);
            rect.sizeDelta = new Vector2(250f, 44f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
