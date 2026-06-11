using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PatternNodeData
{
    public string patternName;
    public int lineIndex;
    public float patternT;
}

[System.Serializable]
public struct PatternData
{
    public PatternNodeData[] patternDatas;
}

[CreateAssetMenu(fileName = "NodePatternData", menuName = "Scriptable Objects/NodePatternData")]
public class NodePatternData : ScriptableObject
{
    public PatternData[] patternDatas;
}
