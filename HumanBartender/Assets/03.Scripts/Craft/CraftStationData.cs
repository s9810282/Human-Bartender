using System.Collections.Generic;
using UnityEngine;

public class CraftingResult
{
    public bool isResult = false;
    public string selectMethod = "";
    public int acionCount = 0;
    public Color32 mixedColor = Color.white;
}

public class CraftIngrediantData
{
    public IngredientData data = default;
    public int value = 0;

    public CraftIngrediantData(IngredientData data, int value)
    {
        this.data = data;
        this.value = value;
    }
}


[CreateAssetMenu(fileName = "CraftLiquidData", menuName = "Scriptable Objects/CraftLiquidData")]
public class CraftStationData : ScriptableObject
{
    public CocktailData targetCocktailData;
    public List<CraftIngrediantData> ingredientDatas = new();
    
    public CraftingResult craftingResult = new();

    public void AddIngrediant(CraftIngrediantData initial)
    {
        ingredientDatas.Add(initial);
    }
    public void AddIngrediant(IngredientData data, int amount)
    {
        CraftIngrediantData initial = new CraftIngrediantData(data, amount);
        ingredientDatas.Add(initial);
    }

    public void ResetIngrediant()
    {
        ingredientDatas.Clear();
    }

    public void ResetCraftStation()
    {
        ResetIngrediant();
        targetCocktailData = default;
        craftingResult = new();
    }
}
