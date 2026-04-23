using UnityEngine;



public enum ECutSceneCameraMoveType
{
    None = 0,
    Right,
    Left,
    Down,
    Up,
}

public enum ECutSceneAction
{
    None,
    ShowImage,
    HideImage,
    HideAll,
    ShowLayout,
}

public enum EEneterPreset
{
    None = 0,
    Cut,
    FadeIn,

    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,

    ScaleUp,
}

public enum EExitPreset
{
    None = 0,
    Cut,
    FadeOut,

    SlideLeft,
    SlideRight,
    SlideUp,
    SlideDown,

    ScaleDown,
}


public struct TestCutSceneStep
{
    public float time;
    public ECutSceneAction action;
    public LayoutPreset layoutPreset;
}


[CreateAssetMenu(fileName = "TestCutScene", menuName = "Scriptable Objects/TestCutScene")]
public class TestCutScene : ScriptableObject
{
    public bool isCameraMove;
    public ECutSceneCameraMoveType camerAMoveType;

    public bool isSetBGSprite;
    public string bgPath;


}

