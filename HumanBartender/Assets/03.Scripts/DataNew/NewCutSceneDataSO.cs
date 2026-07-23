using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewCutSceneRefData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("kind")] public ENewCutSceneKind Kind { get; set; }
    [field: SerializeField][JsonProperty("resource_key")] public string ResourceKey { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

/// <summary>StreamingAssets/json/cutscenes.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewCutSceneDataSO", menuName = "Data/New/CutSceneDataSO")]
public class NewCutSceneDataSO : ScriptableObject
{
    public NewCutSceneRefData[] cutSceneData;
}
