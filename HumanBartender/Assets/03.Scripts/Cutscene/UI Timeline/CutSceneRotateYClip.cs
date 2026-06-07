using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneRotateYClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneRotateYBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneRotateYBehaviour>.Create(graph, template);
    }
}
