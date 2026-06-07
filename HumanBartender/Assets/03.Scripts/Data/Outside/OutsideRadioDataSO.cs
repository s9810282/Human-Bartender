using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public struct RadioDialogue
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("text")] public string Text { get; set; }
    [JsonProperty("delay_after")] public float Delay { get; set; }
}


public struct RadioData
{
    [JsonProperty("day")] public int Days { get; set; }
    [JsonProperty("route")] public EGameFlow Route{ get; set; }
    [JsonProperty("dialogues")] public RadioDialogue[] Dialogues { get; set; }
}


[Serializable]
public class RadioDataBase
{
    [JsonProperty("elevator_radio")] public RadioData[] radioDatas;
}


[CreateAssetMenu(fileName = "OutsideRadioData", menuName = "Scriptable Objects/OutsideRadioData")]
public class OutsideRadioDataSO : ScriptableObject
{
    public RadioDataBase radioData;
}
