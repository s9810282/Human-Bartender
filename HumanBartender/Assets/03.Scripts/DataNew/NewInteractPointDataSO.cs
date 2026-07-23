using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewInteractPointData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("spot")] public string Spot { get; set; }
    [field: SerializeField][JsonProperty("kind")] public ENewInteractKind Kind { get; set; }
    [field: SerializeField][JsonProperty("phase")] public ENewInteractPhase Phase { get; set; }
    [field: SerializeField][JsonProperty("when")] public string When { get; set; }
    [field: SerializeField][JsonProperty("scene_or_shop")] public string SceneOrShop { get; set; }
    [field: SerializeField][JsonProperty("selection")] public ENewSelectionMode Selection { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

/// <summary>StreamingAssets/json/interact_points.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewInteractPointDataSO", menuName = "Data/New/InteractPointDataSO")]
public class NewInteractPointDataSO : ScriptableObject
{
    public NewInteractPointData[] interactPointData;
}
