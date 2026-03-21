using LiquidSimulation;
using System.Collections.Generic;
using Unity.Android.Gradle;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;
using VContainer;

public class IngredientPanel : MonoBehaviour
{
    [Header("SO DATA")]
    [SerializeField] CocktailDataSO cocktailDataSO;
    [SerializeField] VoidEvent effectEvent;
    [SerializeField] CraftStationData craftLiquidData;
    [Inject] LiquidLibrary liquidLibrary;

    [Header("UICocktail Ingredient Panel")]
    [SerializeField] Transform ingredientPanelParent;
    [SerializeField] UIIngredientPanel ingredientPanel;
    [SerializeField] Dictionary<RecipeIngredient, UIIngredientPanel> createdIngredientPanelList = new();

    [Header("INFO")]
    [SerializeField] Text cocktailNameText;


    [Header("UI 할당 (인스펙터용)")]
    [SerializeField] private List<UIContentsText> inspectorContextList;
    [SerializeField] int currentContentsCount = 0;
    [SerializeField] int summaryTextSize;

    Queue<UIContentsText> contentsContextList = new();
    Queue<UIContentsText> usedUIContexts = new Queue<UIContentsText>();


    Dictionary<RecipeIngredient, int> currentSelectIngredients = new Dictionary<RecipeIngredient, int>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        contentsContextList = new Queue<UIContentsText>(inspectorContextList);
        currentSelectIngredients = new Dictionary<RecipeIngredient, int>();
        createdIngredientPanelList = new Dictionary<RecipeIngredient, UIIngredientPanel>();

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
        for (int i = 0; i < cocktailDataSO.allCocktails.Length; i++)
        {
            CocktailData co = cocktailDataSO.allCocktails[i];

            for (int j = 0; j < co.recipe.Length; j++)
            {
                RecipeIngredient data = co.recipe[j];

                if (!createdIngredientPanelList.ContainsKey(data))
                {
                    var item = Instantiate(ingredientPanel, ingredientPanelParent);
                    item.SetImage(null);
                    item.SetNameText(data.ingredient);
                    item.ResetCount();
                    item.GetButton().onClick.AddListener(() => OnClickedIngredient(data));
                    item.GetButton().onClick.AddListener(() => effectEvent?.Raise(new Void()));
                    
                    createdIngredientPanelList.Add(data, item);
                }
            }
        }
    }


    public void OnClickedIngredient(RecipeIngredient data)
    {
        if (!currentSelectIngredients.ContainsKey(data))
            currentSelectIngredients.Add(data, 1);
        else
            currentSelectIngredients[data]++;

        LiquidType d = liquidLibrary.GetLiquidType(data.ingredient);
        LiquidData ld = liquidLibrary.GetLiquidData(d);

        createdIngredientPanelList[data].IncreaseCount();
        craftLiquidData.AddLiquid(ld, 10);
    }

    public void ClearCurrentIngredient()
    {
        foreach (var item in currentSelectIngredients)
        {
            createdIngredientPanelList[item.Key].ResetCount();
        }

        currentSelectIngredients.Clear();
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
    public void SelectRecipe(CocktailData data)
    {
        ResetPanel();

        cocktailNameText.text = data.name;

        for (int i = 0; i < data.recipe.Length; i++)
        {
            AddContentsSummary(data.recipe[i].ingredient + " : " + data.recipe[i].count);
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
