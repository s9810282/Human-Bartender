using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

// ── ScriptableObject ──────────────────────────────────────────────────────
[CreateAssetMenu(fileName = "New CharacterAnimConfigBase", menuName = "Data/CharacterAnimConfigBase")]
public class CharacterAnimSO : ScriptableObject
{
    public CharacterAnimBase animConfig;

    /// <summary>
    /// 캐릭터 + expression + 파트로 PartAnimData 조회.
    /// expression 없으면 default 폴백, default도 없으면 null 반환.
    /// </summary>
    public PartAnimData GetPartData(string characterId, string expression, string partName)
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

        exprData.Parts.TryGetValue(partName, out var partData);
        return partData;
    }

    public PartAnimData GetDefaultPartData(string characterId, string partName)
        => GetPartData(characterId, "default", partName);

    public string GetBaseBody(string id)
    {


        return "";
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
    [JsonProperty("eyes")] public PartAnimData Eyes { get; set; }
    [JsonProperty("eyebrows")] public PartAnimData Eyebrows { get; set; }
    [JsonProperty("upper_face")] public PartAnimData Upper_face { get; set; }
    [JsonProperty("lower_face")] public PartAnimData Lower_face { get; set; }
    [JsonProperty("body")] public PartAnimData Body { get; set; }
    [JsonProperty("extra")] public PartAnimData Extra { get; set; }

    public bool TryGetPart(string partName, out PartAnimData data)
    {
        data = partName switch
        {
            "eyes" => Eyes,
            "eyebrows" => Eyebrows,
            "upper_face" => Upper_face,
            "lower_face" => Lower_face,
            "body" => Body,
            "extra" => Extra,
            _ => null
        };
        return data != null;
    }

    // SO.GetPartData에서 Dictionary처럼 접근하기 위한 래퍼
    public Dictionary<string, PartAnimData> Parts => new()
    {
        { "eyes",  Eyes  },
        { "eyebrows",  Eyebrows  },
        { "upper_face",  Upper_face  },
        { "lower_face",  Lower_face  },
        { "body", Body },
        { "extra",  Extra  }
    };
}

[Serializable]
public class PartAnimData
{
    [JsonProperty("clip")] public string Clip { get; set; }

    /// <summary>"always" / "on_dialogue" / "once" / "none"</summary>
    [JsonProperty("loop")] public string Loop { get; set; }
}