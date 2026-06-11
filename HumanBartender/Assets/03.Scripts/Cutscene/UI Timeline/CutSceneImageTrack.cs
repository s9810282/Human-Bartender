using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.5f, 1f, 0.5f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneImageClip))]
public class CutSceneImageTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneImageMixerBehaviour>.Create(graph, inputCount);
    }
}
