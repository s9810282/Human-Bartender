using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewDayInfoData
{
    [field: SerializeField][JsonProperty("day")] public int Day { get; set; }
    [field: SerializeField][JsonProperty("label_ko")] public string LabelKo { get; set; }
    [field: SerializeField][JsonProperty("label_en")] public string LabelEn { get; set; }
    [field: SerializeField][JsonProperty("start_phase")] public string StartPhase { get; set; }
    [field: SerializeField][JsonProperty("bgm_street")] public string BgmStreet { get; set; }
    [field: SerializeField][JsonProperty("bgm_bar")] public string BgmBar { get; set; }
    [field: SerializeField][JsonProperty("note")] public string Note { get; set; }
    [field: SerializeField][JsonProperty("label")] public LocalizedText Label { get; set; }
}

/// <summary>StreamingAssets/json/days.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewDayInfoDataSO", menuName = "Data/New/DayInfoDataSO")]
public class NewDayInfoDataSO : ScriptableObject
{
    public NewDayInfoData[] dayInfoData;
}
