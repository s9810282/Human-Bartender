using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>칵테일 평가 룰의 매칭 방식을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EMatchType
{
    None = 0,

    [EnumMember(Value = "cocktail_id")]
    Id,

    [EnumMember(Value = "keyword")]
    Keyword,

    [EnumMember(Value = "base")]
    Base,

    [EnumMember(Value = "any")]
    Any,
}


/// <summary>칵테일 제조 반응 판정 등급을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EVerdictType
{
    None = 0,

    [EnumMember(Value = "perfect")]
    perfect,

    [EnumMember(Value = "good")]
    Good,

    [EnumMember(Value = "normal")]
    Normal,

    [EnumMember(Value = "bad")]
    Bad,

    [EnumMember(Value = "miss")]
    Miss,
}

/// <summary>미니게임 제조 성공 여부를 나타내는 열거형(완벽/보통/실패).</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EResultType
{
    [EnumMember(Value = "perfect")]
    Perfect,
    [EnumMember(Value = "normal")]
    Normal,
    [EnumMember(Value = "fail")]
    Failed
}

[Serializable]
public struct CraftEventData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("order")] public string Order { get; set; }
    [JsonProperty("targetId")] public string TargetId { get; set; }
    [JsonProperty("auto_open_recipe_ui")] public bool AutoOpenRecipeUi { get; set; }
    [JsonProperty("tutorial")] public TutorialData? Tutorial { get; set; }
    [JsonProperty("cutscenes")] public CraftCutSceneData CraftCutscenes { get; set; }
    [JsonProperty("evaluation")] public EvaluationData Evaluation { get; set; }
    [JsonProperty("reactions")] public Dictionary<string, ReactionDetailData> Reactions { get; set; }
}

[Serializable]
public struct CraftCutSceneData
{
    //체크하기.
    [JsonProperty("craft_enter")] public Dictionary<string, string> craftEnterData { get; set; }
    [JsonProperty("finish")] public CraftFinishData craftFinishData { get; set; }
}


[Serializable]
public struct CraftFinishData 
{
    [JsonProperty("default")] public string Default { get; set; }
    [JsonProperty("failed")] public string Failed { get; set; }
}





[Serializable]
public struct TutorialData
{
    [JsonProperty("enabled")] public bool Enabled { get; set; }
    [JsonProperty("steps")] public TutorialStepData[] Steps { get; set; }
}

[Serializable]
public struct TutorialStepData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("target")] public string Target { get; set; }
    [JsonProperty("text")] public string Text { get; set; }
}

[Serializable]
public struct EvaluationData
{
    [JsonProperty("craft_tolerance")] public int CraftTolerance { get; set; }
    [JsonProperty("rules")] public VerdictRule[] Rules { get; set; }
}

[Serializable]
public struct VerdictRule
{
    [JsonProperty("result")] public string Result { get; set; }
    [JsonProperty("match_type")] public EMatchType MatchType { get; set; }
    [JsonProperty("match_values")] public string[] MatchValues { get; set; }
    [JsonProperty("require_craft_success")] public EResultType RequireCraftSuccess { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
}



[Serializable]
public struct ReactionDetailData
{
    [JsonProperty("verdict")] public EVerdictType Verdict { get; set; }
    [JsonProperty("dialogue_id")] public string DialogueId { get; set; }
    [JsonProperty("cutscene_id")] public string CutsceneId { get; set; }
    [JsonProperty("skill")] public int Skill { get; set; }
    [JsonProperty("affinity")] public int Affinity { get; set; }
    [JsonProperty("karma")] public int Karma { get; set; }
    [JsonProperty("payment")] public PaymentData Payment { get; set; }
}

public struct PaymentData
{
    [JsonProperty("pay_price")] public bool Payprice { get; set; }
    [JsonProperty("tip_rate")] public float TipRate { get; set; }
}

/// <summary>제조 이벤트 데이터 배열을 보유하는 ScriptableObject. id로 CraftEventData를 조회할 수 있다.</summary>
[CreateAssetMenu(fileName = "CraftDataBase", menuName = "Data/CraftDataBase")]
public class CraftDataSO : ScriptableObject
{
    public CraftDataBase craftData;

    public CraftEventData GetCraftDataByID(string id)
    {
        foreach (var item in craftData.CraftEvents)
        {
            if (item.Id == id)
                return item;
        }

        return default;
    }
}

[Serializable]
public class CraftDataBase
{
    [JsonProperty("craft_events")] public CraftEventData[] CraftEvents { get; set; }
}