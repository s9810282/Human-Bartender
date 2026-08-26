using UnityEngine;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [TrackColor(0.12f, 0.82f, 0.78f)]
    [TrackClipType(typeof(LunaActorMoveClip))]
    [TrackBindingType(typeof(Transform))]
    public sealed class LunaActorMoveTrack : TrackAsset
    {
    }
}
