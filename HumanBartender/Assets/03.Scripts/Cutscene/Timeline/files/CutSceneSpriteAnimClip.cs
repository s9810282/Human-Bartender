using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneSpriteAnimClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneSpriteAnimBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.Looping;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneSpriteAnimBehaviour>.Create(graph, template);
    }
}
