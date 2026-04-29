using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

/// <summary>
/// 클립 구간 동안 활성화된 Image를 시작 위치에서 끝 위치로 이동.
/// ImageTrack과 함께 사용하여 "애니메이션하면서 이동" 연출 가능.
/// </summary>
[Serializable]
public class CutSceneMoveBehaviour : PlayableBehaviour
{
    [Header("대상")]
    [Tooltip("ImageTrack에서 등록한 imagePath와 동일한 값")]
    public string imagePath;

    [Header("시작 위치")]
    public AnchorType startAnchor = AnchorType.Left;
    public float startOffsetX = 0f;
    public float startOffsetY = 0f;

    [Header("끝 위치")]
    public AnchorType endAnchor = AnchorType.Right;
    public float endOffsetX = 0f;
    public float endOffsetY = 0f;

    [Header("이동")]
    public Ease moveEase = Ease.InOutCubic;

    // ── 런타임 ────────────────────────────────────────────────────────
    [NonSerialized] internal CutSceneTimelineManager manager;
    [NonSerialized] private Image targetImage;
    [NonSerialized] private bool initialized;
    [NonSerialized] private Vector2 startPos;
    [NonSerialized] private Vector2 endPos;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        initialized = false;
        targetImage = null;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        // 대상 Image 찾기
        if (targetImage == null)
        {
            targetImage = manager.GetActiveImage(imagePath);
            if (targetImage == null) return;
        }

        // 시작/끝 월드 위치 계산 (한 번만)
        if (!initialized)
        {
            initialized = true;
            startPos = CalculatePosition(startAnchor, startOffsetX, startOffsetY);
            endPos   = CalculatePosition(endAnchor,   endOffsetX,   endOffsetY);

            // 시작 위치로 즉시 이동
            RectTransform rect = targetImage.GetComponent<RectTransform>();
            rect.anchoredPosition = startPos;
        }

        // 클립 내 정규화 시간 (0~1)
        float normalizedTime = (float)(playable.GetTime() / playable.GetDuration());
        normalizedTime = Mathf.Clamp01(normalizedTime);

        // Ease 적용
        float easedTime = DOVirtual.EasedValue(0f, 1f, normalizedTime, moveEase);

        // 보간
        RectTransform rt = targetImage.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.Lerp(startPos, endPos, easedTime);
    }

    Vector2 CalculatePosition(AnchorType anchor, float offsetX, float offsetY)
    {
        RectTransform canvasRect = manager.CanvasRect;
        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        // 앵커 → 캔버스 내 기준점
        Vector2 anchorPos = anchor switch
        {
            AnchorType.Center      => new Vector2(w * 0.5f, h * 0.5f),
            AnchorType.Left        => new Vector2(0,        h * 0.5f),
            AnchorType.Right       => new Vector2(w,        h * 0.5f),
            AnchorType.TopLeft     => new Vector2(0,        h),
            AnchorType.TopRight    => new Vector2(w,        h),
            AnchorType.BottomLeft  => new Vector2(0,        0),
            AnchorType.BottomRight => new Vector2(w,        0),
            _                     => new Vector2(w * 0.5f, h * 0.5f),
        };

        return anchorPos + new Vector2(w * offsetX, h * offsetY);
    }
}
