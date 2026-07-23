using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// script/common.json, script/day_1~3.json 등 하나의 씬 스텝을 나타내는 구조체.
/// type(say/move/fx/enter/exit/sfx/choice/order/craft/serve 등)에 따라 arg의 의미가 달라진다.
/// </summary>
[Serializable]
public struct NewDialogueStepData
{
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("type")] public ENewStepType Type { get; set; }
    [field: SerializeField][JsonProperty("actor")] public string Actor { get; set; }
    [field: SerializeField][JsonProperty("arg")] public string Arg { get; set; }
    [JsonProperty("text")] public LocalizedText? Text { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("effects")] public string Effects { get; set; }
    [field: SerializeField][JsonProperty("sync")] public string Sync { get; set; }
}

[Serializable]
public struct NewScriptSceneData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("day")] public int Day { get; set; }
    [field: SerializeField][JsonProperty("phase")] public ENewScenePhase Phase { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("trigger")] public ENewSceneTrigger Trigger { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("title")] public string Title { get; set; }
    [field: SerializeField][JsonProperty("skippable")] public bool Skippable { get; set; }
    [field: SerializeField][JsonProperty("group")] public string Group { get; set; }
    [field: SerializeField][JsonProperty("steps")] public NewDialogueStepData[] Steps { get; set; }
}

[Serializable]
public class NewDayScriptBase
{
    [field: SerializeField][JsonProperty("day")] public int Day { get; set; }
    [field: SerializeField][JsonProperty("scenes")] public NewScriptSceneData[] Scenes { get; set; }
}

/// <summary>script/common.json 또는 script/day_N.json 한 파일을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewDayScriptDataSO", menuName = "Data/New/DayScriptDataSO")]
public class NewDayScriptDataSO : ScriptableObject
{
    public NewDayScriptBase dayScriptData;
}
