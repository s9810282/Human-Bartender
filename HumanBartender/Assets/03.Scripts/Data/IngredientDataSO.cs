using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IngrediantDataSO", menuName = "Scriptable Objects/IngrediantDataSO")]
public class IngredientDataSO : ScriptableObject
{
    public IngredientDataBase ingredientData;

}


[Serializable]
public class IngredientDataBase
{
    public IngredientData[] ingredients;
}

[Serializable]
public struct IngredientData
{
    public string id;
    public string name;
    public string name_en;
    public string category;
    public string sprite;
    public int max_count;
}
