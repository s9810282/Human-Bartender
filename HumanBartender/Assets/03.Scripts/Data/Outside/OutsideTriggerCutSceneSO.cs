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



/// <summary>
/// Outside 씬 컷씬 트리거 이벤트 데이터를 보유하는 ScriptableObject.
/// Cached() 호출 시 id 기반 딕셔너리를 생성한다.
/// </summary>
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
