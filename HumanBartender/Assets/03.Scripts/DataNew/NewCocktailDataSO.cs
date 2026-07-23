using System;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct NewCocktailRecipeStep
{
    [field: SerializeField][JsonProperty("action")] public ENewRecipeAction Action { get; set; }
    [field: SerializeField][JsonProperty("ingredient")] public string Ingredient { get; set; }
    [field: SerializeField][JsonProperty("qty")] public float Qty { get; set; }
    [field: SerializeField][JsonProperty("unit")] public ENewUnit Unit { get; set; }
}

[Serializable]
public struct NewCocktailData
{
    [field: SerializeField][JsonProperty("id")] public string Id { get; set; }
    [field: SerializeField][JsonProperty("name")] public LocalizedText Name { get; set; }
    [field: SerializeField][JsonProperty("price")] public int Price { get; set; }
    [field: SerializeField][JsonProperty("abv")] public float Abv { get; set; }
    [field: SerializeField][JsonProperty("glass")] public string Glass { get; set; }
    [field: SerializeField][JsonProperty("mix")] public ENewMixMethod Mix { get; set; }
    [field: SerializeField][JsonProperty("prep")] public string Prep { get; set; }
    [field: SerializeField][JsonProperty("fill")] public string Fill { get; set; }
    [field: SerializeField][JsonProperty("garnish")] public string Garnish { get; set; }
    [field: SerializeField][JsonProperty("color")] public string Color { get; set; }
    [field: SerializeField][JsonProperty("tags")] public string[] Tags { get; set; }
    [field: SerializeField][JsonProperty("flavor")] public LocalizedText Flavor { get; set; }
    [field: SerializeField][JsonProperty("unlock_when")] public string UnlockWhen { get; set; }
    [field: SerializeField][JsonProperty("recipe")] public NewCocktailRecipeStep[] Recipe { get; set; }
    [field: SerializeField][JsonProperty("gimmick_count")] public int GimmickCount { get; set; }
    [field: SerializeField][JsonProperty("tier")] public int Tier { get; set; }
    [field: SerializeField][JsonProperty("unlock_day")] public int UnlockDay { get; set; }
    [field: SerializeField][JsonProperty("scoring_items")] public int ScoringItems { get; set; }
    [field: SerializeField][JsonProperty("time_limit_sec")] public float TimeLimitSec { get; set; }
}

/// <summary>StreamingAssets/json/cocktails.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewCocktailDataSO", menuName = "Data/New/CocktailDataSO")]
public class NewCocktailDataSO : ScriptableObject
{
    public NewCocktailData[] cocktailData;
}
