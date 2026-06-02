using System.Collections.Generic;
using UnityEngine;

public class CraftingResult
{
    public bool isResult = false;
    public string selectMethod = "";
    public int actionFailCount = 0;
    public int limitFailCount = 0;
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
    public string targetCocktailId;
    public int targetCraft_tolerance;
    public CocktailData targetCocktailData;

    public Dictionary<string, CraftIngrediantData> ingredientDatas = new();
    
    public CraftingResult craftingResult = new();

    public void AddIngrediant(IngredientData data, int amount)
    {
        if (!ingredientDatas.ContainsKey(data.Id))
        {
            if (amount <= 0) return;

            CraftIngrediantData initial = new CraftIngrediantData(data, amount);
            ingredientDatas.Add(data.Id, initial);
        }
        else
        {
            ingredientDatas[data.Id].value += amount;

            if(ingredientDatas[data.Id].value <= 0)
                ingredientDatas.Remove(data.Id);
        }
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
