
using UnityEngine.Playables;

public class CutSceneRootMoveMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            var inputPlayable = (ScriptPlayable<CutSceneRootMoveBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        // Timeline 종료 시 Root 위치 복원은
        // CutSceneTimelineManager.OnTimelineStopped()에서 처리
    }
}