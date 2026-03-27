using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct CraftEventData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("auto_open_recipe_ui")] public bool AutoOpenRecipeUi { get; set; }
    [JsonProperty("craft_enter_cutscenes")] public CraftCutSceneData CraftEnterCutscenes { get; set; }
    [JsonProperty("tutorial")] public TutorialData? Tutorial { get; set; }
    [JsonProperty("evaluation")] public EvaluationData Evaluation { get; set; }
    [JsonProperty("reactions")] public ReactionsData Reactions { get; set; }
}

[Serializable]
public struct CraftCutSceneData
{
    [JsonProperty("shake")] public string Shake { get; set; }
    [JsonProperty("stir")] public string Stir { get; set; }
    [JsonProperty("build")] public string Build { get; set; }
    [JsonProperty("serve_cutscene")] public string ServeCutscene { get; set; }
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
    [JsonProperty("rules")] public RuleData[] Rules { get; set; }
    [JsonProperty("default_grade")] public string DefaultGrade { get; set; }
    [JsonProperty("craft_penalty")] public bool CraftPenalty { get; set; }
}

[Serializable]
public struct RuleData
{
    [JsonProperty("grade")] public string Grade { get; set; }
    [JsonProperty("match_type")] public string MatchType { get; set; }
    [JsonProperty("match_values")] public string[] MatchValues { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
}

[Serializable]
public struct ReactionsData
{
    [JsonProperty("S")] public ReactionDetailData S { get; set; }
    [JsonProperty("A")] public ReactionDetailData A { get; set; }
    [JsonProperty("B")] public ReactionDetailData B { get; set; }
    [JsonProperty("C")] public ReactionDetailData C { get; set; }
}

[Serializable]
public struct ReactionDetailData
{
    [JsonProperty("dialogue_id")] public string DialogueId { get; set; }
    [JsonProperty("cutscene_id")] public string CutsceneId { get; set; }
    [JsonProperty("affinity")] public int Affinity { get; set; }
    [JsonProperty("karma")] public int Karma { get; set; }
}

[CreateAssetMenu(fileName = "CraftDataBase", menuName = "Data/CraftDataBase")]
public class CraftDataSO : ScriptableObject
{
    public CraftDataBase craftData;
}

[Serializable]
public class CraftDataBase
{
    [JsonProperty("craft_events")] public CraftEventData[] CraftEvents { get; set; }
}