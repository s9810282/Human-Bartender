using System;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>캐릭터 한 명의 기본 정보(id, 표시 이름, 이름 색상, 표정 목록, 플레이어 여부)를 담는 구조체.</summary>
[Serializable]
public struct CharacterData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("display_name")] public string DisplayName { get; set; }
    [JsonProperty("name_color")] public string NameColor { get; set; }
    [JsonProperty("expressions")] public string[] Expressions { get; set; }
    [JsonProperty("is_player")] public bool IsPlayer { get; set; }
}

/// <summary>CharacterDataBase를 보유하는 ScriptableObject 래퍼.</summary>
[CreateAssetMenu(fileName = "New CharacterDataBase", menuName = "Data/CharacterDataBase")]
public class CharacterDataSO : ScriptableObject
{
    public CharacterDataBase characterData;
}

[Serializable]
public class CharacterDataBase
{
    [JsonProperty("characters")] public CharacterData[] Characters { get; set; }
}