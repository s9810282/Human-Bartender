using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public struct PositionPreset
{
    public string achor;
    public float width;
    public float height;

    public float offset_x;
    public float offset_y;
}



[CreateAssetMenu(fileName = "CutSceneDataSO", menuName = "Data/CutSceneDataSO")]
public class CutSceneDataSO : ScriptableObject
{
    public CutSceneDataBase cutSceneData;
}


[Serializable]
public class CutSceneDataBase
{
    public Dictionary<string, PositionPreset> position_presets;
}