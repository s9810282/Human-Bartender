using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


[TrackColor(1f, 1f, 0.4f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneEffectClip))]
public class CutSceneEffectTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneEffectMixerBehaviour>.Create(graph, inputCount);
    }
}
