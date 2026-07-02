using DG.Tweening;
using System;
using System.Collections.Generic;
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
    Effect
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

[Serializable]
public struct TestPositionPreset
{
    public AnchorType Anchor;
    public float OffsetX;
    public float OffsetY;
}



[System.Serializable]
public struct TestCutSceneStep
{
    public float time;
    public ECutSceneAction action;
    public TestPositionPreset positionPreset;

    [Space(10f)]

    public string path;

    [Space(10f)]

    [Tooltip("등장 타입")]
    public EEneterPreset enterType;
    [Tooltip("등장 시간")]
    public float enterDuration;
    [Tooltip("등장 이징 (Unset = OutCubic 기본값)")]
    public Ease enterEase;
    [Tooltip("등장 슬라이드 거리 비율 (1.0 = 화면 전체). 0이면 기본값 1.0")]
    [Range(0.1f, 2.0f)]
    public float enterSlideDistance;

    [Space(10f)]

    [Tooltip("유지 시간")]
    public float duration;

    [Space(10f)]

    [Tooltip("퇴장 타입 (일괄 퇴장 시 None)")]
    public EExitPreset exitType;
    [Tooltip("퇴장 시간")]
    public float exitDuration;
    [Tooltip("퇴장 이징 (Unset = InCubic 기본값)")]
    public Ease exitEase;
    [Tooltip("퇴장 슬라이드 거리 비율 (1.0 = 화면 전체). 0이면 기본값 1.0")]
    [Range(0.1f, 2.0f)]
    public float exitSlideDistance;
}


/// <summary>컷씬 스텝 목록과 카메라 동작을 인스펙터에서 설정하는 테스트용 ScriptableObject.</summary>
[CreateAssetMenu(fileName = "TestCutScene", menuName = "Scriptable Objects/TestCutScene")]
public class TestCutScene : ScriptableObject
{
    public bool isCutSceneMove;
    public float cameraDuration;
    public float cameraRatio;
    public ECutSceneCameraMoveType cameraMoveType;

    public Ease easeGraph;

    public bool isSetBGSprite;
    public string bgPath;

    public List<TestCutSceneStep> steps;
}
