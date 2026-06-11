using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(1f, 0.3f, 0.3f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneShakeClip))]
public class CutSceneShakeTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneShakeMixerBehaviour>.Create(graph, inputCount);
    }
}
