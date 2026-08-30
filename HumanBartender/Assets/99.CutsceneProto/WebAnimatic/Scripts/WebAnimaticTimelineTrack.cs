using UnityEngine.Timeline;

namespace ProjectLuna.WebAnimatic
{
    [TrackColor(0.20f, 0.76f, 0.92f)]
    [TrackClipType(typeof(WebAnimaticTimelineClip))]
    [TrackBindingType(typeof(WebAnimaticPlayer))]
    public sealed class WebAnimaticTimelineTrack : TrackAsset
    {
    }
}
