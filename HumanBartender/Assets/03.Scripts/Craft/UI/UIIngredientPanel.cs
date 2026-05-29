using Spine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField] RectTransform selectUI;
    [SerializeField] TextMeshProUGUI selectUIText;
    [SerializeField] Toggle iceToggle;

    [SerializeField] UIIngredientSlot ingredientPanel;

    Dictionary<string, UIIngredientSlot> createdIngredientPanelList = new();
    Dictionary<string, int> currentSelectIngredients = new Dictionary<string, int>();

    IngredientData iceData;

    public void Init()
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
            if (data.Id == "ice")
            {
                iceData = data;
                continue;
            }

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


    
    public void OnIceIngredient(bool isBool)
    {
        if (isBool)
        {
            currentSelectIngredients.Add(iceData.Id, 1);
            craftLiquidData.AddIngrediant(iceData, 1);
        }
        else
        {
            currentSelectIngredients.Remove(iceData.Id);
            craftLiquidData.AddIngrediant(iceData, -1);
        }
    }

    public void OnLeftClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            currentSelectIngredients.Add(data.Id, 1);
        else
            currentSelectIngredients[data.Id]++;

        createdIngredientPanelList[data.Id].IncreaseCount(1);
        craftLiquidData.AddIngrediant(data, 1);
    }

    public void OnRightClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            return;
        else
            currentSelectIngredients[data.Id]--;

        createdIngredientPanelList[data.Id].IncreaseCount(-1);
        craftLiquidData.AddIngrediant(data, -1);

        if (currentSelectIngredients[data.Id] <= 0)
            currentSelectIngredients.Remove(data.Id);
    }


    //추후 Enum 전환
    public Transform GetCategoryParent(string category) => category switch
    {
        "base" => baseParent,
        "liqueur" => liqueurParent,
        "garnish" => garnishParent,
        _ => etcParent,
    };

    public void ResetCurrentSelectIngredient()
    {
        foreach (var item in currentSelectIngredients)
        {
            createdIngredientPanelList[item.Key].ResetCount();
        }

        iceToggle.isOn = false;
        currentSelectIngredients.Clear();
        craftLiquidData.ResetIngrediant();

        OnCategoryContents(0);
    }

    public void OnCategoryContents(int n)
    {
        baseParent.gameObject.SetActive(false);
        liqueurParent.gameObject.SetActive(false);
        garnishParent.gameObject.SetActive(false);
        etcParent.gameObject.SetActive(false);

        CategoryButtons category = categories[n];

        GetCategoryParent(category.category).gameObject.SetActive(true);

        selectUI.anchoredPosition = new Vector2(category.selectXPos, selectUI.anchoredPosition.y);
        selectUIText.text = category.name;
    }
}
