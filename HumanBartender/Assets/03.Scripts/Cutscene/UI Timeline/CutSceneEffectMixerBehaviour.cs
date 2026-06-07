using UnityEngine.Playables;

public class CutSceneEffectMixerBehaviour : PlayableBehaviour
{
    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        int inputCount = playable.GetInputCount();
        bool anyActive = false;

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            if (weight <= 0f) continue;

            anyActive = true;
            var inputPlayable = (ScriptPlayable<CutSceneEffectBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;
        }
    }
}
