using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[Serializable]
public struct ExpenseConditionData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("flag_id")] public string FlagId { get; set; }
}

[Serializable]
public struct ExpenseData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("label")] public string Label { get; set; }
    [JsonProperty("amount")] public int Amount { get; set; }
    [JsonProperty("condition")] public OutsideCondition? Condition { get; set; }
}

[Serializable]
public struct DailySettlementData
{
    [JsonProperty("day")] public int Day { get; set; }
    [JsonProperty("expenses")] public ExpenseData[] Expenses { get; set; }
}

[Serializable]
public class SettlementDataBase
{
    [JsonProperty("daily_settlements")] public DailySettlementData[] DailySettlements { get; set; }
}

[CreateAssetMenu(fileName = "New SettlementDataSO", menuName = "Data/SettlementDataSO")]
public class SettlementDataSO : ScriptableObject
{
    public SettlementDataBase settlementData;
    public Dictionary<int, DailySettlementData> dailySettlement = new();
    public void Cached()
    {
        dailySettlement = settlementData.DailySettlements.ToDictionary(c => c.Day);
    }
}