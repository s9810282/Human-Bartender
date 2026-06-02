using Spine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class KeywordToggle
{
    public Toggle toggle;
    public string keyword; 
}

public class UICocktailPanel : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("UICocktail Panel")]
    [SerializeField] GameObject cocktailObjs;
    [SerializeField] UICocktailSlot[] cocktailSlots;
    [SerializeField] private List<KeywordToggle> keywordToggles;
    [SerializeField] TMP_InputField searchFieldText;
    [SerializeField] string curSearchKeyword;
    [SerializeField] int currentPage = 0;

    [Header("UICocktail Detail Panel")]
    [SerializeField] UICocktailDetailPanel cocktailDetailPanel;

    private const int ITEMS_PER_PAGE = 6;

    private CocktailData[] allCocktailData;
    private List<CocktailData> filteredData;

    private Dictionary<string, Sprite> cocktailSprites = new Dictionary<string, Sprite>();



    public void Init()
    {
        allCocktailData = cocktailDataSO.cachedSortedByName;
        filteredData = allCocktailData.ToList();
        cocktailSprites = new Dictionary<string, Sprite>();

        for (int i = 0; i < cocktailSlots.Length; i++)
        {
            cocktailSlots[i].OnClick += OnDetailTab;
        }


        currentPage = 0;

        Refresh();
    }

    private void OnDestroy()
    {
        for (int i = 0; i < cocktailSlots.Length; i++)
        {
            cocktailSlots[i].OnClick -= OnDetailTab;
        }
    }

    public void ResetCocktailFilter()
    {
        cocktailDetailPanel.gameObject.SetActive(false);

        filteredData = allCocktailData.ToList();
        cocktailSprites = new Dictionary<string, Sprite>();

        for (int i = 0; i < keywordToggles.Count; i++)
        {
            keywordToggles[i].toggle.isOn = false;
        }

        searchFieldText.text = "";
        curSearchKeyword = "";

        currentPage = 0;
        Refresh();
    }


    #region Slot
    public void ApplyFilter()
    {
        string keyword = curSearchKeyword.Trim();

        var activeKeywords = keywordToggles
            .Where(kt => kt.toggle.isOn)
            .Select(kt => kt.keyword)
            .ToList();

        filteredData = allCocktailData.Where(slot =>
        {
            bool nameMatch = string.IsNullOrEmpty(keyword) ||
                             slot.Name.Contains(keyword);

            bool keywordMatch = activeKeywords.Count == 0 ||
                                activeKeywords.Any(k => slot.Keywords.Contains(k));

            return nameMatch && keywordMatch;
        }).ToList();


        if (filteredData.Count == 0)
            filteredData = new List<CocktailData>(allCocktailData);

        currentPage = 0;
        Refresh();
    }


    public void SetSearch(string keyword)
    {
        curSearchKeyword = keyword;
        ApplyFilter();
    }

    public void ChangePage(int page)
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filteredData.Count / (float)ITEMS_PER_PAGE));
        currentPage = Mathf.Clamp(currentPage + page, 0, totalPages - 1);
        Refresh();
    }
    private void Refresh()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filteredData.Count / (float)ITEMS_PER_PAGE));
        int start = currentPage * ITEMS_PER_PAGE;

        for (int i = 0; i < cocktailSlots.Length; i++)
        {
            int dataIndex = start + i;
            if (dataIndex < filteredData.Count)
            {
                Sprite sprite;

                if (cocktailSprites.ContainsKey(filteredData[dataIndex].Id))
                {
                    sprite = cocktailSprites[filteredData[dataIndex].Id];
                }
                else
                {
                    sprite = Resources.Load<Sprite>("UI/Cocktail/" + filteredData[dataIndex].Id);
                    cocktailSprites.Add(filteredData[dataIndex].Id, sprite);
                }

                cocktailSlots[i].OnOffSlot(true);
                cocktailSlots[i].SetData(filteredData[dataIndex]);
                cocktailSlots[i].SetNameText(filteredData[dataIndex].Name);
                cocktailSlots[i].SetImage(sprite);
            }
            else
            {
                cocktailSlots[i].OnOffSlot(false);
            }
        }
    }

    #endregion
    
    public void OnDetailTab(CocktailData data)
    {
        Sprite sprite;

        if (cocktailSprites.ContainsKey(data.Id))
        {
            sprite = cocktailSprites[data.Id];
        }
        else
        {
            sprite = Resources.Load<Sprite>("UI/Cocktail/" + data.Id);
            cocktailSprites.Add(data.Id, sprite);
        }

        cocktailDetailPanel.SetData(data);
        cocktailDetailPanel.SetImage(sprite);
        cocktailDetailPanel.SetCategorys();
        cocktailDetailPanel.SetIngrediantGauge();
        cocktailDetailPanel.SetSummary();

        cocktailDetailPanel.gameObject.SetActive(true);
        cocktailDetailPanel.SlideDetailPopup(true);

        cocktailObjs.gameObject.SetActive(false);
    }
}
