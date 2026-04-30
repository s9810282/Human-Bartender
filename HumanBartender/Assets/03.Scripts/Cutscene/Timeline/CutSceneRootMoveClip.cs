using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneRootMoveClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneRootMoveBehaviour rootTemplate = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneRootMoveBehaviour>.Create(graph, rootTemplate);
    }
}
