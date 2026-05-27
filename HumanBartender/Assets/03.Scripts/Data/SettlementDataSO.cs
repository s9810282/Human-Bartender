using System;
using UnityEngine;
using Newtonsoft.Json;

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
    [JsonProperty("condition")] public ExpenseConditionData? Condition { get; set; }
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
}