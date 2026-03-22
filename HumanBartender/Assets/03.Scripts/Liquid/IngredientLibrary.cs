using LiquidSimulation;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;

[System.Serializable]
public class LiquidEntry
{
    public string id;
    public LiquidData liquidData;
}

[System.Serializable]
public class GanishEntry
{
    public string id;
    
}


[System.Serializable]
public class ETCEntry
{
    public string id;
    
}



public class IngredientLibrary : MonoBehaviour
{
    [SerializeField] List<LiquidEntry> liquidEntries = new();
    [SerializeField] List<GanishEntry> ganishEntries = new();
    [SerializeField] List<ETCEntry> eTCEntries = new();

    [SerializeField] LiquidData testData;

    public LiquidData GetLiquidData(string id)
    {
        foreach(LiquidEntry data in liquidEntries)
        {
            if (data.id == id)
                return data.liquidData;
        }

        Logger.Log("Not Regist Liquid Data");
        return null;
    }

    public LiquidData GetTestLiquid()
    {
        return testData;
    }
}
