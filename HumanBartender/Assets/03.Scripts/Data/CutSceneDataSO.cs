using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[JsonConverter(typeof(StringEnumConverter))]
public enum AnchorType
{
    [EnumMember(Value = "center")] Center,
    [EnumMember(Value = "left")] Left,
    [EnumMember(Value = "right")] Right,
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
}


[Serializable]
public struct PositionPreset
{
    [JsonProperty("anchor")] public AnchorType Anchor { get; set; }
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
    [JsonProperty("asset")] public string Asset { get; set; }
    [JsonProperty("position")] public string Position { get; set; }   // position_presets 키
    [JsonProperty("scale")] public float? Scale { get; set; }
    [JsonProperty("loop")] public bool? Loop { get; set; }
    [JsonProperty("enter")] public string Enter { get; set; }   // comic_presets.enter_presets 키
    [JsonProperty("enter_duration")] public float? EnterDuration { get; set; }
    [JsonProperty("exit")] public string Exit { get; set; }   // comic_presets.exit_presets 키
    [JsonProperty("exit_duration")] public float? ExitDuration { get; set; }
}


[Serializable]
public struct Sprineutscene
{
   
}


[Serializable]
public struct Comicscene
{

}

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
    [JsonProperty("position_presets")] public Dictionary<string, PositionPreset> PositionPresets { get; set; }
    [JsonProperty("layout_presets")] public Dictionary<string, LayoutPreset> LayoutPresets { get; set; }
    [JsonProperty("comic_presets")] public ComicPresets ComicPresets { get; set; }
    [JsonProperty("cutscenes")] public SpriteCutscene[] SpriteCutscenes { get; set; }
}