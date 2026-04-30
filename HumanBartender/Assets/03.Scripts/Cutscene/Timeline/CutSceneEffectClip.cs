using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneEffectClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneEffectBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneEffectBehaviour>.Create(graph, template);
    }
}
