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
}

[Serializable]
public struct NewSceneData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("day")] public int Day { get; set; }
    [field: SerializeField][JsonProperty("phase")] public ENewScenePhase Phase { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("trigger")] public ENewSceneTrigger Trigger { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("title")] public string Title { get; set; }
    [field: SerializeField][JsonProperty("skippable")] public bool Skippable { get; set; }
    [field: SerializeField][JsonProperty("group")] public ENewStreetGroup Group { get; set; }
    [field: SerializeField][JsonProperty("steps")] public Step[] Steps { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

[Serializable]
public struct ChoiceOption
{
    [field: SerializeField][JsonProperty("idx")] public int Idx { get; set; }
    [field: SerializeField][JsonProperty("text")] public Texts? Text { get; set; }
    [field: SerializeField][JsonProperty("goto")] public string Goto { get; set; }
}

[Serializable]
public struct ChoiceDatas
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("options")] public ChoiceOption[] Options { get; set; }
}
public struct NewStreetData
{

    [field: SerializeField][JsonProperty("place")] public string Place { get; set; }
    [field: SerializeField][JsonProperty("scenes")] public NewSceneData[] Scenes { get; set; }

    [field: SerializeField][JsonProperty("choices")]public ChoiceDatas[] Choices { get; set; }
}
/// <summary>Street.json 단일 객체 구조와 1:1 대응되는 ScriptableObject</summary>
[CreateAssetMenu(fileName = "NewStreetDataSO", menuName = "Data/New/StreetDataSO")]
[Serializable]
public class NewStreetDataSO : ScriptableObject
{
}