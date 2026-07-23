using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewSpotData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("area")] public string Area { get; set; }
    [field: SerializeField][JsonProperty("desc_ko")] public string DescKo { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
}

/// <summary>StreamingAssets/json/spots.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewSpotDataSO", menuName = "Data/New/SpotDataSO")]
public class NewSpotDataSO : ScriptableObject
{
    public NewSpotData[] spotData;
}
