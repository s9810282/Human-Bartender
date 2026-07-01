using Spine;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>재료 카테고리 탭 버튼 하나(표시 이름, 카테고리 키, 탭 아이콘, 선택 표시 X좌표)의 데이터.</summary>
[System.Serializable]
public struct CategoryButtons
{
    public string name;
    public string category;
    public Sprite tapSprite;
    public float selectXPos;
}

/// <summary>
/// 재료 선택 패널. 카테고리(base/liqueur/garnish/etc)별로 재료 슬롯을 생성해 배치하고,
/// 좌클릭(추가)/우클릭(제거) 입력을 받아 CraftStationData에 반영한다.
/// </summary>
public class UIIngredientPanel : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] IngredientDataSO ingredientDataSO;
    [SerializeField] CraftStationData craftLiquidData;

    [Header("UICocktail Contents")]
    [SerializeField] Transform baseParent;
    [SerializeField] Transform liqueurParent;
    [SerializeField] Transform garnishParent;
    [SerializeField] Transform etcParent;
    [Space(20f)]
    [Header("UICocktail Category")]
    [SerializeField] CategoryButtons[] categories;
    [SerializeField] Sprite[] tapSprites;
    [SerializeField] RectTransform selectUI;
    [SerializeField] TextMeshProUGUI selectUIText;
    [SerializeField] Toggle iceToggle;

    [SerializeField] UIIngredientSlot ingredientPanel;

    Dictionary<string, UIIngredientSlot> createdIngredientPanelList = new();
    Dictionary<string, int> currentSelectIngredients = new Dictionary<string, int>();

    IngredientData iceData;

    /// <summary>선택 상태를 초기화하고 전체 재료 슬롯을 생성한다.</summary>
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

    /// <summary>
    /// 데이터의 모든 재료에 대해 슬롯을 하나씩 생성해 카테고리별 부모에 배치한다.
    /// "ice"는 별도 토글(iceToggle)로 처리되므로 슬롯 생성에서 제외한다.
    /// </summary>
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


    
    /// <summary>얼음 토글 On/Off 처리. 별도 슬롯 없이 선택 목록과 제조대 데이터에 직접 반영한다.</summary>
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

    /// <summary>재료 슬롯 좌클릭(추가) 콜백. 선택 수량을 1 늘리고 제조대에 반영한다.</summary>
    public void OnLeftClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            currentSelectIngredients.Add(data.Id, 1);
        else
            currentSelectIngredients[data.Id]++;

        createdIngredientPanelList[data.Id].IncreaseCount(1);
        craftLiquidData.AddIngrediant(data, 1);
    }

    /// <summary>재료 슬롯 우클릭(제거) 콜백. 선택된 적 없으면 무시하고, 있으면 수량을 1 줄이고 제조대에 반영한다.</summary>
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
    /// <summary>카테고리 키 문자열에 대응하는 부모 Transform을 반환한다 (등록되지 않은 값은 etcParent로 처리).</summary>
    public Transform GetCategoryParent(string category) => category switch
    {
        "base" => baseParent,
        "liqueur" => liqueurParent,
        "garnish" => garnishParent,
        _ => etcParent,
    };

    /// <summary>선택된 재료(얼음 포함)를 모두 초기화하고 슬롯 UI/제조대 데이터를 리셋한다.</summary>
    public void ResetCurrentSelectIngredient()
    {
        OnIceIngredient(false);

        foreach (var item in currentSelectIngredients)
        {
            createdIngredientPanelList[item.Key].ResetCount();
        }

        iceToggle.isOn = false;
        currentSelectIngredients.Clear();
        craftLiquidData.ResetIngrediant();
    }

    /// <summary>카테고리 탭 전환. n번째 카테고리 부모만 활성화하고 선택 표시(밑줄 등) 위치를 이동시킨다.</summary>
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
