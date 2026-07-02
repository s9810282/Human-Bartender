using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>캐릭터 호감도 등급을 나타내는 열거형. JSON에서 snake_case로 직렬화된다.</summary>
[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum EAffinityTier
{
    [EnumMember(Value = "very_low")] Very_Low = 0,
    [EnumMember(Value = "low")] Low,
    [EnumMember(Value = "mid")] Mid,
    [EnumMember(Value = "high")] High,
    [EnumMember(Value = "very_high")] Very_High
}

[Serializable]
public struct AffinityTierData
{
    [JsonProperty("tier")] public EAffinityTier Tier { get; set; }
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

/// <summary>캐릭터별 호감도 등급 범위 데이터를 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "New CharacterTierDataSO", menuName = "Data/CharacterTierDataSO")]
public class CharacterTierDataSO : ScriptableObject
{
    public CharacterTierDataBase characterTiers;
}