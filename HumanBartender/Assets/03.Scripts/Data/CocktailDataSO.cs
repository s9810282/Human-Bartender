using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New CocktailData", menuName = "Data/CockTailData")]
public class CocktailDataSO : ScriptableObject
{
    public CocktailDataBase cocktailData;
}

[Serializable]
public class CocktailDataBase
{
    public CocktailData[] cocktails;
}