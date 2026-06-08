using UnityEngine;
using UnityEngine.Playables;

public class CutSceneRootMoveMixerBehaviour : PlayableBehaviour
{
    // Hold 모드로 끝난 클립의 마지막 위치를 기억
    private bool hasHeldPosition;
    private Vector2 heldPosition;

    // 이전 프레임에서 활성이었던 클립 추적
    private int lastActiveIndex = -1;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var manager = playerData as CutSceneTimelineManager;
        if (manager == null || manager.CutSceneRoot == null) return;

        int inputCount = playable.GetInputCount();
        int currentActiveIndex = -1;

        // 활성 클립 찾기 + manager 연결
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            currentActiveIndex = i;

            var inputPlayable = (ScriptPlayable<CutSceneRootMoveBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;
        }

        // 클립이 방금 끝났는지 감지
        if (lastActiveIndex >= 0 && currentActiveIndex < 0)
        {
            // 이전 활성 클립이 끝남
            var prevPlayable = (ScriptPlayable<CutSceneRootMoveBehaviour>)playable.GetInput(lastActiveIndex);
            var prevBehaviour = prevPlayable.GetBehaviour();

            switch (prevBehaviour.endMode)
            {
                case ERootMoveEndMode.Hold:
                    hasHeldPosition = true;
                    heldPosition = prevBehaviour.EndPos;
                    break;

                case ERootMoveEndMode.Return:
                    hasHeldPosition = false;
                    manager.CutSceneRoot.anchoredPosition = prevBehaviour.StartPos;
                    break;
            }
        }

        // 활성 클립이 바뀜 (새 클립 시작)
        if (currentActiveIndex >= 0 && currentActiveIndex != lastActiveIndex)
        {
            // 새 클립이 시작되면 held 상태 해제 (새 클립이 직접 제어)
        }

        // 활성 클립이 없고 Hold 상태면 위치 유지
        if (currentActiveIndex < 0 && hasHeldPosition)
        {
            manager.CutSceneRoot.anchoredPosition = heldPosition;
        }

        lastActiveIndex = currentActiveIndex;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        hasHeldPosition = false;
        lastActiveIndex = -1;
    }
}
