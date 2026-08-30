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
public struct ResultStep
{
    [field: SerializeField][JsonProperty("type")] public string Type { get; set; }
    [field: SerializeField][JsonProperty("effects")] public string Effects { get; set; }
    [field: SerializeField][JsonProperty("scene_id")] public string SceneId { get; set; }
}

/// <summary>
/// 선택지 세부 항목(options) 데이터 구조체
/// </summary>
[Serializable]
public struct ChoiceOption
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("text")] public Texts? Text { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("lock_reason")] public Texts? LockReason { get; set; }
    [field: SerializeField][JsonProperty("result_steps")] public ResultStep[] ResultSteps { get; set; }
}
}

[Serializable]
public struct Step
{
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("type")] public string Type { get; set; }
    [field: SerializeField][JsonProperty("actor")] public string Actor { get; set; }
    [field: SerializeField][JsonProperty("dialogue_id")] public string DialogueId { get; set; }
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
    [JsonProperty("day")] public int? Day { get; set; }

    [field: SerializeField][JsonProperty("phase")] public ENewScenePhase Phase { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("start_mode")] public ENewSceneTrigger StartMode { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("title")] public string Title { get; set; }
    [field: SerializeField][JsonProperty("skippable")] public bool Skippable { get; set; }
    [JsonProperty("group")] public ENewStreetGroup? Group { get; set; }
    [field: SerializeField][JsonProperty("steps")] public Step[] Steps { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

[Serializable]
public struct NewStreetData
{
    [field: SerializeField][JsonProperty("place")] public string Place { get; set; }
    [field: SerializeField][JsonProperty("scenes")] public NewSceneData[] Scenes { get; set; }
}

/// <summary>Street.json 단일 객체 구조와 1:1 대응되는 ScriptableObject</summary>
[CreateAssetMenu(fileName = "NewStreetDataSO", menuName = "Data/New/StreetDataSO")]
public class NewStreetDataSO : ScriptableObject
{
    public NewStreetData newStreetData;

    // Fast-lookup Dictionary (Inspector 미노출)
    private Dictionary<string, NewSceneData> _sceneDict;

    /// <summary>
    /// Scene ID를 Key로 하는 SceneData 딕셔너리 프로퍼티
    /// </summary>
    public Dictionary<string, NewSceneData> SceneDict
    {
        get
        {
            if (_sceneDict == null)
            {
                InitializeDictionary();
            }
            return _sceneDict;
        }
    }

    /// <summary>
    /// Scenes 배열을 기반으로 Scene ID 딕셔너리를 초기화합니다.
    /// </summary>
    public void InitializeDictionary()
    {
        _sceneDict = new Dictionary<string, NewSceneData>();

        if (newStreetData.Scenes == null) return;

        foreach (var scene in newStreetData.Scenes)
        {
            if (string.IsNullOrEmpty(scene.Id)) continue;

            if (!_sceneDict.ContainsKey(scene.Id))
            {
                _sceneDict.Add(scene.Id, scene);
            }
            else
            {
                Debug.LogWarning($"[NewStreetDataSO] 중복된 Scene ID가 존재합니다: {scene.Id}");
            }
        }
    }

    /// <summary>
    /// Scene ID로 NewSceneData를 안전하게 검색합니다.
    /// </summary>
    public bool TryGetSceneData(string sceneId, out NewSceneData sceneData)
    {
        return SceneDict.TryGetValue(sceneId, out sceneData);
    }

    /// <summary>
    /// [요청 기능] Scene ID를 Key값으로 전달하여 해당 Scene의 Steps 배열을 바로 가져옵니다.
    /// </summary>
    public bool TryGetSteps(string sceneId, out Step[] steps)
    {
        if (TryGetSceneData(sceneId, out NewSceneData sceneData))
        {
            steps = sceneData.Steps;
            return true;
        }

        steps = null;
        return false;
    }

    private void OnValidate()
    {
        InitializeDictionary();
    }
}