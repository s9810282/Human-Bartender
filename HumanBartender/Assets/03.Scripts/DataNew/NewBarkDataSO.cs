using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewBarkData
{
    [field: SerializeField][JsonProperty("voice_id")] public string VoiceId { get; set; }
    [field: SerializeField][JsonProperty("gender")] public string Gender { get; set; }
    [field: SerializeField][JsonProperty("situation")] public string Situation { get; set; }
    [field: SerializeField][JsonProperty("text")] public LocalizedText Text { get; set; }
    [field: SerializeField][JsonProperty("weight")] public int Weight { get; set; }
}

/// <summary>StreamingAssets/json/barks.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewBarkDataSO", menuName = "Data/New/BarkDataSO")]
public class NewBarkDataSO : ScriptableObject
{
    public NewBarkData[] barkData;
}
