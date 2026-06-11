using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.65f, 0.45f, 0.85f)]
[TrackBindingType(typeof(CutSceneTimelineManager))]
[TrackClipType(typeof(CutSceneSpriteAnimClip))]
public class CutSceneSpriteAnimTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<CutSceneSpriteAnimMixerBehaviour>.Create(graph, inputCount);
    }
}
