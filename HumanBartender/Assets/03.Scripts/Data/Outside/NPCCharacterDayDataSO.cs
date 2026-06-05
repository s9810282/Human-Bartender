using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum SelectionType
{
    [EnumMember(Value = "sequential")] Sequential,
    [EnumMember(Value = "conditional")] Conditional,
    [EnumMember(Value = "random")] Random
}

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum DialogueNodeType
{
    [EnumMember(Value = "normal")] Normal,
    [EnumMember(Value = "system")] System,
    [EnumMember(Value = "choice")] Choice,
    [EnumMember(Value = "condition_branch")] ConditionBranch
}

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum TriggerType
{
    [EnumMember(Value = "set_flag")] SetFlag,
    [EnumMember(Value = "money_change")] MoneyChange
}

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum ConditionType
{
    [EnumMember(Value = "flag")] Flag,
    [EnumMember(Value = "money")] Money
}

public struct InteractData
{
    [JsonProperty("label")] public string Label { get; set; }
}
public struct SelectionConfigClass
{
    [JsonProperty("type")] public SelectionType Type { get; set; }
}

public struct OutsideConditionCheck
{
    [JsonProperty("type")] public EConditionCheckType Type { get; set; }
    [JsonProperty("flag_id")] public string FlagId { get; set; }
    [JsonProperty("character_id")] public string Character { get; set; }
    [JsonProperty("min")] public string Min { get; set; }
    [JsonProperty("value")] public bool Value { get; set; }
}

public struct OutsideCondition
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("conditions")] public OutsideConditionCheck Conditions { get; set; }
}




[Serializable]
public struct FlowData
{
    [JsonProperty("flow_id")] public string FlowId { get; set; }
    [JsonProperty("condition")] public OutsideCondition? Conditions { get; set; }
    [JsonProperty("dialogues")] public DialogueData[] Dialogues{ get; set; }
}

[Serializable]
public struct NPCDayData
{
    [JsonProperty("day")] public int Day{ get; set; }
    [JsonProperty("selection")] public SelectionConfigClass selection { get; set; }
    [JsonProperty("flows")] public FlowData[] FlowData { get; set; }
}


[Serializable]
public class NPCCharacterDay
{
    [JsonProperty("character_id")] public string Id { get; set; }
    [JsonProperty("interact")] public InteractData InteractData { get; set; }
    [JsonProperty("days")] public NPCDayData[] Days { get; set; }
}

[CreateAssetMenu(fileName = "NPCCharacterDayData", menuName = "Scriptable Objects/NPCCharacterDayData")]
public class NPCCharacterDayDataSO : ScriptableObject
{
    public NPCCharacterDay dayData;
}
