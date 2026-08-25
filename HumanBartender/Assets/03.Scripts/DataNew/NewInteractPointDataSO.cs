using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

[Serializable]
public struct NewInteractPointData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("kind")] public ENewInteractKind Kind { get; set; }
    [field: SerializeField][JsonProperty("source_id")] public string SourceId { get; set; }
    [field: SerializeField][JsonProperty("spot_id")] public string SpotId { get; set; }
    [field: SerializeField][JsonProperty("facing")] public EFacing? Facing { get; set; }
    [field: SerializeField][JsonProperty("phase")] public ENewInteractPhase Phase { get; set; }
    [field: SerializeField][JsonProperty("spawn_when")] public string SpawnWhen { get; set; }
    [field: SerializeField][JsonProperty("activation_mode")] public EActivationMode ActivationMode { get; set; }
    [field: SerializeField][JsonProperty("interact_when")] public string InteractWhen { get; set; }
    [field: SerializeField][JsonProperty("priority")] public int Priority { get; set; }
    [field: SerializeField][JsonProperty("action_type")] public string EActionType { get; set; }
    [field: SerializeField][JsonProperty("action_ref")] public string ActionRef { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

/// <summary>StreamingAssets/json/interact_points.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewInteractPointDataSO", menuName = "Data/New/InteractPointDataSO")]
public class NewInteractPointDataSO : ScriptableObject
{
    public NewInteractPointData[] interactPointData;

    // Fast-lookup Dictionary (Inspector에 직렬화되지 않음)
    private Dictionary<string, NewInteractPointData> _dict;

    /// <summary>
    /// 딕셔너리 프로퍼티 (최초 접근 시 캐싱)
    /// </summary>
    public Dictionary<string, NewInteractPointData> Dict
    {
        get
        {
            if (_dict == null)
            {
                InitializeDictionary();
            }
            return _dict;
        }
    }

    /// <summary>
    /// ScriptableObject 로드 시 또는 배열 수정 후 캐시 갱신
    /// </summary>
    public void InitializeDictionary()
    {
        _dict = new Dictionary<string, NewInteractPointData>();

        if (interactPointData == null) return;

        foreach (var data in interactPointData)
        {
            if (string.IsNullOrEmpty(data.Id)) continue;

            if (!_dict.ContainsKey(data.Id))
            {
                _dict.Add(data.Id, data);
            }
            else
            {
                Debug.LogWarning($"[NewInteractPointDataSO] 중복된 ID가 존재합니다: {data.Id}");
            }
        }
    }

    /// <summary>
    /// ID로 데이터를 가져오는 안전한 접근 메서드
    /// </summary>
    public bool TryGetData(string id, out NewInteractPointData data)
    {
        return Dict.TryGetValue(id, out data);
    }

    /// <summary>
    /// 에디터에서 데이터 변경 시 딕셔너리를 재구성합니다.
    /// </summary>
    private void OnValidate()
    {
        InitializeDictionary();
    }
}