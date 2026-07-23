using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct LocalizedText
{
    [field: SerializeField][JsonProperty("ko")] public string Ko { get; set; }
    [field: SerializeField][JsonProperty("en")] public string En { get; set; }
}
