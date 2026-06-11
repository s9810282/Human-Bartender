using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CategoryColorData", menuName = "Scriptable Objects/CategoryColorData")]
public class CategoryColorData : ScriptableObject
{
    public List<string> categorys = new List<string>();
    public List<Color> colors = new List<Color>();
}
