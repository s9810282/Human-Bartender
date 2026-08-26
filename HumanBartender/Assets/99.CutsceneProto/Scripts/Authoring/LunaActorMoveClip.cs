using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public enum LunaFacingMode
    {
        Keep,
        Auto,
        Left,
        Right
    }

    [Serializable]
    public sealed class LunaActorMoveBehaviour : PlayableBehaviour
    {
        public Transform fromAnchor;
        public Transform toAnchor;
        public LunaFacingMode facingMode;
        public bool rightMeansFlipXFalse = true;
        public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (playerData is not Transform target || fromAnchor == null || toAnchor == null)
                return;

            double duration = playable.GetDuration();
            float normalized = duration <= 0d
                ? 1f
                : Mathf.Clamp01((float)(playable.GetTime() / duration));
            float curved = positionCurve == null ? normalized : positionCurve.Evaluate(normalized);
            target.position = Vector3.LerpUnclamped(fromAnchor.position, toAnchor.position, curved);
            ApplyFacing(target, fromAnchor.position, toAnchor.position);
        }

        private void ApplyFacing(Transform target, Vector3 from, Vector3 to)
        {
            LunaFacingMode resolved = facingMode;
            if (resolved == LunaFacingMode.Auto)
            {
                float delta = to.x - from.x;
                if (Mathf.Abs(delta) < 0.0001f)
                    return;
                resolved = delta > 0f ? LunaFacingMode.Right : LunaFacingMode.Left;
            }

            if (resolved == LunaFacingMode.Keep)
                return;

            bool faceRight = resolved == LunaFacingMode.Right;
            bool flipX = rightMeansFlipXFalse ? !faceRight : faceRight;
            foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
                renderer.flipX = flipX;
        }
    }

    [Serializable]
    public sealed class LunaActorMoveClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<Transform> fromAnchor;
        public ExposedReference<Transform> toAnchor;
        public LunaFacingMode facingMode = LunaFacingMode.Auto;
        public bool rightMeansFlipXFalse = true;
        public AnimationCurve positionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.ClipIn | ClipCaps.SpeedMultiplier;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<LunaActorMoveBehaviour> playable = ScriptPlayable<LunaActorMoveBehaviour>.Create(graph);
            LunaActorMoveBehaviour behaviour = playable.GetBehaviour();
            behaviour.fromAnchor = fromAnchor.Resolve(graph.GetResolver());
            behaviour.toAnchor = toAnchor.Resolve(graph.GetResolver());
            behaviour.facingMode = facingMode;
            behaviour.rightMeansFlipXFalse = rightMeansFlipXFalse;
            behaviour.positionCurve = positionCurve;
            return playable;
        }

        public Transform ResolveFrom(IExposedPropertyTable resolver)
        {
            return fromAnchor.Resolve(resolver);
        }

        public Transform ResolveTo(IExposedPropertyTable resolver)
        {
            return toAnchor.Resolve(resolver);
        }
    }
}
