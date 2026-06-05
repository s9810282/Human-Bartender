using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;


[Serializable]
public struct OutsideObjectData
{
    [JsonProperty("object_id")] public string ObjectId { get; set; }
    [JsonProperty("day")] public int? Day { get; set; }
    [JsonProperty("interact_label")] public string InteractLabel { get; set; }
    [JsonProperty("dialogues")] public DialogueData[] Dialogues { get; set; }
}

[Serializable]
public class OutsideObjectDataBase
{
    [JsonProperty("objects")] public OutsideObjectData[] outsideObjectDatas;
}


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
