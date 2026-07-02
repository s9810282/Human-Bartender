using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

/// <summary>바텐더 숙련도 등급을 나타내는 열거형.</summary>
[Serializable]
[JsonConverter(typeof(StringEnumConverter))]
public enum ESkillTier
{
    [EnumMember(Value = "beginner")] Beginner = 0,
    [EnumMember(Value = "apprentice")] Apprentice,
    [EnumMember(Value = "intermediate")] Intermediate,
    [EnumMember(Value = "advanced")] Advanced,
    [EnumMember(Value = "master")] Master
}

// 1. 역직렬화를 위한 임시 클래스
public class SkillTierDataClass
{
    [JsonProperty("display_name")] public string DisplayName { get; set; }
    [JsonProperty("min")] public int Min { get; set; }
    [JsonProperty("max")] public int Max { get; set; }
}

// JSON의 최상위 "tiers" 키를 받기 위한 래퍼 클래스
public class SkillTierDataBase
{
    [JsonProperty("tiers")]
    public Dictionary<ESkillTier, SkillTierDataClass> Tiers { get; set; }
}



/// <summary>숙련도 등급별 범위(min/max) 데이터를 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "New SkillTierDataSO", menuName = "Data/SkillTierDataSO")]
public class SkillTierDataSO : ScriptableObject
{
    public SkillTierDataBase skillTier;
}