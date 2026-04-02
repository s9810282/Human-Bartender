using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

[Serializable]
public struct PositionPreset
{
    [JsonProperty("anchor")] public string Anchor { get; set; }
    [JsonProperty("width")] public float Width { get; set; }
    [JsonProperty("height")] public float Height { get; set; }
    [JsonProperty("offset_x")] public float OffsetX { get; set; }
    [JsonProperty("offset_y")] public float OffsetY { get; set; }
}

[Serializable]
public struct LayoutPreset
{
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("slots")] public LayoutSlot[] Slots { get; set; }
}

[Serializable]
public struct LayoutSlot
{
    [JsonProperty("position")] public string Position { get; set; }
    [JsonProperty("width_override")] public float? WidthOverride { get; set; }
    [JsonProperty("height_override")] public float? HeightOverride { get; set; }
    [JsonProperty("anchor")] public string Anchor { get; set; }
    [JsonProperty("width")] public float? Width { get; set; }
    [JsonProperty("height")] public float? Height { get; set; }
    [JsonProperty("offset_x")] public float? OffsetX { get; set; }
    [JsonProperty("offset_y")] public float? OffsetY { get; set; }
    [JsonProperty("dim")] public float? Dim { get; set; }           // 신규: blur/effect 삭제 → dim으로 대체
}

[Serializable]
public struct EnterPreset
{
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("duration_default")] public float DurationDefault { get; set; }
}

[Serializable]
public struct ExitPreset                                             // 신규
{
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("duration_default")] public float DurationDefault { get; set; }
}

[Serializable]
public struct EffectPreset
{
    [JsonProperty("description")] public string Description { get; set; }
}

[Serializable]
public struct Cutscene
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("blocking")] public bool Blocking { get; set; }
    [JsonProperty("background")] public string Background { get; set; }
    [JsonProperty("steps")] public CutsceneStep[] Steps { get; set; }
}

[Serializable]
public struct CutsceneStep
{
    [JsonProperty("time")] public float Time { get; set; }
    [JsonProperty("action")] public string Action { get; set; }
    [JsonProperty("effect_type")] public string EffectType { get; set; }
    [JsonProperty("duration")] public float? Duration { get; set; }
    [JsonProperty("image")] public string Image { get; set; }
    [JsonProperty("position")] public string Position { get; set; }
    [JsonProperty("enter")] public string Enter { get; set; }
    [JsonProperty("enter_duration")] public float? EnterDuration { get; set; }
    [JsonProperty("enters")] public string[] Enters { get; set; }                // 신규: show_layout 이미지별 개별 enter
    [JsonProperty("enter_durations")] public float[] EnterDurations { get; set; } // 신규: show_layout 이미지별 개별 duration
    [JsonProperty("exit")] public string Exit { get; set; }
    [JsonProperty("exit_duration")] public float? ExitDuration { get; set; }
    [JsonProperty("sfx")] public string Sfx { get; set; }
    [JsonProperty("intensity")] public float? Intensity { get; set; }
    [JsonProperty("layout")] public string Layout { get; set; }
    [JsonProperty("images")] public string[] Images { get; set; }
}

[CreateAssetMenu(fileName = "CutSceneDataSO", menuName = "Data/CutSceneDataSO")]
public class CutSceneDataSO : ScriptableObject
{
    public CutSceneDataBase cutSceneData;
    public Dictionary<string, Cutscene> cachedById;

    public void Cached()
    {
        cachedById = GroupById();
    }

    public Dictionary<string, Cutscene> GroupById()
    {
        return cutSceneData.Cutscenes.ToDictionary(g => g.Id, g => g);
    }
}

[Serializable]
public class CutSceneDataBase
{
    [JsonProperty("position_presets")] public Dictionary<string, PositionPreset> PositionPresets { get; set; }
    [JsonProperty("layout_presets")] public Dictionary<string, LayoutPreset> LayoutPresets { get; set; }
    [JsonProperty("enter_presets")] public Dictionary<string, EnterPreset> EnterPresets { get; set; }
    [JsonProperty("exit_presets")] public Dictionary<string, ExitPreset> ExitPresets { get; set; }     // 신규
    [JsonProperty("effect_presets")] public Dictionary<string, EffectPreset> EffectPresets { get; set; }
    [JsonProperty("cutscenes")] public Cutscene[] Cutscenes { get; set; }
}
