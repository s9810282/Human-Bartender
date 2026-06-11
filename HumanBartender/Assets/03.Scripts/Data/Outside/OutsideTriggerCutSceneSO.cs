using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public struct CutSceneEventData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("cutscene_id")] public string CutSceneId { get; set; }
    [JsonProperty("day")] public int Day { get; set; }
    [JsonProperty("timing")] public EGameFlow timing { get; set; }
    [JsonProperty("condition")] public OutsideCondition? Conditions { get; set; }
}

[Serializable]
public class CutSceneEventBase
{
    [JsonProperty("cutscene_events")] public CutSceneEventData[] cutScenes;
}



[CreateAssetMenu(fileName = "OutsideTriggerCutSceneSO", menuName = "Scriptable Objects/OutsideTriggerCutSceneSO")]
public class OutsideTriggerCutSceneSO : ScriptableObject
{
    public CutSceneEventBase cutSceneEvent;
    public Dictionary<string, CutSceneEventData> cutSceneEventDic = new();

    public void Cached()
    {
        cutSceneEventDic = cutSceneEvent?.cutScenes.ToDictionary(c => c.Id);
    }
}
