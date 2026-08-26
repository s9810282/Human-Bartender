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

/// <summary>
/// 길거리 스크립트의 선택지 하나.
///
/// 다른 파일의 choices(NewChoiceOptionData)와 모양이 다르다 — 여기서는 선택지가 스텝 안에 직접 들어가고,
/// 고른 결과도 goto로 다른 씬을 가리키는 대신 result_steps에 이어질 스텝을 그대로 품는다.
/// 같은 타입으로 묶으면 한쪽에만 있는 필드가 계속 늘어난다.
/// </summary>
[Serializable]
public struct NewStreetOptionData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [JsonProperty("text")] public LocalizedText? Text { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }

    /// <summary>고를 수 없을 때 보여줄 이유. 화면에 그대로 나오는 문구라 언어별로 들어 있다. 고를 수 있으면 null이다.</summary>
    [JsonProperty("lock_reason")] public LocalizedText? LockReason { get; set; }

    /// <summary>이 선택지를 고른 뒤 이어서 실행할 스텝.</summary>
    [field: SerializeField][JsonProperty("result_steps")] public Step[] ResultSteps { get; set; }
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

    /// <summary>이 대사가 참조하는 대사 id. 없으면 null이다.</summary>
    [field: SerializeField][JsonProperty("dialogue_id")] public string DialogueId { get; set; }

    /// <summary>type이 goto일 때 옮겨 갈 씬 id.</summary>
    [field: SerializeField][JsonProperty("scene_id")] public string SceneId { get; set; }

    /// <summary>
    /// 이 스텝에 딸린 선택지. 다른 스크립트 파일의 choices와 달리 스텝 안에 직접 들어 있고,
    /// 고른 뒤 이어갈 내용도 goto가 아니라 result_steps로 품고 있다.
    /// </summary>
    [field: SerializeField][JsonProperty("options")] public NewStreetOptionData[] Options { get; set; }
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