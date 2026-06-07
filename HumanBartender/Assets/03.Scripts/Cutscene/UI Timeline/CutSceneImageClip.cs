using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneImageClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneImageBehaviour template = new();

    // 클립이 지원하는 기능 플래그
    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<CutSceneImageBehaviour>.Create(graph, template);
        return playable;
    }
}
