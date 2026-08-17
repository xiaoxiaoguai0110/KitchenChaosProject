using TMPro;
using UnityEngine;

public class CountDownUI : MonoBehaviour
{
    private static readonly int IsShake = Animator.StringToHash("IsShake");

    [SerializeField] private TextMeshProUGUI numberText;
    [SerializeField] private Color threeColor = new Color32(255, 205, 80, 255);
    [SerializeField] private Color twoColor = new Color32(245, 135, 28, 255);
    [SerializeField] private Color oneColor = new Color32(224, 55, 45, 255);

    private int previousNumber = -1;
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void Update()
    {
        if (!GameManager.Instance.IsCountDownState())
            return;

        // GameManager 会在 0 秒停留一帧；限制到 1 可以避免画面短暂显示“0”。
        int currentNumber = Mathf.Clamp(
            Mathf.CeilToInt(GameManager.Instance.GetCountDownTimer()),
            1,
            3);

        numberText.text = currentNumber.ToString();
        numberText.color = GetNumberColor(currentNumber);

        if (currentNumber == previousNumber)
            return;

        previousNumber = currentNumber;
        animator.SetTrigger(IsShake);
        SoundManager.Instance.PlayCountDownSound();
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsCountDownState())
        {
            previousNumber = -1;
            numberText.gameObject.SetActive(true);
        }
        else
        {
            numberText.gameObject.SetActive(false);
        }
    }

    private Color GetNumberColor(int number)
    {
        return number switch
        {
            3 => threeColor,
            2 => twoColor,
            _ => oneColor
        };
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= GameManager_OnStateChanged;
    }
}
