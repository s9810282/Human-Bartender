using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct CharacterData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("display_name")] public string DisplayName { get; set; }
    [JsonProperty("name_color")] public string NameColor { get; set; }
    [JsonProperty("expressions")] public string[] Expressions { get; set; }
    [JsonProperty("is_player")] public bool IsPlayer { get; set; }
}

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