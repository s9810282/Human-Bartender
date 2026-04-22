using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Runtime.Serialization;
using UnityEngine;

[JsonConverter(typeof(StringEnumConverter))]
public enum EDialogueType
{
    None,

    [EnumMember(Value = "system")]
    System,

    [EnumMember(Value = "monologue")]
    Monologue,

    [EnumMember(Value = "normal")]
    Normal,
}

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
    [JsonProperty("type")] public EDialogueType Type { get; set; }
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
    [JsonProperty("slot")] public string Slot { get; set; }

    
    [JsonProperty("sfx")] public string Sfx { get; set; }
    [JsonProperty("sfx_mode")] public string SfxMode { get; set; }
    [JsonProperty("bgm")] public string Bgm { get; set; }

    
    [JsonProperty("animation")] public string Animation { get; set; }
    [JsonProperty("anim_id")] public string AnimId { get; set; }

    
    [JsonProperty("craft_event_id")] public string CraftEventId { get; set; }
    [JsonProperty("cutscene_id")] public string CutsceneId { get; set; }

    
    [JsonProperty("effect_type")] public string EffectType { get; set; }
    [JsonProperty("duration")] public float? Duration { get; set; }

    
    [JsonProperty("enter_effect")] public string EnterEffect { get; set; }
    [JsonProperty("enter_duration")] public float? EnterDuration { get; set; }

    
    [JsonProperty("exit_effect")] public string ExitEffect { get; set; }
    [JsonProperty("exit_duration")] public float? ExitDuration { get; set; }

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