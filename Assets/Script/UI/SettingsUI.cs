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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        Instance = null;
    }

    private void Awake()
    {
        Instance = this;
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

        upKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Up);
        downKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Down);
        leftKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Left);
        rightKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Right);
        interactKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Interact);
        operateKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Operate);
        pauseKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Pause);
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
        GameInput.Instance.ReBinding(bindingType, () =>
        {
            rebindingHint.SetActive(false);
            UpdateVisual();
        });
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
