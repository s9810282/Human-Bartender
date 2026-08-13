using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct LocalizedText
{
    [field: SerializeField][JsonProperty("ko")] public string Ko { get; set; }
    [field: SerializeField][JsonProperty("en")] public string En { get; set; }
}

/// <summary>
/// 선택지 하나. script/day_N.json, script/common.json, script/street.json의 choices가 모두 같은 모양이라
/// 여기 한 곳에 둔다 — 파일마다 따로 두면 한쪽만 고쳐지고 나머지가 뒤처지는 일이 반복된다.
///
/// choices는 { "선택지_id": [ option, ... ] } 꼴의 딕셔너리이고, 스텝의 arg가 그 id를 가리킨다.
/// </summary>
[Serializable]
public struct NewChoiceOptionData
{
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [JsonProperty("text")] public LocalizedText? Text { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("effects")] public string Effects { get; set; }
    /// <summary>고른 뒤 이어갈 씬 id. null이면 그대로 다음 스텝으로 넘어간다.</summary>
    [field: SerializeField][JsonProperty("goto")] public string Goto { get; set; }
}
