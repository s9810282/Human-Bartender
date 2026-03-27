using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

[Serializable]
public struct RecipeIngredient
{
    [JsonProperty("ingredient")] public string Ingredient { get; set; }
    [JsonProperty("count")] public int Count { get; set; }
}

[Serializable]
public struct CocktailData
{
    [JsonProperty("id")] public string Id { get; set; }
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("name_en")] public string NameEn { get; set; }
    [JsonProperty("recipe")] public RecipeIngredient[] Recipe { get; set; }
    [JsonProperty("method")] public string Method { get; set; }
    [JsonProperty("target_count")] public int TargetCount { get; set; }
    [JsonProperty("keywords")] public string[] Keywords { get; set; }
    [JsonProperty("baseIngredient")] public string BaseIngredient { get; set; }
    [JsonProperty("flavor_text")] public string FlavorText { get; set; }
}

[CreateAssetMenu(fileName = "New CocktailData", menuName = "Data/CockTailData")]
public class CocktailDataSO : ScriptableObject
{
    public CocktailDataBase cocktailData;

    public CocktailData[] allCocktails;
    public CocktailData[] cachedSortedByName;
    public Dictionary<string, CocktailData[]> cachedByInitialConsonant;
    public Dictionary<string, CocktailData[]> cachedByTaste;
    public Dictionary<string, CocktailData[]> cachedByBase;
    public Dictionary<string, CocktailData[]> cachedByMethod;
    public Dictionary<string, CocktailData[]> cachedByStyle;

    private static readonly char[] ChoSung = {
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ',
        'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
    };

    public void Cached()
    {
        allCocktails = cocktailData?.Cocktails;

        if (allCocktails == null || allCocktails.Length == 0) return;

        cachedSortedByName = SortByName();
        cachedByInitialConsonant = GroupByInitial();
        cachedByTaste = GroupByTaste();
        cachedByBase = GroupByBase();
        cachedByMethod = GroupByMethod();
        cachedByStyle = GroupByStyle();
    }

    public CocktailData[] SortByName()
    {
        return allCocktails.OrderBy(c => c.Name).ToArray();
    }

    public Dictionary<string, CocktailData[]> GroupByInitial()
    {
        return allCocktails
            .GroupBy(c => GetInitialConsonant(c.Name))
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Name).ToArray());
    }

    public Dictionary<string, CocktailData[]> GroupByTaste()
    {
        return allCocktails.GroupBy(c => c.Keywords != null && c.Keywords.Length > 0 ? c.Keywords[0] : "기타")
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public Dictionary<string, CocktailData[]> GroupByBase()
    {
        return allCocktails.GroupBy(c => c.BaseIngredient)
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public Dictionary<string, CocktailData[]> GroupByMethod()
    {
        return allCocktails.GroupBy(c => c.Method)
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }

    public Dictionary<string, CocktailData[]> GroupByStyle()
    {
        Dictionary<string, List<CocktailData>> styleDict = new Dictionary<string, List<CocktailData>>();

        foreach (var cocktail in allCocktails)
        {
            if (cocktail.Keywords == null) continue;

            for (int i = 1; i <= 2; i++)
            {
                if (cocktail.Keywords.Length > i)
                {
                    string styleKey = cocktail.Keywords[i];
                    if (!styleDict.ContainsKey(styleKey))
                    {
                        styleDict[styleKey] = new List<CocktailData>();
                    }
                    styleDict[styleKey].Add(cocktail);
                }
            }
        }

        return styleDict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToArray());
    }

    private string GetInitialConsonant(string text)
    {
        if (string.IsNullOrEmpty(text)) return "기타";
        char firstChar = text[0];

        if (firstChar >= 0xAC00 && firstChar <= 0xD7A3)
        {
            int uniVal = firstChar - 0xAC00;
            int choIdx = uniVal / (21 * 28);
            return ChoSung[choIdx].ToString();
        }

        if (char.IsLetter(firstChar))
        {
            return firstChar.ToString().ToUpper();
        }
        return "기타";
    }
}

[Serializable]
public class CocktailDataBase
{
    [JsonProperty("cocktails")] public CocktailData[] Cocktails { get; set; }
}