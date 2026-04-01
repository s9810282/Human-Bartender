using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct CraftEventData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("auto_open_recipe_ui")] public bool AutoOpenRecipeUi { get; set; }
    [JsonProperty("tutorial")] public TutorialData? Tutorial { get; set; }
    [JsonProperty("cutscenes")] public CraftCutSceneData CraftEnterCutscenes { get; set; }
    [JsonProperty("evaluation")] public EvaluationData Evaluation { get; set; }
    [JsonProperty("reactions")] public Dictionary<string, ReactionDetailData> Reactions { get; set; }
}

[Serializable]
public struct CraftCutSceneData
{
    [JsonProperty("craft_enter")] public CraftEnterData craftEnterData { get; set; }
    [JsonProperty("finish")] public CraftFinishData craftFinishData { get; set; }
}

[Serializable]
public struct CraftEnterData 
{
    [JsonProperty("shake")] public string Shake { get; set; }
    [JsonProperty("stir")] public string Stir { get; set; }
    [JsonProperty("build")] public string Build { get; set; }
}

[Serializable]
public struct CraftFinishData 
{
    [JsonProperty("default")] public string Default { get; set; }
    [JsonProperty("by_cocktail")] Dictionary<string, string> ByCocktail { get; set; }
    [JsonProperty("failed")] string Failed { get; set; }
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
    [JsonProperty("match_type")] public string MatchType { get; set; }
    [JsonProperty("match_values")] public string[] MatchValues { get; set; }
    [JsonProperty("require_craft_success")] public bool RequireCraftSuccess { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
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