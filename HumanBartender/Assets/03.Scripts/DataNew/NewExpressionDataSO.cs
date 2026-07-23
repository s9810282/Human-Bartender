using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewExpressionPartClip
{
    [field: SerializeField][JsonProperty("clip")] public string Clip { get; set; }
    [field: SerializeField][JsonProperty("loop")] public ENewAnimLoopMode Loop { get; set; }
}

[Serializable]
public struct NewExpressionEntry
{
    [field: SerializeField][JsonProperty("mode")] public ENewExpressionMode Mode { get; set; }
    [field: SerializeField][JsonProperty("sprite")] public string Sprite { get; set; }
    [field: SerializeField][JsonProperty("talk_anim")] public bool TalkAnim { get; set; }
    [JsonProperty("parts")] public Dictionary<string, NewExpressionPartClip> Parts { get; set; }
}

/// <summary>
/// StreamingAssets/json/expressions.json을 보유하는 ScriptableObject.
/// 루트가 캐릭터 id -> 표정 이름 -> 표정 데이터의 2단 딕셔너리 구조다.
/// </summary>
[CreateAssetMenu(fileName = "NewExpressionDataSO", menuName = "Data/New/ExpressionDataSO")]
public class NewExpressionDataSO : ScriptableObject
{
    public Dictionary<string, Dictionary<string, NewExpressionEntry>> expressionData;
}
