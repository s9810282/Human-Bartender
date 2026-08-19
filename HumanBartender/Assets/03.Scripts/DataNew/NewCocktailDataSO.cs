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
    // tags는 문자열 배열이 아니라 {ko, en} 객체의 배열이다. string[]으로 두면 cocktails.json 전체가
    // JsonReaderException으로 터지고, NewDataLoadManager의 로드 순서상 그 뒤 데이터가 전부 안 들어온다.
    [field: SerializeField][JsonProperty("tags")] public LocalizedText[] Tags { get; set; }
    [field: SerializeField][JsonProperty("flavor")] public LocalizedText Flavor { get; set; }
    [field: SerializeField][JsonProperty("recipe_desc")] public LocalizedText RecipeDesc { get; set; }
    [field: SerializeField][JsonProperty("unlock_when")] public string UnlockWhen { get; set; }
    [field: SerializeField][JsonProperty("recipe")] public NewCocktailRecipeStep[] Recipe { get; set; }
    [field: SerializeField][JsonProperty("gimmick_count")] public int GimmickCount { get; set; }
    [field: SerializeField][JsonProperty("tier")] public int Tier { get; set; }
    [field: SerializeField][JsonProperty("unlock_day")] public int UnlockDay { get; set; }
    [field: SerializeField][JsonProperty("scoring_items")] public int ScoringItems { get; set; }
    [field: SerializeField][JsonProperty("time_limit_sec")] public float TimeLimitSec { get; set; }
    /// <summary>완성 컷씬에 쓰는 스프라이트 이름. 구 CocktailData의 Finish_animation에 대응한다.</summary>
    [field: SerializeField][JsonProperty("sprite")] public string Sprite { get; set; }
    /// <summary>서빙 컷씬에 쓰는 스프라이트 이름. 구 CocktailData의 Serve_animation에 대응한다.</summary>
    [field: SerializeField][JsonProperty("serve_sprite")] public string ServeSprite { get; set; }
}

/// <summary>StreamingAssets/json/cocktails.json을 보유하는 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "NewCocktailDataSO", menuName = "Data/New/CocktailDataSO")]
public class NewCocktailDataSO : ScriptableObject
{
    public NewCocktailData[] cocktailData;
}
