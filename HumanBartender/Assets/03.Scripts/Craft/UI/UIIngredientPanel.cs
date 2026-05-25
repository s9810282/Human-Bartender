using Spine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[System.Serializable]
public struct CategoryButtons
{
    public string name;
    public string category;
    public Sprite tapSprite;
    public float selectXPos;
}

public class UIIngredientPanel : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] IngredientDataSO ingredientDataSO;
    [SerializeField] CraftStationData craftLiquidData;

    [Header("UICocktail Ingredient Panel")]
    [SerializeField] Transform baseParent;
    [SerializeField] Transform liqueurParent;
    [SerializeField] Transform garnishParent;
    [SerializeField] Transform etcParent;
    [Space(20f)]
    [SerializeField] CategoryButtons[] categories;
    [SerializeField] RectTransform selectBtn;
    [SerializeField] TextMeshProUGUI selectBtnText;

    [SerializeField] UIIngredientSlot ingredientPanel;


    Dictionary<string, UIIngredientSlot> createdIngredientPanelList = new();
    Dictionary<string, int> currentSelectIngredients = new Dictionary<string, int>();

    void Start()
    {
        currentSelectIngredients = new Dictionary<string, int>();
        createdIngredientPanelList = new Dictionary<string, UIIngredientSlot>();

        CreateIngredientPanelAll();
    }

    private void OnDestroy()
    {
        foreach(var item in createdIngredientPanelList.Values)
        {
            item.OnLeftClick -= OnLeftClickedIngredient;
            item.OnRightClick -= OnRightClickedIngredient;
        }
    }

    public void CreateIngredientPanelAll()
    {
        for (int i = 0; i < ingredientDataSO.ingredientData.Ingredients.Length; i++)
        {
            IngredientData data = ingredientDataSO.ingredientData.Ingredients[i];

            if (!createdIngredientPanelList.ContainsKey(data.Id))
            {
                var item = Instantiate(ingredientPanel, GetCategoryParent(data.Category));

                Sprite sprite = Resources.Load<Sprite>("UI/Ingredient/" + data.Id);

                item.SetData(data);
                item.SetImage(sprite);
                item.SetNameText(data.Name);
                item.ResetCount();

                item.OnLeftClick += OnLeftClickedIngredient;
                item.OnRightClick += OnRightClickedIngredient;

                createdIngredientPanelList.Add(data.Id, item);
            }
        }
    }

    public void OnLeftClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            currentSelectIngredients.Add(data.Id, 1);
        else
            currentSelectIngredients[data.Id]++;

        createdIngredientPanelList[data.Id].IncreaseCount(1);
        craftLiquidData.AddIngrediant(data, 10);
    }

    public void OnRightClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            return;
        else
            currentSelectIngredients[data.Id]--;


        createdIngredientPanelList[data.Id].IncreaseCount(-1);
        craftLiquidData.AddIngrediant(data, -10);
    }


    //추후 Enum 전환
    public Transform GetCategoryParent(string category) => category switch
    {
        "base" => baseParent,
        "liqueur" => liqueurParent,
        "garnish" => garnishParent,
        _ => etcParent,
    };

    public void ClearCurrentSelectIngredient()
    {
        foreach (var item in currentSelectIngredients)
        {
            createdIngredientPanelList[item.Key].ResetCount();
        }

        currentSelectIngredients.Clear();
        craftLiquidData.ResetIngrediant();
    }

    public void OnCategoryContents(int n)
    {
        baseParent.gameObject.SetActive(false);
        liqueurParent.gameObject.SetActive(false);
        garnishParent.gameObject.SetActive(false);
        etcParent.gameObject.SetActive(false);

        CategoryButtons category = categories[n];

        GetCategoryParent(category.category).gameObject.SetActive(true);

        selectBtn.anchoredPosition = new Vector2(category.selectXPos, selectBtn.anchoredPosition.y);
        selectBtnText.text = category.name;
    }
}
