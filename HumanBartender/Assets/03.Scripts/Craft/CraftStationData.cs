using System.Collections.Generic;
using UnityEngine;

public class CraftingResult
{
    public int acionCount = 0;
    public Color32 mixedColor = Color.white;
}

[CreateAssetMenu(fileName = "CraftLiquidData", menuName = "Scriptable Objects/CraftLiquidData")]
public class CraftStationData : ScriptableObject
{
    public List<InitialLiquid> liquids = new();
    public CraftingResult craftingResult = new();

    public void AddLiquid(InitialLiquid initial)
    {
        liquids.Add(initial);
    }
    public void AddLiquid(LiquidData data, int amount)
    {
        InitialLiquid initial = new InitialLiquid(data, amount);
        liquids.Add(initial);
    }

    public void ResetLiquid()
    {
        liquids.Clear();
    }
}
