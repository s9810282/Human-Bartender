using Spine;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>키워드 필터 토글 UI와 실제 필터링에 쓰일 키워드 문자열을 짝지은 데이터.</summary>
[System.Serializable]
public class KeywordToggle
{
    public Toggle toggle;
    public string keyword;
}

/// <summary>
/// 칵테일 목록(도감) 패널. 이름 검색/키워드 토글로 필터링하고 페이지 단위(ITEMS_PER_PAGE)로 슬롯에 표시하며,
/// 슬롯 클릭 시 UICocktailDetailPanel에 상세 정보를 채워 보여준다. 로드한 스프라이트는 캐시(cocktailSprites)해 재사용한다.
/// </summary>
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



    /// <summary>전체 칵테일 데이터를 로드하고 슬롯 클릭 이벤트를 구독한 뒤 첫 페이지를 표시한다.</summary>
    public void Init()
    {
        allCocktailData = cocktailDataSO.cachedSortedByName;
        filteredData = allCocktailData.ToList();
        cocktailSprites = new Dictionary<string, Sprite>();

        cocktailDetailPanel.Init();

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

    /// <summary>검색어/키워드 토글/페이지를 모두 초기화하고 상세 패널을 닫는다.</summary>
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
    /// <summary>
    /// 검색어(이름 포함 여부)와 활성화된 키워드 토글을 AND 조건(이름 매칭 AND 키워드 매칭)으로 적용해 목록을 갱신한다.
    /// 필터 결과가 0건이면 필터를 무시하고 전체 목록을 보여준다.
    /// </summary>
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


    /// <summary>검색어를 갱신하고 필터를 재적용한다.</summary>
    public void SetSearch(string keyword)
    {
        curSearchKeyword = keyword;
        ApplyFilter();
    }

    /// <summary>현재 페이지에서 page만큼 이동한다 (범위를 벗어나지 않도록 clamp).</summary>
    public void ChangePage(int page)
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(filteredData.Count / (float)ITEMS_PER_PAGE));
        currentPage = Mathf.Clamp(currentPage + page, 0, totalPages - 1);
        Refresh();
    }
    /// <summary>현재 페이지에 해당하는 데이터를 슬롯에 채운다. 스프라이트는 최초 로드 시 캐시에 저장해 재사용한다.</summary>
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
    
    /// <summary>슬롯 클릭 콜백. 해당 칵테일 상세 정보를 상세 패널에 채우고 펼쳐서 보여준다.</summary>
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
