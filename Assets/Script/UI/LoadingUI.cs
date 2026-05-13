using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class LoadingUI : MonoBehaviour
{
    [SerializeField]private TextMeshProUGUI dotText;
    private float dotRate = 0.3f;

    private void Start()
    {
        StartCoroutine(DotAnimation());
    }

    IEnumerator DotAnimation()
    {
        while (true)
        {
            dotText.text = ".";
            yield return new WaitForSeconds(dotRate);
            dotText.text = "..";
            yield return new WaitForSeconds(dotRate);
            dotText.text = "...";
            yield return new WaitForSeconds(dotRate);
            dotText.text = "....";
            yield return new WaitForSeconds(dotRate);
            dotText.text = ".....";
            yield return new WaitForSeconds(dotRate);
            dotText.text = "......";
            yield return new WaitForSeconds(dotRate);
        }
    }

}
