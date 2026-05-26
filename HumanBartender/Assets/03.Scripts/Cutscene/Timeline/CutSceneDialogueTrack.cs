using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.95f, 0.85f, 0.4f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneDialogueClip))]
public class CutSceneDialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneDialogueMixerBehaviour>.Create(graph, inputCount);
    }
}
