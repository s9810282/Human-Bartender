using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

public enum ERootMoveEndMode
{
    [Tooltip("클립 종료 후 이동한 위치 유지")]
    Hold,

    [Tooltip("클립 종료 후 원래 위치로 복귀")]
    Return,
}

/// <summary>
/// 클립 구간 동안 cutSceneRoot를 패닝.
///
/// Hold:   클립 끝나도 위치 유지 → 다음 RootMove 클립이 거기서 이어갈 수 있음
/// Return: 클립 끝나면 시작 위치로 복귀
///
/// 시작 위치는 항상 현재 Root 위치 기준이므로 Hold 클립 뒤에 이어 붙이면 자연스럽게 연결됨.
/// </summary>
[Serializable]
public class CutSceneRootMoveBehaviour : PlayableBehaviour
{
    [Header("패닝")]
    public ECutSceneCameraMoveType direction = ECutSceneCameraMoveType.Right;
    [Range(0.05f, 1.0f)] public float moveRatio = 0.15f;
    public Ease ease = Ease.OutCubic;

    [Header("종료 동작")]
    [Tooltip("Hold: 이동한 위치 유지, Return: 시작 위치로 복귀")]
    public ERootMoveEndMode endMode = ERootMoveEndMode.Hold;

    // ── 런타임 ────────────────────────────────────────────────────────
    [NonSerialized] internal CutSceneTimelineManager manager;
    [NonSerialized] private bool initialized;
    [NonSerialized] private Vector2 startPos;
    [NonSerialized] private Vector2 endPos;

    // Mixer에서 접근
    internal Vector2 StartPos => startPos;
    internal Vector2 EndPos   => endPos;

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        initialized = false;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null || manager.CutSceneRoot == null) return;

        if (!initialized)
        {
            initialized = true;
            float w = manager.CanvasRect.rect.width;
            float h = manager.CanvasRect.rect.height;

            // 현재 Root 위치에서 시작 (이전 클립이 Hold로 남긴 위치 이어받기)
            startPos = manager.CutSceneRoot.anchoredPosition;

            Vector2 delta = direction switch
            {
                ECutSceneCameraMoveType.Right => new Vector2(-w * moveRatio, 0),
                ECutSceneCameraMoveType.Left  => new Vector2( w * moveRatio, 0),
                ECutSceneCameraMoveType.Up    => new Vector2(0, -h * moveRatio),
                ECutSceneCameraMoveType.Down  => new Vector2(0,  h * moveRatio),
                _                            => Vector2.zero,
            };

            endPos = startPos + delta;
        }

        float normalizedTime = (float)(playable.GetTime() / playable.GetDuration());
        normalizedTime = Mathf.Clamp01(normalizedTime);

        float easedTime = DOVirtual.EasedValue(0f, 1f, normalizedTime, ease);

        manager.CutSceneRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, easedTime);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // 종료 처리는 Mixer에서 담당 (endMode에 따라 Hold/Return)
    }
}
