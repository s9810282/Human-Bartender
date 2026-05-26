using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class CutSceneDialogueClip : PlayableAsset, ITimelineClipAsset
{
    public CutSceneDialogueBehaviour template = new();

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<CutSceneDialogueBehaviour>.Create(graph, template);
    }
}
