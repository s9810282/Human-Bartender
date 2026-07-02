using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JetBrains.Annotations;

[JsonConverter(typeof(StringEnumConverter))]
public enum AnchorType
{
    [EnumMember(Value = "center")] Center,
    [EnumMember(Value = "left")] Left,
    [EnumMember(Value = "right")] Right,
    [EnumMember(Value = "top")] Top,
    [EnumMember(Value = "bottom")] Bottom,
    [EnumMember(Value = "top_left")] TopLeft,
    [EnumMember(Value = "top_right")] TopRight,
    [EnumMember(Value = "bottom_left")] BottomLeft,
    [EnumMember(Value = "bottom_right")] BottomRight,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ComicAnimType
{
    [EnumMember(Value = "fade")] Fade,
    [EnumMember(Value = "slide")] Slide,
    [EnumMember(Value = "scale")] Scale,
    [EnumMember(Value = "instant")] Instant,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum SlideDirection
{
    [EnumMember(Value = "left")] Left,
    [EnumMember(Value = "right")] Right,
    [EnumMember(Value = "top")] Top,
    [EnumMember(Value = "bottom")] Bottom,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ComicEffectType
{
    [EnumMember(Value = "flash")] Flash,
    [EnumMember(Value = "shake")] Shake,
    [EnumMember(Value = "zoom")] Zoom,
    [EnumMember(Value = "vignette")] Vignette,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum FlashColor
{
    [EnumMember(Value = "white")] White,
    [EnumMember(Value = "black")] Black,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum CutsceneType
{
    [EnumMember(Value = "sprite")] Sprite,
    [EnumMember(Value = "spine")] Spine,
    [EnumMember(Value = "comic")] Comic,
    [EnumMember(Value = "timeline")] Timeline,
}

[JsonConverter(typeof(StringEnumConverter))]
public enum EPositionPreset
{
    [EnumMember(Value = "full")] Full,
    [EnumMember(Value = "center")] Center,
    [EnumMember(Value = "center_small")] CenterSmall,
    [EnumMember(Value = "center_top")] CenterTop,
    [EnumMember(Value = "center_bottom")] CenterBottom,
    [EnumMember(Value = "left_panel")] LeftPanel,
    [EnumMember(Value = "right_panel")] RightPanel,
    [EnumMember(Value = "left_focus")] LeftFocus,
    [EnumMember(Value = "right_focus")] RightFocus,
    [EnumMember(Value = "top_left")] TopLeft,
    [EnumMember(Value = "top_right")] TopRight,
    [EnumMember(Value = "bottom_left")] BottomLeft,
    [EnumMember(Value = "bottom_right")] BottomRight,
}


[JsonConverter(typeof(StringEnumConverter))]
public enum ELayoutPreset
{
    [EnumMember(Value = "single")] Single,
    [EnumMember(Value = "split_2")] Split2,
    [EnumMember(Value = "split_3_horizontal")] Split3_horizontal,
    [EnumMember(Value = "main_left_sub_right")] MainLeftSubRight,
    [EnumMember(Value = "stack_vertical")] StackVertical,
    [EnumMember(Value = "dim_focus")] DimFocus,
}


[Serializable]
public struct PositionPreset
{
    [JsonProperty("anchor")] public AnchorType Anchor { get; set; }
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
    [JsonProperty("position")] public EPositionPreset Position { get; set; }
    [JsonProperty("anchor")] public AnchorType? Anchor { get; set; }

    [JsonProperty("width")] public float? Width { get; set; }
    [JsonProperty("height")] public float? Height { get; set; }
    [JsonProperty("offset_x")] public float? OffsetX { get; set; }
    [JsonProperty("offset_y")] public float? OffsetY { get; set; }
    [JsonProperty("dim")] public float? Dim { get; set; }
}


[Serializable]
public struct ComicPresets
{
    [JsonProperty("enter_presets")] public Dictionary<string, ComicEnterPreset> EnterPresets { get; set; }
    [JsonProperty("exit_presets")] public Dictionary<string, ComicExitPreset> ExitPresets { get; set; }
    [JsonProperty("effect_presets")] public Dictionary<string, ComicEffectPreset> EffectPresets { get; set; }
}

[Serializable]
public struct ComicEnterPreset
{
    [JsonProperty("type")] public ComicAnimType Type { get; set; }
    [JsonProperty("duration")] public float? Duration { get; set; }
    [JsonProperty("from")] public object From { get; set; }

    public SlideDirection? FromDirection
    {
        get
        {
            string s = From?.ToString();
            if (string.IsNullOrEmpty(s)) return null;
            return Enum.TryParse<SlideDirection>(s, true, out var dir) ? dir : null;
        }
    }

    public float? FromScale
    {
        get
        {
            if (From == null) return null;
            return float.TryParse(From.ToString(), out float v) ? v : null;
        }
    }
}

[Serializable]
public struct ComicExitPreset
{
    [JsonProperty("type")] public ComicAnimType Type { get; set; }
    [JsonProperty("duration")] public float? Duration { get; set; }
    [JsonProperty("to")] public object To { get; set; }

    public SlideDirection? ToDirection
    {
        get
        {
            string s = To?.ToString();
            if (string.IsNullOrEmpty(s)) return null;
            return Enum.TryParse<SlideDirection>(s, true, out var dir) ? dir : null;
        }
    }

    public float? ToScale
    {
        get
        {
            if (To == null) return null;
            return float.TryParse(To.ToString(), out float v) ? v : null;
        }
    }
}

[Serializable]
public struct ComicEffectPreset
{
    [JsonProperty("type")] public ComicEffectType Type { get; set; }
    [JsonProperty("duration")] public float? Duration { get; set; }
    [JsonProperty("color")] public FlashColor? Color { get; set; }
    [JsonProperty("intensity")] public float? Intensity { get; set; }
    [JsonProperty("scale")] public float? Scale { get; set; }
}


[Serializable]
public struct SpriteCutscene
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
    [JsonProperty("type")] public CutsceneType Type { get; set; }
    [JsonProperty("blocking")] public bool Blocking { get; set; }
    [JsonProperty("position")] public string Position { get; set; }   // position_presets 키
    [JsonProperty("scale")] public float? Scale { get; set; }
    [JsonProperty("loop")] public bool? Loop { get; set; }
}


[Serializable]
public struct Sprineutscene
{
   
}


[Serializable]
public struct Comicscene
{

}

/// <summary>
/// 컷씬 전체 데이터(위치 프리셋, 레이아웃 프리셋, 코믹 프리셋, 스프라이트 컷씬)를 보유하는 ScriptableObject.
/// Cached() 호출 시 id 기반 조회용 딕셔너리를 생성한다.
/// </summary>
[CreateAssetMenu(fileName = "CutSceneDataSO", menuName = "Data/CutSceneDataSO")]
public class CutSceneDataSO : ScriptableObject
{
    public CutSceneDataBase cutSceneData;

    public Dictionary<string, SpriteCutscene> spriteCutsceneCachedById;
    public Dictionary<string, SpriteCutscene> sprineCutsceneCachedById;
    public Dictionary<string, SpriteCutscene> comicCutsceneCachedById;

    public void Cached()
    {
        spriteCutsceneCachedById = Sprite_GroupById();
    }

    public Dictionary<string, SpriteCutscene> Sprite_GroupById()
    {
        return cutSceneData.SpriteCutscenes.ToDictionary(g => g.Id, g => g);
    }
}

[Serializable]
public class CutSceneDataBase
{
    [JsonProperty("position_presets")] public Dictionary<EPositionPreset, PositionPreset> PositionPresets { get; set; }
    [JsonProperty("layout_presets")] public Dictionary<ELayoutPreset, LayoutPreset> LayoutPresets { get; set; }
    [JsonProperty("comic_presets")] public ComicPresets ComicPresets { get; set; }
    [JsonProperty("cutscenes")] public SpriteCutscene[] SpriteCutscenes { get; set; }
}