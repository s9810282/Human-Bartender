using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneShakeClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneShakeBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneShakeBehaviour>.Create(graph, template);
    }
}
