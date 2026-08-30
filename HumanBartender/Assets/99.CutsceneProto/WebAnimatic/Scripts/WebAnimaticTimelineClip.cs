using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.WebAnimatic
{
    [Serializable]
    public sealed class WebAnimaticTimelineBehaviour : PlayableBehaviour
    {
        public string sceneId;
        WebAnimaticPlayer _player;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is not WebAnimaticPlayer player || string.IsNullOrEmpty(sceneId))
                return;

            _player = player;
            player.SetExternallyDriven(true);
            if (!player.LoadSceneById(sceneId))
                return;

            bool emitAudio = info.effectivePlayState == PlayState.Playing
                             && info.evaluationType == FrameData.EvaluationType.Playback;
            player.Evaluate((float)playable.GetTime(), emitAudio);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            if (_player != null)
                _player.SetExternallyDriven(false);
            _player = null;
        }
    }

    [Serializable]
    public sealed class WebAnimaticTimelineClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("WebAnimaticLibrary.scenes[].id 예: s1, s2, s3, s5, s99")]
        public string sceneId = "s1";

        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<WebAnimaticTimelineBehaviour> playable =
                ScriptPlayable<WebAnimaticTimelineBehaviour>.Create(graph);
            playable.GetBehaviour().sceneId = sceneId;
            return playable;
        }
    }
}
