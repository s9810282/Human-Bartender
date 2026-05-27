using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum AffinityTier
{
    [EnumMember(Value = "very_low")] VeryLow,
    [EnumMember(Value = "low")] Low,
    [EnumMember(Value = "mid")] Mid,
    [EnumMember(Value = "high")] High,
    [EnumMember(Value = "very_high")] VeryHigh
}

[Serializable]
public struct AffinityTierData
{
    [JsonProperty("tier")] public AffinityTier Tier { get; set; }
    [JsonProperty("min")] public int? Min { get; set; }
    [JsonProperty("max")] public int? Max { get; set; }
}

public class CharacterAffinityData
{
    [JsonProperty("affinity")] public AffinityTierData[] Affinity { get; set; }
}

public class CharacterTierDataBase
{
    [JsonProperty("characters")] public Dictionary<string, CharacterAffinityData> Characters { get; set; }
}

[CreateAssetMenu(fileName = "New CharacterTierDataSO", menuName = "Data/CharacterTierDataSO")]
public class CharacterTierDataSO : ScriptableObject
{
    public CharacterTierDataBase characterTiers;
}