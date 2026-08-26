using UnityEngine;
using UnityEngine.Playables;

namespace ProjectLuna.CutscenePrototype
{
    public enum PrototypeTimelineSegment
    {
        LabEntry,
        LabAttack,
        LabEscape
    }

    [CreateAssetMenu(menuName = "Project L.U.N.A/Cutscene Prototype/Timeline Segment")]
    public sealed class PrototypeTimelineAsset : PlayableAsset
    {
        [SerializeField] private PrototypeTimelineSegment segment;
        [SerializeField, Min(0.1f)] private float segmentDuration = 1.5f;

        public PrototypeTimelineSegment Segment => segment;
        public override double duration => segmentDuration;

        public void Configure(PrototypeTimelineSegment value, float valueDuration)
        {
            segment = value;
            segmentDuration = Mathf.Max(0.1f, valueDuration);
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            ScriptPlayable<PrototypeTimelineBehaviour> playable =
                ScriptPlayable<PrototypeTimelineBehaviour>.Create(graph);
            PrototypeTimelineBehaviour behaviour = playable.GetBehaviour();
            behaviour.Segment = segment;
            behaviour.Duration = segmentDuration;
            behaviour.View = owner != null ? owner.GetComponent<PrototypeCutsceneView>() : null;
            return playable;
        }
    }

    public sealed class PrototypeTimelineBehaviour : PlayableBehaviour
    {
        public PrototypeTimelineSegment Segment;
        public float Duration;
        public PrototypeCutsceneView View;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            View?.BeginTimelineSegment(Segment);
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            float normalizedTime = Duration <= 0f
                ? 1f
                : Mathf.Clamp01((float)(playable.GetTime() / Duration));
            View?.ApplyTimelinePose(Segment, normalizedTime);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (playable.GetTime() >= Duration - 0.02f)
                View?.ApplyTimelinePose(Segment, 1f);
        }
    }
}
