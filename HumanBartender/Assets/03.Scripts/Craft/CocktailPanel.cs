using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CocktailPanel : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("UICocktail Text Panel")]
    [SerializeField] ScrollRect cocktailTextScollRect;
    [SerializeField] Transform cocktailTextPanelInitialParent;
    [SerializeField] Transform cocktailTextPanelTasteParent;
    [SerializeField] Transform cocktailTextPanelBaseParent;
    [SerializeField] Transform cocktailTextPanelMethodParent;
    [SerializeField] Transform cocktailTextPanelStyleParent;

    [SerializeField] UICocktailStyleTextPanel cocktailTextPanel;

    [Header("UICocktail Panel")]
    [SerializeField] Dictionary<CocktailData, UICocktailMenuPanel> cocktailPanelList = new();
    [SerializeField] Transform cocktailPanelParent;
    [SerializeField] UICocktailMenuPanel cocktailPanel;

    [Header("UICocktail Detail Panel")]
    [SerializeField] UICocktailDetailPanel cocktailDetailPanel;



    void Start()
    {
        CreateCocktailTextPanelAll();
        CreateCocktailPanelAll();
    }


    #region Cocktail Recipe

    public void CreateCocktailTextPanelAll()
    {
        foreach(var item in cocktailDataSO.cachedByInitialConsonant)
        {
            var panel = Instantiate(cocktailTextPanel, cocktailTextPanelInitialParent);
            panel.GetUIContentsText().SetText(item.Key);
            panel.GetUIContentsText().SetFontSize(25);
            panel.GetButton().onClick.AddListener(() => OnIniatialTabClicked(item.Key));
        }

        foreach (var item in cocktailDataSO.cachedByTaste)
        {
            var panel = Instantiate(cocktailTextPanel, cocktailTextPanelTasteParent);
            panel.GetUIContentsText().SetText(item.Key);
            panel.GetUIContentsText().SetFontSize(25);
            panel.GetButton().onClick.AddListener(() => OnTasteTabClicked(item.Key));
        }

        foreach (var item in cocktailDataSO.cachedByBase)
        {
            var panel = Instantiate(cocktailTextPanel, cocktailTextPanelBaseParent);
            panel.GetUIContentsText().SetText(item.Key);
            panel.GetUIContentsText().SetFontSize(25);
            panel.GetButton().onClick.AddListener(() => OnBaseTabClicked(item.Key));
        }

        foreach (var item in cocktailDataSO.cachedByMethod)
        {
            var panel = Instantiate(cocktailTextPanel, cocktailTextPanelMethodParent);
            panel.GetUIContentsText().SetText(item.Key);
            panel.GetUIContentsText().SetFontSize(25);
            panel.GetButton().onClick.AddListener(() => OnMethodTabClicked(item.Key));
        }

        foreach (var item in cocktailDataSO.cachedByStyle)
        {
            var panel = Instantiate(cocktailTextPanel, cocktailTextPanelStyleParent);
            panel.GetUIContentsText().SetText(item.Key);
            panel.GetUIContentsText().SetFontSize(25);
            panel.GetButton().onClick.AddListener(() => OnStyleTabClicked(item.Key));
        }

        cocktailTextPanelInitialParent.gameObject.SetActive(false);
        cocktailTextPanelBaseParent.gameObject.SetActive(false);
        cocktailTextPanelTasteParent.gameObject.SetActive(false);
        cocktailTextPanelStyleParent.gameObject.SetActive(false);
        cocktailTextPanelMethodParent.gameObject.SetActive(false);
    }
    public void CreateCocktailPanelAll()
    {
        for(int i = 0; i < cocktailDataSO.cachedSortedByName.Length; i++)
        {
            CocktailData data = cocktailDataSO.cachedSortedByName[i];

            var panel =  Instantiate(cocktailPanel, cocktailPanelParent);
            panel.SetImage(null);
            panel.SetText(data.Name);
            panel.gameObject.SetActive(false);
            panel.GetButton().onClick.AddListener(() => OnCocktailPanelTabClicked(data));

            cocktailPanelList.Add(data, panel);
        }
    }


    /// <summary>
    /// 키는건 걍 버튼에서 처리 귀찮음.
    /// </summary>
    public void DisableTextPanelAll()
    {
        cocktailTextPanelInitialParent.gameObject.SetActive(false);
        cocktailTextPanelBaseParent.gameObject.SetActive(false);
        cocktailTextPanelTasteParent.gameObject.SetActive(false);
        cocktailTextPanelStyleParent.gameObject.SetActive(false);
        cocktailTextPanelMethodParent.gameObject.SetActive(false);
    }


    #region Cocktail Panel
    public void ClearAndReturnContents()
    {
        foreach(var item in cocktailPanelList)
        {
            item.Value.gameObject.SetActive(false);
        }
    }
    public void OnIniatialTabClicked(string targetInitail)
    {
        if (cocktailDataSO.cachedByInitialConsonant.TryGetValue(targetInitail, out CocktailData[] resultList))
        {
            cocktailTextPanelInitialParent.gameObject.SetActive(true);
            UpdateUIWithCocktailTextPanels(resultList);
        }
        else
        {
            Debug.Log($"{targetInitail} 이니셜을 가진 칵테일이 없습니다!");
        }
    }
    public void OnTasteTabClicked(string targetTaste)
    {
        if (cocktailDataSO.cachedByTaste.TryGetValue(targetTaste, out CocktailData[] resultList))
        {
            cocktailTextPanelTasteParent.gameObject.SetActive(true);
            UpdateUIWithCocktailTextPanels(resultList);
        }
        else
        {
            Debug.Log($"{targetTaste} 맛을 가진 칵테일이 없습니다!");
        }
    }
    public void OnBaseTabClicked(string targetBase)
    {
        if (cocktailDataSO.cachedByBase.TryGetValue(targetBase, out CocktailData[] resultList))
        {
            cocktailTextPanelBaseParent.gameObject.SetActive(true);
            UpdateUIWithCocktailTextPanels(resultList);
        }
        else
        {
            Debug.Log($"{targetBase} 베이스를 가진 칵테일이 없습니다!");
        }
    }
    public void OnStyleTabClicked(string targetStyle)
    {
        if (cocktailDataSO.cachedByStyle.TryGetValue(targetStyle, out CocktailData[] resultList))
        {
            cocktailTextPanelStyleParent.gameObject.SetActive(true);
            UpdateUIWithCocktailTextPanels(resultList);
        }
        else
        {
            Debug.Log($"{targetStyle} 스타일을 가진 칵테일이 없습니다!");
        }
    }
    public void OnMethodTabClicked(string targetMethod)
    {
        if (cocktailDataSO.cachedByMethod.TryGetValue(targetMethod, out CocktailData[] resultList))
        {
            cocktailTextPanelMethodParent.gameObject.SetActive(true);
            UpdateUIWithCocktailTextPanels(resultList);
        }
        else
        {
            Debug.Log($"{targetMethod} 메소드를 가진 칵테일이 없습니다!");
        }
    }
    private void UpdateUIWithCocktailTextPanels(CocktailData[] listToShow)
    {
        ClearAndReturnContents();

        for (int i = 0; i < listToShow.Length; i++)
        {
            cocktailPanelList[listToShow[i]].gameObject.SetActive(true);
        }
    }

    #endregion


    #region Cocktail Detail Panel
    public void OnCocktailPanelTabClicked(CocktailData data)
    {
        //Cocktail Panel을 클릭 시 Detail Panel에 정보설정
        
        cocktailDetailPanel.ResetPanel();

        cocktailDetailPanel.SetCocktailData(data);
        cocktailDetailPanel.SetImage(null);
        cocktailDetailPanel.SetCocktailName(data.Name);

        cocktailDetailPanel.AddContentsTitle("레시피");
        for(int i = 0; i < data.Recipe.Length; i++)
        {
            cocktailDetailPanel.AddContentsSummary(data.Recipe[i].Ingredient + " : " + data.Recipe[i].Count);
        }
        
        cocktailDetailPanel.AddContentsTitle("특성");

        string s = "";
        for (int i = 0; i < data.Keywords.Length; i++)
            s += data.Keywords[i] + " ";
        cocktailDetailPanel.AddContentsSummary(s);        


        cocktailDetailPanel.AddContentsTitle("설명");
        cocktailDetailPanel.AddContentsSummary(data.FlavorText);
    }

    #endregion

    #endregion


   
}
