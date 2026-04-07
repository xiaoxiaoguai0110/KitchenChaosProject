using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [SerializeField]private GameObject uiParent;
    [SerializeField] private GameObject rebindingHint;
    [SerializeField] private Button soundButton;
    [SerializeField] private TextMeshProUGUI soundButtonText;
    [SerializeField] private Button musicButton;
    [SerializeField] private TextMeshProUGUI MusicButtonText;
    [SerializeField] private Button closeButton;

    [SerializeField] private Button upKeyButton;
    [SerializeField] private Button downKeyButton;
    [SerializeField] private Button leftKeyButton;
    [SerializeField] private Button rightKeyButton;
    [SerializeField] private Button interactKeyButton;
    [SerializeField] private Button operateKeyButton; 
    [SerializeField] private Button pauseKeyButton;

    [SerializeField] private TextMeshProUGUI upKeyButtonText;
    [SerializeField] private TextMeshProUGUI downKeyButtonText;
    [SerializeField] private TextMeshProUGUI leftKeyButtonText;
    [SerializeField] private TextMeshProUGUI rightKeyButtonText;
    [SerializeField] private TextMeshProUGUI interactKeyButtonText;
    [SerializeField] private TextMeshProUGUI operateKeyButtonText;
    [SerializeField] private TextMeshProUGUI pauseKeyButtonText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        Hide();
        UpdateVisual();

        soundButton.onClick.AddListener(() =>
        {
            SoundManager.Instance.ChangeVolume();
            UpdateVisual();
        });
        musicButton.onClick.AddListener(() =>
        {
            MusicManager.Instance.ChangeVolume();
            UpdateVisual();
        });
        closeButton.onClick.AddListener(() =>
        {
            Hide();
        });

        upKeyButton.onClick.AddListener(() =>{ReBinding(GameInput.BindingType.Up);});
        downKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Down); });
        leftKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Left); });
        rightKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Right); });
        interactKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Interact); });
        operateKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Operate); });
        pauseKeyButton.onClick.AddListener(() => { ReBinding(GameInput.BindingType.Pause); });
    }

    public void Show()
    {
        uiParent.SetActive(true);
    }
    private void Hide()
    {
        uiParent.SetActive(false);
    }

    void UpdateVisual()
    {
        soundButtonText.text = "音效大小：" + SoundManager.Instance.GetVolume();
        MusicButtonText.text = "音乐大小：" + MusicManager.Instance.GetVolume();

        upKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Up);
        downKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Down);
        leftKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Left);
        rightKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Right);
        interactKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Interact);
        pauseKeyButtonText.text = GameInput.Instance.GetBindingDisplayString(GameInput.BindingType.Pause);
        operateKeyButtonText.text = GameInput.Instance.GetBindingDisplayString (GameInput.BindingType.Operate);

    }
    
    private void ReBinding(GameInput.BindingType bindingType)
    {
        rebindingHint.SetActive(true);
        GameInput.Instance.ReBinding(bindingType, () =>
        {
            rebindingHint.SetActive(false);
            UpdateVisual();
        });
    }
}
