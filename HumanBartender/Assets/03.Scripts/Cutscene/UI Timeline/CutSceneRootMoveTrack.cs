using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;


[TrackColor(0.3f, 0.8f, 0.6f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneRootMoveClip))]
public class CutSceneRootMoveTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneRootMoveMixerBehaviour>.Create(graph, inputCount);
    }
}


