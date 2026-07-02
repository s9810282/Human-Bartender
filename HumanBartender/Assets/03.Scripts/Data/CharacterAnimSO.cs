using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;


/// <summary>캐릭터 표정 애니메이션의 파트(부위)를 구분하는 열거형.</summary>
public enum EAnimationPart
{
    Eyes = 1,
    Eyeblows = 2,
    Body = 3,
    Upper_Face = 4,
    Lower_Face = 5,
    Extra = 6,
    Etc = 7,
    Sprite = 10,
}

/// <summary>애니메이션 파트의 재생 반복 모드를 나타내는 열거형.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum EAnimLoopMode
{
    [EnumMember(Value = "always")]
    Always,

    [EnumMember(Value = "always_on_dialogue")]
    Always_OnDialogue,

    [EnumMember(Value = "Special_on_dialogue")]
    Special_OnDialogue,

    [EnumMember(Value = "once")]
    Once,
    [EnumMember(Value = "none")]
    None
}




/// <summary>
/// 캐릭터 표정 애니메이션 설정 데이터를 보유하는 ScriptableObject.
/// characterId + expression + EAnimationPart 조합으로 PartAnimData를 조회한다.
/// expression이 없으면 "default"로 폴백한다.
/// </summary>
[CreateAssetMenu(fileName = "New CharacterAnimConfigBase", menuName = "Data/CharacterAnimConfigBase")]
public class CharacterAnimSO : ScriptableObject
{
    public CharacterAnimBase animConfig;

    /// <summary>
    /// 캐릭터 + expression + 파트로 PartAnimData 조회.
    /// expression 없으면 default 폴백, default도 없으면 null 반환.
    /// </summary>
    public PartAnimData GetPartData(string characterId, string expression, EAnimationPart partName)
    {
        if (animConfig?.Characters == null) return null;

        if (!animConfig.Characters.TryGetValue(characterId, out var charData))
        {
            Logger.LogWarning($"[AnimConfig] 캐릭터 없음: {characterId}");
            return null;
        }

        if (!charData.Expressions.TryGetValue(expression, out var exprData))
        {
            Logger.LogWarning($"[AnimConfig] '{characterId}'에 '{expression}' 없음 → default 사용");
            if (!charData.Expressions.TryGetValue("default", out exprData))
                return null;
        }

        exprData.TryGetPart(partName, out var partData);
        return partData;
    }

    public PartAnimData GetDefaultPartData(string characterId, EAnimationPart partName)
        => GetPartData(characterId, "default", partName);

    public bool CheckExpressionPortailSprite(string characterId, string expression)
    {
        if (!animConfig.Characters.TryGetValue(characterId, out var charData))
        {
            Logger.LogWarning($"[AnimConfig] 캐릭터 없음: {characterId}");
            return false;
        }


        if (!charData.Expressions.TryGetValue(expression, out var exprData))
        {
            Logger.LogWarning($"[AnimConfig] '{characterId}'에 '{expression}' 없음 → default 사용");
            if (!charData.Expressions.TryGetValue("default", out exprData))
                return false;
        }

        return exprData.Type != null && exprData.Type == "sprite";
    }

    public string GetSpritePath(string characterId, string expression)
    {
        if (!animConfig.Characters.TryGetValue(characterId, out var charData))
        {
            Logger.LogWarning($"[AnimConfig] 캐릭터 없음: {characterId}");
            return null;
        }


        if (!charData.Expressions.TryGetValue(expression, out var exprData))
        {
            Logger.LogWarning($"[AnimConfig] '{characterId}'에 '{expression}' 없음 → default 사용");
            if (!charData.Expressions.TryGetValue("default", out exprData))
                return null;
        }

        return exprData.SpritePath;
    }
    
}

[Serializable]
public class CharacterAnimBase
{
    [JsonProperty("characters")] public Dictionary<string, CharacterAnimData> Characters { get; set; }
}


[Serializable]
public class CharacterAnimData
{
    [JsonProperty("base_body")] public string BaseBody { get; set; }
    [JsonProperty("expressions")] public Dictionary<string, ExpressionAnimData> Expressions { get; set; }
}


[Serializable]
public class ExpressionAnimData
{
    [JsonProperty("type")] public string Type { get; set; }
    [JsonProperty("sprite")] public string SpritePath { get; set; }

    [JsonProperty("eyes")] public PartAnimData Eyes { get; set; }
    [JsonProperty("eyebrows")] public PartAnimData Eyebrows { get; set; }
    [JsonProperty("upper_face")] public PartAnimData Upper_face { get; set; }
    [JsonProperty("lower_face")] public PartAnimData Lower_face { get; set; }
    [JsonProperty("body")] public PartAnimData Body { get; set; }
    [JsonProperty("extra")] public PartAnimData Extra { get; set; }
    [JsonProperty("etc")] public PartAnimData Etc { get; set; }

    public bool TryGetPart(EAnimationPart partName, out PartAnimData data)
    {
        data = partName switch
        {
            EAnimationPart.Eyes => Eyes,
            EAnimationPart.Eyeblows => Eyebrows,
            EAnimationPart.Upper_Face => Upper_face,
            EAnimationPart.Lower_Face => Lower_face,
            EAnimationPart.Body => Body,
            EAnimationPart.Extra => Extra,
            EAnimationPart.Etc => Etc,
            _ => null
        };
        return data != null;
    }

    //// SO.GetPartData에서 Dictionary처럼 접근하기 위한 래퍼
    //public Dictionary<string, PartAnimData> Parts => new()
    //{
    //    { "eyes",  Eyes  },
    //    { "eyebrows",  Eyebrows  },
    //    { "upper_face",  Upper_face  },
    //    { "lower_face",  Lower_face  },
    //    { "body", Body },
    //    { "extra",  Extra  }
    //};
}

[Serializable]
public class PartAnimData
{
    [JsonProperty("clip")] public string Clip { get; set; }

    /// <summary>"always" / "on_dialogue" / "once" / "none"</summary>
    [JsonProperty("loop")] public EAnimLoopMode Loop { get; set; }
}

