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
    /// 에디터에서 데이터 변경 시 딕셔너리 캐시 재구성
    /// </summary>
    private void OnValidate()
    {
        InitializeDictionary();
    }
}