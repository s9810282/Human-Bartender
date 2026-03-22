using System;
using UnityEngine;


[Serializable]
public struct RecipeIngredient
{
    public string 
        ingredient;
    public int count;
}

[Serializable]
public struct CocktailData
{
    public string id;
    public string name;
    public string name_en;
    public RecipeIngredient[] recipe;
    public string method;
    public int target_count;
    public string[] keywords;
    public string baseIngredient;
    public string flavor_text;
}


