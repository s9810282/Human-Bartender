using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewGuestBodyPartData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("gender")] public string Gender { get; set; }
    [field: SerializeField][JsonProperty("personalities")] public string[] Personalities { get; set; }
    [field: SerializeField][JsonProperty("mode")] public string Mode { get; set; }
    [field: SerializeField][JsonProperty("sprite")] public string Sprite { get; set; }
    [JsonProperty("parts")] public Dictionary<string, string> Parts { get; set; }
    [field: SerializeField][JsonProperty("weight")] public int Weight { get; set; }
}

[Serializable]
public class NewGuestBodyDataBase
{
    [field: SerializeField][JsonProperty("bodies")] public NewGuestBodyPartData[] Bodies { get; set; }
    [field: SerializeField][JsonProperty("outfits")] public NewGuestBodyPartData[] Outfits { get; set; }
    [field: SerializeField][JsonProperty("eyes")] public NewGuestBodyPartData[] Eyes { get; set; }
    [field: SerializeField][JsonProperty("hairs")] public NewGuestBodyPartData[] Hairs { get; set; }
}

/// <summary>StreamingAssets/json/guest_bodies.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewGuestBodyDataSO", menuName = "Data/New/GuestBodyDataSO")]
public class NewGuestBodyDataSO : ScriptableObject
{
    public NewGuestBodyDataBase guestBodyData;
}
