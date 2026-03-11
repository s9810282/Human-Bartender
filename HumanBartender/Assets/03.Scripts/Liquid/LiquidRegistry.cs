using LiquidSimulation;

using UnityEngine;

/// <summary>
/// 모든 액체 데이터를 관리하는 레지스트리
/// 씬에 하나만 존재해야 함
/// </summary>
[CreateAssetMenu(fileName = "LiquidRegistry", menuName = "Liquid Simulation/Liquid Registry")]
public class LiquidRegistry : ScriptableObject
{
    public LiquidData[] liquids;

    public LiquidData GetData(LiquidType type)
    {
        foreach (var liquid in liquids)
        {
            if (liquid.liquidType == type) return liquid;
        }
        return null;
    }
}