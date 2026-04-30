using System;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 배경 클립 데이터.
/// 실제 전환 로직은 MixerBehaviour에서 처리.
/// </summary>
[Serializable]
public class CutSceneBGBehaviour : PlayableBehaviour
{
    [Header("배경 이미지")]
    [Tooltip("Resources/Cutscenes/ 하위 경로 (확장자 제외)")]
    public string bgPath;

    [Header("전환")]
    [Tooltip("등장 페이드 시간 (0이면 즉시)")]
    public float fadeInDuration = 0.3f;

    [Tooltip("퇴장 페이드 시간 (0이면 즉시)")]
    public float fadeOutDuration = 0.3f;

    [Tooltip("배경 색상 틴트 (White = 원본)")]
    public Color tint = Color.white;

    // ── 런타임 (Mixer에서 관리) ───────────────────────────────────────
    [NonSerialized] internal CutSceneTimelineManager manager;
    [NonSerialized] internal bool isActive;
    [NonSerialized] internal bool fadeInDone;
    [NonSerialized] internal bool fadeOutStarted;
    [NonSerialized] internal Sprite cachedSprite;
    [NonSerialized] internal bool spriteLoaded;

    public override void OnPlayableCreate(Playable playable)
    {
        isActive = false;
        fadeInDone = false;
        fadeOutStarted = false;
        spriteLoaded = false;
    }

    /// <summary> 스프라이트 로드 (한 번만) </summary>
    internal Sprite LoadSprite()
    {
        if (!spriteLoaded)
        {
            spriteLoaded = true;
            if (!string.IsNullOrEmpty(bgPath))
                cachedSprite = Resources.Load<Sprite>($"Cutscenes/{bgPath}");
        }
        return cachedSprite;
    }
}
