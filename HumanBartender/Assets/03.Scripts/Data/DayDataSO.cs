using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct SceneData
{
    [JsonProperty("scene_id")] public string SceneId { get; set; }
    [JsonProperty("customer")] public string Customer { get; set; }
    [JsonProperty("dialogues")] public DialogueData[] Dialogues { get; set; }
}

[Serializable]
public struct DialogueData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("speaker")] public string Speaker { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("text")] public string Text { get; set; }
    [JsonProperty("expression")] public string Expression { get; set; }
    [JsonProperty("next")] public string Next { get; set; }
    [JsonProperty("choices")] public ChoiceData[] Choices { get; set; }
    [JsonProperty("trigger")] public TriggerData? Trigger { get; set; }
}

[Serializable]
public struct ChoiceData
{
    [JsonProperty("text")] public string Text { get; set; }
    [JsonProperty("next")] public string Next { get; set; }
    [JsonProperty("choice_effect")] public string ChoiceEffect { get; set; }
}

[Serializable]
public struct TriggerData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("data")] public TriggerDetailData Data { get; set; }
}

[Serializable]
public struct TriggerDetailData
{
    [JsonProperty("character_id")] public string CharacterId { get; set; }
    [JsonProperty("sfx")] public string Sfx { get; set; }
    [JsonProperty("bgm")] public string Bgm { get; set; }
    [JsonProperty("animation")] public string Animation { get; set; }
    [JsonProperty("craft_event_id")] public string CraftEventId { get; set; }
    [JsonProperty("cutscene_id")] public string CutsceneId { get; set; }
    [JsonProperty("resume_after")] public bool? ResumeAfter { get; set; }
}

[Serializable]
public class DayDatabBase
{
    [JsonProperty("day")] public int Day { get; set; }
    [JsonProperty("scenes")] public SceneData[] Scenes { get; set; }
}

[CreateAssetMenu(fileName = "DayDatabBase", menuName = "Data/DayDatabBase")]
public class DayDataSO : ScriptableObject
{
    public DayDatabBase dayData;
}