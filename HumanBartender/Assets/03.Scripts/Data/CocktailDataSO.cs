using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
        '¤¡', '¤¢', '¤¤', '¤§', '¤¨', '¤©', '¤±', '¤²', '¤³',
        '¤µ', '¤¶', '¤·', '¤¸', '¤¹', '¤º', '¤»', '¤¼', '¤½', '¤¾'
    };


    public void Cached()
    {
        allCocktails = cocktailData.cocktails;

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
        return allCocktails.OrderBy(c => c.name).ToArray();
    }
    public Dictionary<string, CocktailData[]> GroupByInitial()
    {
        return cachedByInitialConsonant = allCocktails
            .GroupBy(c => GetInitialConsonant(c.name))
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.name).ToArray());
    }
    public Dictionary<string, CocktailData[]> GroupByTaste()
    {
        return allCocktails.GroupBy(c => c.keywords[0])
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }
    public Dictionary<string, CocktailData[]> GroupByBase()
    {
        return allCocktails.GroupBy(c => c.baseIngredient)
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }
    public Dictionary<string, CocktailData[]> GroupByMethod()
    {
        return allCocktails.GroupBy(c => c.method)
                           .ToDictionary(g => g.Key, g => g.ToArray());
    }
    public Dictionary<string, CocktailData[]> GroupByStyle()
    {
        Dictionary<string, List<CocktailData>> styleDict = new Dictionary<string, List<CocktailData>>();

        foreach (var cocktail in allCocktails)
        {
            for (int i = 1; i <= 2; i++)
            {
                if (cocktail.keywords.Length > i)
                {
                    string styleKey = cocktail.keywords[i];
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
        if (string.IsNullOrEmpty(text)) return "±âÅ¸";

        char firstChar = text[0];

        // ÇÑ±Û '°¡'(0xAC00) ~ 'ÆR'(0xD7A3) »çÀÌÀÇ ±ÛÀÚÀÎÁö È®ÀÎ
        if (firstChar >= 0xAC00 && firstChar <= 0xD7A3)
        {
            // À¯´ÏÄÚµå ¼öÇÐ °ø½ÄÀ» ÀÌ¿ëÇØ ÃÊ¼º ÀÎµ¦½º ÃßÃâ
            int uniVal = firstChar - 0xAC00;
            int choIdx = uniVal / (21 * 28);
            return ChoSung[choIdx].ToString(); // "¤¡", "¤¤" µîÀ» ¹ÝÈ¯
        }

        // ÇÑ±ÛÀÌ ¾Æ´Ï¶ó¸é (¿µ¾î, ¼ýÀÚ µî) Ã¹ ±ÛÀÚ¸¦ ´ë¹®ÀÚ·Î ¹ÝÈ¯ÇÏ°Å³ª "±âÅ¸"·Î ¹­½À´Ï´Ù.
        // ¾ËÆÄºªÀÎ °æ¿ì (¿¹: "B-52" -> "B" Ä«Å×°í¸®)
        if (char.IsLetter(firstChar))
        {
            return firstChar.ToString().ToUpper();
        }

        return "±âÅ¸"; // ¼ýÀÚ³ª Æ¯¼ö¹®ÀÚ·Î ½ÃÀÛÇÏ´Â °æ¿ì
    }
}


[Serializable]
public class CocktailDataBase
{
    public CocktailData[] cocktails;
}