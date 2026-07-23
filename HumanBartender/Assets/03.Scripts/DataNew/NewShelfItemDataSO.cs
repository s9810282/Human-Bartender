using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewShelfItemData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("kind")] public ENewShelfKind Kind { get; set; }
    [field: SerializeField][JsonProperty("name")] public LocalizedText Name { get; set; }
    [JsonProperty("category")] public ENewIngredientCategory? Category { get; set; }
    [field: SerializeField][JsonProperty("color")] public string Color { get; set; }
    [field: SerializeField][JsonProperty("sprite")] public string Sprite { get; set; }
    [field: SerializeField][JsonProperty("unlock_day")] public int UnlockDay { get; set; }
    [field: SerializeField][JsonProperty("unlock_when")] public string UnlockWhen { get; set; }
    [JsonProperty("shop_price")] public int? ShopPrice { get; set; }
    [field: SerializeField][JsonProperty("desc")] public LocalizedText Desc { get; set; }
}

/// <summary>StreamingAssets/json/shelf_items.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewShelfItemDataSO", menuName = "Data/New/ShelfItemDataSO")]
public class NewShelfItemDataSO : ScriptableObject
{
    public NewShelfItemData[] shelfItemData;
}
