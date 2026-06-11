using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.1f, 0.1f, 0.1f)]
[TrackClipType(typeof(SpriteRevealClip))]
[TrackBindingType(typeof(SpriteRenderer))]
public class SpriteRevealTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<SpriteRevealMixerBehaviour>.Create(graph, inputCount);
    }
}
