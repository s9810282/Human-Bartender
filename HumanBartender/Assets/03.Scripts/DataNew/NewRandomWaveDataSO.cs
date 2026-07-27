using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewRandomWaveData
{
    [field: SerializeField][JsonProperty("day")] public int Day { get; set; }
    [field: SerializeField][JsonProperty("seq")] public int Seq { get; set; }
    [field: SerializeField][JsonProperty("tier")] public int Tier { get; set; }
    [field: SerializeField][JsonProperty("personality")] public string Personality { get; set; }
    [field: SerializeField][JsonProperty("delay_sec")] public float DelaySec { get; set; }
    [field: SerializeField][JsonProperty("max_rounds")] public int MaxRounds { get; set; }
    [field: SerializeField][JsonProperty("branch_choice")] public bool BranchChoice { get; set; }
}

/// <summary>StreamingAssets/json/random_waves.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewRandomWaveDataSO", menuName = "Data/New/RandomWaveDataSO")]
public class NewRandomWaveDataSO : ScriptableObject
{
    public NewRandomWaveData[] randomWaveData;
}
