using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PatternData
{
    public string patternName;
    public float[] patternTime;
}


[CreateAssetMenu(fileName = "NodePatternData", menuName = "Scriptable Objects/NodePatternData")]
public class NodePatternData : ScriptableObject
{
    public PatternData[] patternDatas;
}
