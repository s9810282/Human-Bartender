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

/// <summary>대화 텍스트 색상 태그 데이터를 보유하는 ScriptableObject. 태그명 → 색상/설명 딕셔너리로 구성된다.</summary>
[CreateAssetMenu(fileName = "New TextTagDataSO", menuName = "Data/TextTagDataSO")]
public class TextTagDataSO : ScriptableObject
{
    public TextTagDataBase textTagData;
}