using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

/// <summary>script 스텝(say/move/fx/...)의 종류를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewStepType
{
    [EnumMember(Value = "say")] Say,
    [EnumMember(Value = "move")] Move,
    [EnumMember(Value = "fx")] Fx,
    [EnumMember(Value = "enter")] Enter,
    [EnumMember(Value = "exit")] Exit,
    [EnumMember(Value = "sfx")] Sfx,
    [EnumMember(Value = "choice")] Choice,
    [EnumMember(Value = "order")] Order,
    [EnumMember(Value = "craft")] Craft,
    [EnumMember(Value = "serve")] Serve,
    [EnumMember(Value = "effect")] Effect,
    [EnumMember(Value = "end_part")] EndPart,
    [EnumMember(Value = "timeline")] Timeline,
}

/// <summary>씬이 진행되는 하루 중 구간을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewScenePhase
{
    [EnumMember(Value = "intro")] Intro,
    [EnumMember(Value = "home")] Home,
    [EnumMember(Value = "street")] Street,
    [EnumMember(Value = "commute_in")] CommuteIn,
    [EnumMember(Value = "commute_out")] CommuteOut,
    [EnumMember(Value = "bar")] Bar,
    [EnumMember(Value = "bar_open")] BarOpen,
    [EnumMember(Value = "dream")] Dream,
}

/// <summary>씬이 시작되는 방식을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewSceneTrigger
{
    [EnumMember(Value = "auto")] Auto,
    [EnumMember(Value = "interact")] Interact,
    [EnumMember(Value = "cameo")] Cameo,
    [EnumMember(Value = "manual")] Manual,
}

/// <summary>캐릭터의 등장 역할군을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewCharacterRole
{
    [EnumMember(Value = "player")] Player,
    [EnumMember(Value = "master")] Master,
    [EnumMember(Value = "guest_multi")] GuestMulti,
    [EnumMember(Value = "guest_twice")] GuestTwice,
    [EnumMember(Value = "guest_once")] GuestOnce,
    [EnumMember(Value = "cutscene")] Cutscene,
    [EnumMember(Value = "npc_street")] NpcStreet,
}

/// <summary>칵테일 제조 기법을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewMixMethod
{
    [EnumMember(Value = "build")] Build,
    [EnumMember(Value = "shake")] Shake,
    [EnumMember(Value = "stir")] Stir,
    [EnumMember(Value = "none")] None,
}

/// <summary>레시피 한 단계의 동작(붓기/짜기/가루)을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewRecipeAction
{
    [EnumMember(Value = "pour")] Pour,
    [EnumMember(Value = "squeeze")] Squeeze,
    [EnumMember(Value = "powder")] Powder,
}

/// <summary>레시피 계량 단위를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewUnit
{
    [EnumMember(Value = "oz")] Oz,
    [EnumMember(Value = "tsp")] Tsp,
    [EnumMember(Value = "ml")] Ml,
}

/// <summary>선반 아이템의 종류(재료/잔/도구/가니시)를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewShelfKind
{
    [EnumMember(Value = "ingredient")] Ingredient,
    [EnumMember(Value = "glass")] Glass,
    [EnumMember(Value = "tool")] Tool,
    [EnumMember(Value = "garnish")] Garnish,
}

/// <summary>
/// 재료(kind=ingredient)의 세부 카테고리를 나타내는 열거형.
/// 잔/도구/가니시는 category가 null이므로 사용하는 쪽에서 nullable로 받아야 한다.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewIngredientCategory
{
    [EnumMember(Value = "base")] Base,
    [EnumMember(Value = "wine_beer")] WineBeer,
    [EnumMember(Value = "mixer")] Mixer,
    [EnumMember(Value = "fruit")] Fruit,
    [EnumMember(Value = "powder")] Powder,
    [EnumMember(Value = "juice")] Juice,
    [EnumMember(Value = "liqueur")] Liqueur,
    [EnumMember(Value = "syrup")] Syrup,
    [EnumMember(Value = "dairy")] Dairy,
}

/// <summary>거리 인터랙트 포인트의 종류를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewInteractKind
{
    [EnumMember(Value = "object")] Object,
    [EnumMember(Value = "npc")] Npc,
    [EnumMember(Value = "shop")] Shop,
}

/// <summary>인터랙트 포인트가 활성화되는 출퇴근 구간을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewInteractPhase
{
    [EnumMember(Value = "commute_in")] CommuteIn,
    [EnumMember(Value = "commute_out")] CommuteOut,
    [EnumMember(Value = "both")] Both,
}

/// <summary>인터랙트 포인트의 컨텐츠 소비 방식을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewSelectionMode
{
    [EnumMember(Value = "once")] Once,
    [EnumMember(Value = "repeat")] Repeat,
    [EnumMember(Value = "conditional")] Conditional,
    [EnumMember(Value = "sequential")] Sequential,
}

/// <summary>주문 규칙 평가 결과를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewOrderVerdict
{
    [EnumMember(Value = "fulfill")] Fulfill,
    [EnumMember(Value = "care")] Care,
    [EnumMember(Value = "partial")] Partial,
    [EnumMember(Value = "miss")] Miss,
}

/// <summary>서빙 결과 등급을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewGrade
{
    [EnumMember(Value = "excellent")] Excellent,
    [EnumMember(Value = "good")] Good,
    [EnumMember(Value = "decent")] Decent,
    [EnumMember(Value = "poor")] Poor,
    [EnumMember(Value = "sewage")] Sewage,
}

/// <summary>손님의 칵테일 취향 단계를 나타내는 열거형. tastes.json과 balance.json의 affinity_matrix가 공유한다.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewTasteTier
{
    [EnumMember(Value = "love")] Love,
    [EnumMember(Value = "good")] Good,
    [EnumMember(Value = "ok")] Ok,
    [EnumMember(Value = "dislike")] Dislike,
    [EnumMember(Value = "miss")] Miss,
}

/// <summary>표정 표현 방식(파츠 합성 애니메이션 / 단일 스프라이트)을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewExpressionMode
{
    [EnumMember(Value = "parts_anim")] PartsAnim,
    [EnumMember(Value = "sprite")] Sprite,
}

/// <summary>
/// 표정 파츠 애니메이션의 반복 모드를 나타내는 열거형.
/// 기존 EAnimLoopMode와 의미는 같지만 json/expressions.json의 표기(소문자 special_on_dialogue)에 맞춰 새로 정의했다.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewAnimLoopMode
{
    [EnumMember(Value = "always")] Always,
    [EnumMember(Value = "always_on_dialogue")] AlwaysOnDialogue,
    [EnumMember(Value = "special_on_dialogue")] SpecialOnDialogue,
}

/// <summary>인물 정보(dossier) 항목의 분류를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewDossierKind
{
    [EnumMember(Value = "desc")] Desc,
    [EnumMember(Value = "taste")] Taste,
    [EnumMember(Value = "history")] History,
    [EnumMember(Value = "secret")] Secret,
    [EnumMember(Value = "recent")] Recent,
}

/// <summary>컷신 리소스의 재생 방식을 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ENewCutSceneKind
{
    [EnumMember(Value = "timeline")] Timeline,
    [EnumMember(Value = "sprite")] Sprite,
    [EnumMember(Value = "gif")] Gif,
}
