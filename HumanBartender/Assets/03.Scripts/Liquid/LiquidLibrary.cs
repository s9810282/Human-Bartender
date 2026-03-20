using LiquidSimulation;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;

[System.Serializable]
public class LiquidEntry
{
    public string name;
    public LiquidType key;
}


public class LiquidLibrary : MonoBehaviour
{
    [SerializeField] List<LiquidData> liquidDatas = new();
    [SerializeField] List<LiquidEntry> liquidEntry;

    public LiquidData GetLiquidData(LiquidType liquidType)
    {
        foreach(LiquidData data in liquidDatas)
        {
            if (data.liquidType == liquidType)
                return data;
        }

        Logger.Log("Not Regist Liquid Data");
        return null;
    }

    public LiquidType GetLiquidType(string jsonName)
    {
        foreach (LiquidEntry data in liquidEntry)
        {
            if (data.name == jsonName)
                return data.key;
        }

        Logger.Log($"Not Find LiquidType : {jsonName}");
        return LiquidType.Empty;
    }
}
