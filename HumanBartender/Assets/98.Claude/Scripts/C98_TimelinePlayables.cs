// 98.Claude 컷씬 프로토타입 — Unity Timeline 커스텀 트랙·클립·마커
//
// Timeline 창이 컷씬의 시각 편집기가 된다: 클립을 드래그해 배치·길이 조절하고,
// 스크럽 바를 긁으면 무대(씬에 배치된 것)가 그 시점 상태로 미리보기된다.
// 트랙은 바인딩 없이 C98_Id 표식을 스캔해 스스로 대상을 찾는다(에디터·런타임 공용).
// 대사·SFX·화면효과처럼 미리보기 불가능한 것은 마커(C98_EventMarker)로 런타임에만 실행한다.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Claude98
{
    // ───────────────────────── ID 해석 ─────────────────────────

    public static class C98_Resolve
    {
        static Dictionary<string, C98_Id> cache;

        static void Rebuild()
        {
            cache = new Dictionary<string, C98_Id>();
            foreach (var tag in Object.FindObjectsByType<C98_Id>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!string.IsNullOrEmpty(tag.id) && !cache.ContainsKey(tag.id))
                    cache[tag.id] = tag;
        }

        public static T Find<T>(string id) where T : Component
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (cache == null || !cache.TryGetValue(id, out var tag) || tag == null)
            {
                Rebuild();
                if (!cache.TryGetValue(id, out tag) || tag == null) return null;
            }
            return tag.GetComponent<T>();
        }

        public static Vector3? AnchorPos(string id)
        {
            var a = Find<C98_Anchor>(id);
            return a != null ? a.transform.position : (Vector3?)null;
        }

        public static Camera StageCamera()
        {
            var cam = Camera.main;
            if (cam == null) cam = Object.FindFirstObjectByType<Camera>();
            return cam;
        }

        public static void InvalidateCache() => cache = null;
    }

    // ───────────────────────── 배우 이동 ─────────────────────────

    // 클립 구간 동안 from 앵커 → to 앵커로 이동. from 공란 = 클립 시작 시점의 현재 위치
    // (스크럽을 앞뒤로 긁을 땐 from을 명시해야 미리보기가 정확하다 — README 참조).
    [TrackColor(0.55f, 0.85f, 1f)]
    [TrackClipType(typeof(C98_ActorMoveClip))]
    public class C98_ActorMoveTrack : TrackAsset { }

    public class C98_ActorMoveClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("이동할 배우 id (예: luna, hound)")] public string actorId = "luna";
        [Tooltip("출발 앵커 id — 공란이면 클립 시작 시점의 현재 위치")] public string fromAnchor = "";
        [Tooltip("도착 앵커 id (예: a_lab_center)")] public string toAnchor = "";
        [Tooltip("클립 시작 시 배우 표시")] public bool showOnStart;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var p = ScriptPlayable<C98_ActorMoveBehaviour>.Create(graph);
            var b = p.GetBehaviour();
            b.actorId = actorId; b.fromAnchor = fromAnchor; b.toAnchor = toAnchor; b.showOnStart = showOnStart;
            return p;
        }
    }

    public class C98_ActorMoveBehaviour : PlayableBehaviour
    {
        public string actorId, fromAnchor, toAnchor;
        public bool showOnStart;
        Vector3? capturedFrom;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var actor = C98_Resolve.Find<C98_Actor>(actorId);
            if (actor != null && showOnStart) actor.SetVisible(true);
            if (string.IsNullOrEmpty(fromAnchor) && actor != null)
                capturedFrom = actor.transform.position;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (info.weight <= 0f) return;
            var actor = C98_Resolve.Find<C98_Actor>(actorId);
            if (actor == null) return;
            Vector3? to = C98_Resolve.AnchorPos(toAnchor);
            if (to == null) return;
            Vector3 from = C98_Resolve.AnchorPos(fromAnchor) ?? capturedFrom ?? actor.transform.position;

            double dur = playable.GetDuration();
            float k = dur <= 0 ? 1f : Mathf.Clamp01((float)(playable.GetTime() / dur));
            k = Mathf.SmoothStep(0f, 1f, k);
            actor.transform.position = C98_Actor.Snap(Vector3.Lerp(from, to.Value, k));
            if (Application.isPlaying) actor.FaceToward(to.Value.x);
        }
    }

    // ───────────────────────── 카메라 ─────────────────────────

    // 클립 구간 동안 카메라를 앵커 위치·지정 줌으로 이동. from 공란 = 클립 시작 시점 상태.
    [TrackColor(1f, 0.8f, 0.4f)]
    [TrackClipType(typeof(C98_CameraClip))]
    public class C98_CameraTrack : TrackAsset { }

    public class C98_CameraClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("도착 카메라 앵커 id (예: cam_door)")] public string toAnchor = "cam_home";
        [Tooltip("도착 직교 크기(줌) — 0이면 유지")] public float toSize = 3.2f;
        [Tooltip("출발 앵커 id — 공란이면 클립 시작 시점 위치")] public string fromAnchor = "";

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var p = ScriptPlayable<C98_CameraBehaviour>.Create(graph);
            var b = p.GetBehaviour();
            b.toAnchor = toAnchor; b.toSize = toSize; b.fromAnchor = fromAnchor;
            return p;
        }
    }

    public class C98_CameraBehaviour : PlayableBehaviour
    {
        public string toAnchor, fromAnchor;
        public float toSize;
        Vector3? capturedPos;
        float capturedSize;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var cam = C98_Resolve.StageCamera();
            if (cam == null) return;
            capturedPos = cam.transform.position;
            capturedSize = cam.orthographicSize;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (info.weight <= 0f) return;
            var cam = C98_Resolve.StageCamera();
            if (cam == null) return;
            Vector3? to = C98_Resolve.AnchorPos(toAnchor);
            if (to == null) return;
            Vector3 from = C98_Resolve.AnchorPos(fromAnchor) ?? capturedPos ?? cam.transform.position;
            float fromSize = capturedSize > 0 ? capturedSize : cam.orthographicSize;

            double dur = playable.GetDuration();
            float k = dur <= 0 ? 1f : Mathf.Clamp01((float)(playable.GetTime() / dur));
            k = Mathf.SmoothStep(0f, 1f, k);
            var dest = new Vector3(to.Value.x, to.Value.y, cam.transform.position.z);
            var src = new Vector3(from.x, from.y, cam.transform.position.z);
            cam.transform.position = Vector3.Lerp(src, dest, k);
            if (toSize > 0) cam.orthographicSize = Mathf.Lerp(fromSize, toSize, k);
        }
    }

    // ───────────────────────── 오브젝트 슬라이드 ─────────────────────────

    // 클립 구간 동안 오브젝트를 offset만큼 밀어낸다 (문 열림·덮개 낙하).
    [TrackColor(0.7f, 0.95f, 0.6f)]
    [TrackClipType(typeof(C98_ObjectSlideClip))]
    public class C98_ObjectSlideTrack : TrackAsset { }

    public class C98_ObjectSlideClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("오브젝트 id (예: lab_door, vent_cover)")] public string objectId = "lab_door";
        [Tooltip("이동량 — 문 열림은 (0, 2.7)")] public Vector2 offset = new Vector2(0, 2.7f);

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var p = ScriptPlayable<C98_ObjectSlideBehaviour>.Create(graph);
            var b = p.GetBehaviour();
            b.objectId = objectId; b.offset = offset;
            return p;
        }
    }

    public class C98_ObjectSlideBehaviour : PlayableBehaviour
    {
        public string objectId;
        public Vector2 offset;
        Vector3? basePos;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            var obj = C98_Resolve.Find<C98_SceneObject>(objectId);
            if (obj != null) basePos = obj.transform.localPosition;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (info.weight <= 0f) return;
            var obj = C98_Resolve.Find<C98_SceneObject>(objectId);
            if (obj == null || basePos == null) return;
            double dur = playable.GetDuration();
            float k = dur <= 0 ? 1f : Mathf.Clamp01((float)(playable.GetTime() / dur));
            k = Mathf.SmoothStep(0f, 1f, k);
            obj.transform.localPosition = basePos.Value + (Vector3)(offset * k);
        }
    }

    // ───────────────────────── 조명 프리셋 ─────────────────────────

    // 클립이 걸린 구간 동안 조명 프리셋 적용 — 스크럽하면 색이 미리보기된다.
    [TrackColor(1f, 0.45f, 0.45f)]
    [TrackClipType(typeof(C98_LightPresetClip))]
    public class C98_LightTrack : TrackAsset { }

    public class C98_LightPresetClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("light_default / light_lab_alarm / light_blackout")]
        public string preset = "light_lab_alarm";

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var p = ScriptPlayable<C98_LightPresetBehaviour>.Create(graph);
            p.GetBehaviour().preset = preset;
            return p;
        }
    }

    public class C98_LightPresetBehaviour : PlayableBehaviour
    {
        public string preset;
        bool applied;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (info.weight <= 0f) { applied = false; return; }
            if (applied) return;
            applied = true;
            var quad = C98_LightQuad.Instance ?? Object.FindFirstObjectByType<C98_LightQuad>();
            if (quad != null) quad.Apply(preset);
        }
    }

    // ───────────────────────── 이벤트 마커 ─────────────────────────

    public enum C98_MarkerType
    {
        Sfx,            // payload = alarm / thud / ping / whoosh
        ScreenFlash,    // duration
        FadeFromBlack,  // duration
        FadeToBlack,    // duration
        CameraShake,    // value = 강도, duration
        Dialogue,       // payload = 대사 씬 id, pauseTimeline 옵션
        Bark,           // payload = 대사 씬 id (비차단)
        ActorShow,      // targetId
        ActorHide,      // targetId
        ActorExpression // targetId, payload = 표정
    }

    // 미리보기가 불가능한 이벤트(대사·SFX·화면효과)는 마커로 런타임에만 실행한다.
    // 문서의 CutsceneEventMarker 계약: wait_mode / fire_on_skip / event_key.
    public class C98_EventMarker : Marker, INotification, INotificationOptionProvider
    {
        public C98_MarkerType eventType = C98_MarkerType.Sfx;
        [Tooltip("대상 배우·오브젝트 id (Actor* 계열용)")] public string targetId = "";
        [Tooltip("sfx id · 대사 씬 id · 표정 이름")] public string payload = "";
        public float duration = 0.3f;
        [Tooltip("CameraShake 강도")] public float value = 0.15f;
        [Tooltip("Dialogue 전용 — 대사 완료까지 타임라인 일시정지")] public bool pauseTimeline = true;
        [Tooltip("스킵 중에도 반드시 실행")] public bool fireOnSkip;
        [Tooltip("중복 실행 차단 키 — 공란이면 자동 생성")] public string eventKey = "";

        PropertyName INotification.id => new PropertyName("C98_EventMarker");
        NotificationFlags INotificationOptionProvider.flags => NotificationFlags.TriggerOnce;

        public string ResolvedKey(TimelineAsset asset)
            => string.IsNullOrEmpty(eventKey) ? $"{asset.name}@{time:0.###}#{eventType}" : eventKey;
    }
}
