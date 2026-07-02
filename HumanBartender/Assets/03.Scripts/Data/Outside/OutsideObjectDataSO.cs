using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public struct OutsideObjectData
{
    [JsonProperty("object_id")] public string ObjectId { get; set; }
    [JsonProperty("day")] public int? Day { get; set; }
    [JsonProperty("selection")] public SelectionConfigData Selection { get; set; }
    [JsonProperty("interact_label")] public string InteractLabel { get; set; }
    [JsonProperty("flows")] public FlowData[] FlowData { get; set; }

}

[Serializable]
public class OutsideObjectDataBase
{
    [JsonProperty("objects")] public OutsideObjectData[] outsideObjectDatas;
}


/// <summary>
/// Outside 씬 상호작용 오브젝트 데이터를 보유하는 ScriptableObject.
/// Cached() 호출 시 objectId 기반 딕셔너리를 생성한다.
/// </summary>
[CreateAssetMenu(fileName = "New OutsideObjectDataSO", menuName = "Data/OutsideObjectDataSO")]
public class OutsideObjectDataSO : ScriptableObject
{
    public OutsideObjectDataBase outsideObjectData;
    public Dictionary<string, OutsideObjectData> outsideObjects = new();

    public void Cached()
    {
        outsideObjects = outsideObjectData?.outsideObjectDatas.ToDictionary(c => c.ObjectId);
    }
}
