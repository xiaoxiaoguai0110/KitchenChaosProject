using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecipeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipeNameText;
    [SerializeField] private Transform kitchenObjectParent;
    [SerializeField] private Image iconUITemplate;

    private void Start()
    {
        iconUITemplate.gameObject.SetActive(false);
    }

    public void UpdateUI(RecipeSO recipeSO)
    {
        recipeNameText.text = recipeSO.recipeName;
        foreach (KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOList)
        {
            // 直接指定父节点会保持 UI 的本地缩放为 1，避免图标因 worldPositionStays 继承错误尺寸。
            Image newIcon = Instantiate(iconUITemplate, kitchenObjectParent);
            newIcon.sprite = kitchenObjectSO.sprite;
            newIcon.gameObject.SetActive(true);
        }
    }
}
