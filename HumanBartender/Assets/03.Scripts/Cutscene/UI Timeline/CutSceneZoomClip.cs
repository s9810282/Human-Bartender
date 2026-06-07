using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneZoomClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneZoomBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneZoomBehaviour>.Create(graph, template);
    }
}
