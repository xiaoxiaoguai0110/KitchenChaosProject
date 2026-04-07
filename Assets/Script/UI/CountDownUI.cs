using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CountDownUI : MonoBehaviour
{
    private const string IS_SHAKE = "IsShake";
    [SerializeField]private TextMeshProUGUI numberText;

    private int PreNumber = -1;

    private Animator anim;
    private void Start()
    {
        anim = GetComponent<Animator>();
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
    }

    private void Update()
    {
        if (GameManager.Instance.IsCountDownState())
        {
            int nowNumber = Mathf.CeilToInt(GameManager.Instance.GetCountDownTimer());
            numberText.text = nowNumber.ToString();
            if (nowNumber != PreNumber) 
            {
                PreNumber = nowNumber;
                anim.SetTrigger(IS_SHAKE);
            }
        }
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsCountDownState())
        {
            numberText.gameObject.SetActive(true);
        }
        else
        {
            numberText.gameObject.SetActive(false);
        }
    }
}
