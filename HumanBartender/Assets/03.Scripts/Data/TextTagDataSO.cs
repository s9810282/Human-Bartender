using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct TextTagData
{
    [JsonProperty("color")] public string Color { get; set; }
    [JsonProperty("description")] public string Description { get; set; }
}

[Serializable]
public class TextTagDataBase
{
    [JsonProperty("text_tags")] public Dictionary<string, TextTagData> TextTags { get; set; }
}

[CreateAssetMenu(fileName = "New TextTagDataSO", menuName = "Data/TextTagDataSO")]
public class TextTagDataSO : ScriptableObject
{
    public TextTagDataBase textTagData;
}