using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KitchenObjectGridUI : MonoBehaviour
{

    [SerializeField]private KitchenObjectIconUI iconTemplateUI;

    private void Start()
    {
        iconTemplateUI.Hide();
    }

    public void ShowKitchenObjectUI(KitchenObjectSO kitchenObjectSO)
    {
        KitchenObjectIconUI newIconUI=GameObject.Instantiate(iconTemplateUI,transform);
        newIconUI.Show(kitchenObjectSO.sprite);
    }
}
