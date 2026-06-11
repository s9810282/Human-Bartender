using LiquidSimulation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using VContainer;

public class IngredientPanelR : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] IngredientDataSO ingredientDataSO;
    [SerializeField] CraftStationData craftLiquidData;
    [SerializeField] VoidEvent effectEvent;
    [SerializeField] VoidEvent disEffectEvent;

    [Header("UICocktail Ingredient Panel")]
    [SerializeField] Transform ingredientPanelParent;
    [SerializeField] UIIngredientSlot ingredientPanel;
    [SerializeField] Dictionary<string, UIIngredientSlot> createdIngredientPanelList = new();

    [Header("INFO")]
    [SerializeField] Text cocktailNameText;


    [Header("UI 할당 (인스펙터용)")]
    [SerializeField] private List<UIContentsText> inspectorContextList;
    [SerializeField] int currentContentsCount = 0;
    [SerializeField] int summaryTextSize;

    Queue<UIContentsText> contentsContextList = new();
    Queue<UIContentsText> usedUIContexts = new Queue<UIContentsText>();


    //ingredient data. id, cur Count
    Dictionary<string, int> currentSelectIngredients = new Dictionary<string, int>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        contentsContextList = new Queue<UIContentsText>(inspectorContextList);
        currentSelectIngredients = new Dictionary<string, int>();
        createdIngredientPanelList = new Dictionary<string, UIIngredientSlot>();

        usedUIContexts.Clear(); 
        inspectorContextList.Clear();

        ResetPanel();

        CreateIngredientPanelAll();
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    #region Ingredient

    public void CreateIngredientPanelAll()
    {
        for (int i = 0; i < ingredientDataSO.ingredientData.Ingredients.Length; i++)
        {
            IngredientData data = ingredientDataSO.ingredientData.Ingredients[i];
            
            if (!createdIngredientPanelList.ContainsKey(data.Id))
            {
                var item = Instantiate(ingredientPanel, ingredientPanelParent);

                Sprite sprite = Resources.Load<Sprite>("UI/Ingredient/" + data.Id);
                
                item.SetImage(sprite);
                item.SetNameText(data.Name);
                item.ResetCount();
                //item.GetButton().onClick.AddListener(() => OnClickedIngredient(data));
                //item.GetButton().onClick.AddListener(() => effectEvent?.Raise(new Void()));

                createdIngredientPanelList.Add(data.Id, item);
            }
        }
    }


    public void OnClickedIngredient(IngredientData data)
    {
        if (!currentSelectIngredients.ContainsKey(data.Id))
            currentSelectIngredients.Add(data.Id, 1);
        else
            currentSelectIngredients[data.Id]++;

        disEffectEvent?.Raise(new Void());
        createdIngredientPanelList[data.Id].IncreaseCount(1);
        craftLiquidData.AddIngrediant(data, 10);
    }


    public void ClearCurrentSelectIngredient()
    {
        foreach (var item in currentSelectIngredients)
        {
            createdIngredientPanelList[item.Key].ResetCount();
        }

        currentSelectIngredients.Clear();
        craftLiquidData.ResetIngrediant();
    }


    public void ResetPanel()
    {
        cocktailNameText.text = "";

        currentContentsCount = 0;

        foreach (var context in usedUIContexts)
        {
            context.gameObject.SetActive(false);
            contentsContextList.Enqueue(context);
        }

        usedUIContexts.Clear();
    }

    /// <summary>
    /// Recipe 탭에서 선택 버튼을 눌렀을 떄 호출되는 함수.
    /// </summary>
    /// <param name="data"></param>
    public void SelectRecipe(CocktailData data)
    {
        ResetPanel();

        cocktailNameText.text = data.Name;

        for (int i = 0; i < data.Recipe.Length; i++)
        {
            AddContentsSummary(data.Recipe[i].DisplayName + " : " + data.Recipe[i].Count);
        }
    }
    public void AddContentsSummary(string text)
    {
        if (contentsContextList.Count > 0)
        {
            var context = contentsContextList.Dequeue();
            context.gameObject.SetActive(true);
            context.transform.SetSiblingIndex(currentContentsCount);

            context.GetComponent<Text>().text = text;
            context.SetFontSize(summaryTextSize);

            usedUIContexts.Enqueue(context);
            currentContentsCount++;
        }
        else
        {
            Debug.LogWarning("사용 가능한 Context UI가 큐에 부족합니다!");
        }
    }

    

    #endregion
}
