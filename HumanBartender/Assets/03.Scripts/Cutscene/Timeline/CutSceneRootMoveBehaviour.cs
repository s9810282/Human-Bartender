using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class CutSceneRootMoveBehaviour : PlayableBehaviour
{
    public ECutSceneCameraMoveType direction = ECutSceneCameraMoveType.Right;
    [Range(0.05f, 0.5f)] public float moveRatio = 0.15f;
    public Ease ease = Ease.OutCubic;

    // 런타임
    [NonSerialized] internal CutSceneTimelineManager manager;
    [NonSerialized] private bool initialized;
    [NonSerialized] private Vector2 startPos;
    [NonSerialized] private Vector2 endPos;

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

            Vector2 delta = direction switch
            {
                ECutSceneCameraMoveType.Right => new Vector2(-w * moveRatio, 0),
                ECutSceneCameraMoveType.Left  => new Vector2( w * moveRatio, 0),
                ECutSceneCameraMoveType.Up    => new Vector2(0, -h * moveRatio),
                ECutSceneCameraMoveType.Down  => new Vector2(0,  h * moveRatio),
                _                            => Vector2.zero,
            };

            startPos = Vector2.zero;
            endPos   = delta;
        }

        // 클립 내 정규화 시간 (0~1)
        float normalizedTime = (float)(playable.GetTime() / playable.GetDuration());
        normalizedTime = Mathf.Clamp01(normalizedTime);

        // Ease 적용
        float easedTime = DOVirtual.EasedValue(0f, 1f, normalizedTime, ease);

        manager.CutSceneRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, easedTime);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        // 클립 종료 시 위치 유지 (ResetRootPosition은 Timeline 전체 종료 시 호출)
    }
}
