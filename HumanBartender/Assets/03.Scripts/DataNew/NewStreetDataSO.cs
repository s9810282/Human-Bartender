using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct Texts
{
    [field: SerializeField][JsonProperty("ko")] public string Ko { get; set; }
    [field: SerializeField][JsonProperty("en")] public string En { get; set; }
}

[Serializable]
public struct Step
{
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("type")] public ENewStepType Type { get; set; }
    [field: SerializeField][JsonProperty("actor")] public string Actor { get; set; }
    [field: SerializeField][JsonProperty("arg")] public string Arg { get; set; }
    [field: SerializeField][JsonProperty("text")] public Texts? Text { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("effects")] public string Effects { get; set; }
    /// <summary>"wait"면 이 스텝이 끝날 때까지 다음 스텝을 진행하지 않는다.</summary>
    [field: SerializeField][JsonProperty("sync")] public string Sync { get; set; }
}

[Serializable]
public struct NewSceneData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }

    /// <summary>
    /// 이 씬이 열리는 일차. 특정 일차에 묶이지 않는 씬은 null이다 — 그런 씬은 날짜가 아니라
    /// when 조건(플래그 등)으로만 열린다. 거리 씬 절반이 여기 해당한다.
    /// </summary>
    [JsonProperty("day")] public int? Day { get; set; }

    [field: SerializeField][JsonProperty("phase")] public ENewScenePhase Phase { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("trigger")] public ENewSceneTrigger Trigger { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("title")] public string Title { get; set; }
    [field: SerializeField][JsonProperty("skippable")] public bool Skippable { get; set; }
    /// <summary>
    /// 이어지는 씬 묶음. 어느 묶음에도 속하지 않는 단독 씬은 null이다. 12개 중 9개가 여기 해당한다.
    /// </summary>
    [JsonProperty("group")] public ENewStreetGroup? Group { get; set; }
    [field: SerializeField][JsonProperty("steps")] public Step[] Steps { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

// ChoiceOption / ChoiceDatas는 걷어냈다. street.json의 선택지는 day_N.json·common.json과 완전히 같은
// 모양(seq/text/when/effects/goto)인데 여기 사본만 idx를 들고 있어서 실제 데이터와 맞지 않았고,
// when·effects는 아예 읽지 못했다. 공용 NewChoiceOptionData(NewCommonTypes.cs)로 통일한다.

[Serializable]
public struct NewStreetData
{

    [field: SerializeField][JsonProperty("place")] public string Place { get; set; }
    [field: SerializeField][JsonProperty("scenes")] public NewSceneData[] Scenes { get; set; }
    [JsonProperty("choices")] public Dictionary<string, NewChoiceOptionData[]> Choices { get; set; }
}
/// <summary>Street.json 단일 객체 구조와 1:1 대응되는 ScriptableObject</summary>
[CreateAssetMenu(fileName = "NewStreetDataSO", menuName = "Data/New/StreetDataSO")]
[Serializable]
public class NewStreetDataSO : ScriptableObject
{
    public NewStreetData newStreetData;
}