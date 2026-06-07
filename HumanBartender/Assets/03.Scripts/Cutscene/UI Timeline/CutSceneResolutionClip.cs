using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneResolutionClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneResolutionBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneResolutionBehaviour>.Create(graph, template);
    }
}
