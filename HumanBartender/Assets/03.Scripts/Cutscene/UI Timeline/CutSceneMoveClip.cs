using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneMoveClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneMoveBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneMoveBehaviour>.Create(graph, template);
    }
}
