using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;
using UnityEngine;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum ESelectionType
{
    [EnumMember(Value = "sequential")] Sequential,
    [EnumMember(Value = "conditional")] Conditional,
    [EnumMember(Value = "random")] Random
}


public struct InteractData
{
    [JsonProperty("label")] public string Label { get; set; }
}
public struct SelectionConfigData
{
    [JsonProperty("type")] public ESelectionType Type { get; set; }
}



public struct OutsideCondition
{
    [JsonProperty("type")] public EConditionCheckType Type { get; set; }
    [JsonProperty("flag_id")] public string FlagId { get; set; }
    [JsonProperty("character_id")] public string Character { get; set; }
    [JsonProperty("min")] public int Min { get; set; }
    [JsonProperty("value")] public bool Value { get; set; }
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
    [JsonProperty("route")] public EGameFlow Route { get; set; }
    [JsonProperty("spawn_condition")] public OutsideCondition? SpawnCondotion { get; set; }
    [JsonProperty("selection")] public SelectionConfigData Selection { get; set; }
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
