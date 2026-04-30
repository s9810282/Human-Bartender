using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[Serializable]
public class CutSceneImageBehaviour : PlayableBehaviour
{
    // ── 데이터 (인스펙터에서 설정) ────────────────────────────────────
    [Header("Image")]
    public string imagePath;

    [Header("Position")]
    public AnchorType anchor = AnchorType.Center;
    public float offsetX;
    public float offsetY;

    [Header("Enter")]
    public EEneterPreset enterType = EEneterPreset.FadeIn;
    public float enterDuration = 0.3f;
    public Ease enterEase = Ease.Unset;
    [Range(0.1f, 2f)] public float enterSlideDistance = 1.0f;

    [Header("Exit")]
    public EExitPreset exitType = EExitPreset.FadeOut;
    public float exitDuration = 0.3f;
    public Ease exitEase = Ease.Unset;
    [Range(0.1f, 2f)] public float exitSlideDistance = 1.0f;

    // ── 런타임 상태 (Mixer에서 관리) ──────────────────────────────────
    [NonSerialized] public Image assignedImage;
    [NonSerialized] public bool isActive;
    [NonSerialized] public bool enterDone;
    [NonSerialized] public bool exitDone;

    public override void OnPlayableCreate(Playable playable)
    {
        isActive = false;
        enterDone = false;
        exitDone = false;
    }
}
