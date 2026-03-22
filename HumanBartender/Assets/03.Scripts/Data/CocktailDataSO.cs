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
        'ㄱ', 'ㄲ', 'ㄴ', 'ㄷ', 'ㄸ', 'ㄹ', 'ㅁ', 'ㅂ', 'ㅃ',
        'ㅅ', 'ㅆ', 'ㅇ', 'ㅈ', 'ㅉ', 'ㅊ', 'ㅋ', 'ㅌ', 'ㅍ', 'ㅎ'
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
        if (string.IsNullOrEmpty(text)) return "기타";

        char firstChar = text[0];

        // 한글 '가'(0xAC00) ~ '힣'(0xD7A3) 사이의 글자인지 확인
        if (firstChar >= 0xAC00 && firstChar <= 0xD7A3)
        {
            // 유니코드 수학 공식을 이용해 초성 인덱스 추출
            int uniVal = firstChar - 0xAC00;
            int choIdx = uniVal / (21 * 28);
            return ChoSung[choIdx].ToString(); // "ㄱ", "ㄴ" 등을 반환
        }

        // 한글이 아니라면 (영어, 숫자 등) 첫 글자를 대문자로 반환하거나 "기타"로 묶습니다.
        // 알파벳인 경우 (예: "B-52" -> "B" 카테고리)
        if (char.IsLetter(firstChar))
        {
            return firstChar.ToString().ToUpper();
        }

        return "기타"; // 숫자나 특수문자로 시작하는 경우
    }
}


[Serializable]
public class CocktailDataBase
{
    public CocktailData[] cocktails;
}