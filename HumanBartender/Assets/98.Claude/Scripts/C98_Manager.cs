// 98.Claude 컷씬 프로토타입 — CutsceneManager(상태 머신·시퀀스 실행기) + MicroTimeline 실행기
//
// MicroTimeline은 Unity Timeline의 자리 표시자다: 같은 계약(t 기반 이벤트 배치,
// wait_mode=pause_timeline, fire_on_skip, event_key 중복 차단)을 코드로 재현해
// 데이터·실행 구조를 검증하는 것이 목적이며, 실전에서는 PlayableDirector 재생으로 교체된다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Claude98
{
    // Timeline 마커 알림 수신기 — 매니저와 같은 GameObject에 붙는다
    public class C98_MarkerReceiver : MonoBehaviour, INotificationReceiver
    {
        C98_CutsceneManager mgr;
        public void Init(C98_CutsceneManager m) => mgr = m;

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is C98_EventMarker m && mgr != null) mgr.OnMarkerNotify(m);
        }
    }

    public enum C98_State
    {
        Idle, Preparing, PlayingTimeline, ShowingDialogue, ShowingChoice,
        Transitioning, Skipping, Completing, Recovering
    }

    // 재생 중 런타임 상태만 보관 — 저장 데이터가 아니며 종료 시 폐기 (문서 규칙)
    public class C98_Context
    {
        public C98_Cutscene cutscene;
        public int step_index;
        public readonly HashSet<string> fired_event_keys = new HashSet<string>();
        public bool skip_requested;
        public C98_MicroTimelineRunner active_runner;
    }

    public class C98_CutsceneManager : MonoBehaviour
    {
        public C98_State State { get; private set; } = C98_State.Idle;
        public C98_Context Ctx { get; private set; }
        public bool InputLocked { get; private set; }
        public string LastLog { get; private set; } = "";

        readonly HashSet<string> completed = new HashSet<string>(); // 완료 플래그(세션 한정)

        C98_Bundle bundle;
        C98_BindingRegistry reg;
        C98_DialogueUI ui;
        C98_CameraDirector camDir;
        C98_ScreenFx fx;
        C98_AudioStub audioFx;
        bool completionFired; // 종료 이벤트 1회 보장

        PlayableDirector director;
        TimelineAsset activeAsset;   // 마커 event_key 생성용
        bool pausedForDialogue;      // Timeline 대사 일시정지 상태

        public void Init(C98_Bundle b, C98_BindingRegistry r, C98_DialogueUI u,
            C98_CameraDirector c, C98_ScreenFx f, C98_AudioStub a)
        {
            bundle = b; reg = r; ui = u; camDir = c; fx = f; audioFx = a;
        }

        void SetState(C98_State s)
        {
            State = s;
            Log($"상태 → {s}");
        }

        void Log(string msg)
        {
            LastLog = msg;
            Debug.Log($"[C98] {msg}");
        }

        public bool IsCompleted(string cutsceneId) => completed.Contains(cutsceneId);

        public void ResetProgress()
        {
            completed.Clear();
            C98_Flags.Clear();
            Log("진행 상태 초기화 (완료 기록·플래그)");
        }

        void Update()
        {
            // Skip 요청 — 선택지 표시 중에는 차단 (필수 선택 이전 Skip 금지)
            if (Input.GetKeyDown(KeyCode.S) && State != C98_State.Idle)
            {
                if (State == C98_State.ShowingChoice)
                    ui.Toast(C98_Loc.English ? "Cannot skip before a required choice." : "선택 전에는 스킵할 수 없다.");
                else if (Ctx != null && !Ctx.skip_requested)
                {
                    Ctx.skip_requested = true;
                    Log("Skip 요청");
                }
            }
        }

        // ───────────────────────── 진입 ─────────────────────────

        public void RequestSkip()
        {
            if (Ctx == null || State == C98_State.Idle) return;
            if (State == C98_State.ShowingChoice)
            {
                ui.Toast(C98_Loc.English ? "Cannot skip before a required choice." : "선택 전에는 스킵할 수 없다.");
                return;
            }
            Ctx.skip_requested = true;
        }

        public bool Play(string cutsceneId)
        {
            if (State != C98_State.Idle) { Log($"재생 거부 — 이미 실행 중 ({State})"); return false; }
            var cs = bundle.Cutscene(cutsceneId);
            if (cs == null) { Log($"재생 거부 — cutscene_id '{cutsceneId}' 없음"); return false; }
            if (cs.play_mode == "once" && completed.Contains(cs.id))
            {
                ui.Toast(C98_Loc.English ? "Already played (play_mode=once). Press R to reset." : "이미 재생된 컷씬 (play_mode=once). R로 초기화.");
                Log($"재생 거부 — {cs.id}는 once이고 완료됨");
                return false;
            }
            foreach (var f in cs.required_flags)
                if (!C98_Flags.Get(f)) { Log($"재생 거부 — required_flag '{f}' 미충족"); return false; }

            StartCoroutine(PlayCo(cs));
            return true;
        }

        IEnumerator PlayCo(C98_Cutscene cs)
        {
            Ctx = new C98_Context { cutscene = cs };
            completionFired = false;
            SetState(C98_State.Preparing);

            // 필수 바인딩 검증 — 누락 시 재생 중단 (개발 기준)
            var missing = CollectMissingBindings(cs);
            if (missing.Count > 0)
            {
                SetState(C98_State.Recovering);
                foreach (var m in missing) Debug.LogError($"[C98] 필수 바인딩 누락: {m} (cutscene={cs.id})");
                ui.Toast("DATA_ERROR — 바인딩 누락 (콘솔 확인)");
                yield return Finish(cs, applyEndState: false);
                yield break;
            }

            InputLocked = true;
            camDir.SaveState();   // temporary_state
            ui.HideIdle();

            // 시네마틱 진입 연출 — 레터박스가 위아래에서 차오르며 카메라 살짝 줌인
            fx.ShowLetterbox(0.9f);
            camDir.ZoomByFactor(0.93f, 1.0f);

            // ── 시퀀스 실행 ──
            var stepById = new Dictionary<string, int>();
            for (int i = 0; i < cs.sequence.Count; i++)
                if (!string.IsNullOrEmpty(cs.sequence[i].id)) stepById[cs.sequence[i].id] = i;

            int idx = 0;
            while (idx >= 0 && idx < cs.sequence.Count && !Ctx.skip_requested)
            {
                var st = cs.sequence[idx];
                Ctx.step_index = idx;
                string jump = null;

                switch (st.type)
                {
                    case "timeline":
                    {
                        SetState(C98_State.PlayingTimeline);
                        // 같은 키의 Unity Timeline 에셋(Resources/Claude98/Timelines)이 있으면 우선 재생,
                        // 없으면 JSON MicroTimeline으로 폴백
                        var asset = LoadTimelineAsset(st.timeline_key);
                        if (asset != null)
                        {
                            yield return RunTimelineAsset(asset);
                        }
                        else
                        {
                            var tl = bundle.Timeline(st.timeline_key);
                            if (tl == null)
                            {
                                Debug.LogError($"[C98] 타임라인 없음(에셋·JSON 모두): {st.timeline_key}");
                                break;
                            }
                            var runner = new C98_MicroTimelineRunner(tl, this);
                            Ctx.active_runner = runner;
                            yield return runner.Run();
                            Ctx.active_runner = null;
                        }
                        break;
                    }
                    case "dialogue":
                    {
                        SetState(C98_State.ShowingDialogue);
                        var scene = bundle.DialogueScene(st.dialogue_scene);
                        yield return ui.RunDialogueScene(scene, () => Ctx.skip_requested);
                        break;
                    }
                    case "choice":
                    {
                        SetState(C98_State.ShowingChoice);
                        var choice = bundle.Choice(st.choice_id);
                        yield return ui.ShowChoice(choice);
                        var op = choice.options[ui.SelectedOption];
                        foreach (var f in op.set_flags) C98_Flags.Set(f);
                        if (!string.IsNullOrEmpty(op.goto_step)) jump = op.goto_step;
                        Log($"선택: {C98_Loc.Pick(op.text_ko, op.text_en)}");
                        break;
                    }
                    case "command":
                        ExecuteCommand(st);
                        break;
                    case "wait":
                    {
                        float t = 0;
                        while (t < st.seconds && !Ctx.skip_requested) { t += Time.deltaTime; yield return null; }
                        break;
                    }
                    case "branch":
                        jump = C98_Flags.Eval(st.when) ? st.true_next : st.false_next;
                        Log($"branch {st.when} → {jump}");
                        break;
                }

                if (jump == null && !string.IsNullOrEmpty(st.next)) jump = st.next;
                if (Ctx.skip_requested) break;
                idx = jump != null ? stepById[jump] : idx + 1;
            }

            if (Ctx.skip_requested)
            {
                SetState(C98_State.Skipping);
                ui.HideMainBubble();
                ui.ClearBarks();
                fx.ClearTransients();
                camDir.StopAll();
                FirePendingSkipEvents(cs, Ctx.step_index);
            }

            yield return Finish(cs, applyEndState: true);
        }

        IEnumerator Finish(C98_Cutscene cs, bool applyEndState)
        {
            SetState(C98_State.Transitioning);
            if (applyEndState) ApplyEndState(cs);

            // 시네마틱 종료 연출 — 레터박스 걷힘 + 카메라 복귀를 보여준 뒤 확정
            fx.HideLetterbox(0.5f);
            camDir.RestoreSmooth(0.5f);
            float t = 0;
            while (t < 0.55f) { t += Time.deltaTime; yield return null; }

            SetState(C98_State.Completing);
            camDir.RestoreState();
            InputLocked = false;
            if (!completionFired)
            {
                completionFired = true; // 종료 이벤트는 한 번만
                if (applyEndState)
                {
                    completed.Add(cs.id);
                    Log($"컷씬 완료: {cs.id}" + (string.IsNullOrEmpty(cs.next) ? " (next 없음)" : $" → next: {cs.next}"));
                }
            }
            Ctx = null;
            SetState(C98_State.Idle);
        }

        // ───────────────────────── end_state ─────────────────────────

        // 정상 종료·Skip 어느 쪽에서도 같은 최종 상태를 만든다 (문서 핵심 규칙)
        void ApplyEndState(C98_Cutscene cs)
        {
            var es = cs.end_state;
            foreach (var a in es.actor_states)
            {
                var actor = reg.Get<C98_Actor>(a.actor);
                if (actor == null) continue;
                actor.StopMove();
                var anchor = reg.Get<C98_Anchor>(a.anchor);
                if (anchor != null) actor.TeleportTo(anchor);
                actor.SetVisible(a.visible);
                actor.SetFacing(a.facing);
            }
            if (!string.IsNullOrEmpty(es.light_preset)) fx.SetLightPreset(es.light_preset);
            audioFx.SetBgm(es.bgm_id);
            foreach (var f in es.set_flags) C98_Flags.Set(f);
        }

        // ───────────────────────── Unity Timeline 에셋 재생 ─────────────────────────

        public static TimelineAsset LoadTimelineAsset(string key)
            => Resources.Load<TimelineAsset>("Claude98/Timelines/" + key);

        PlayableDirector EnsureDirector()
        {
            if (director == null)
            {
                director = GetComponent<PlayableDirector>();
                if (director == null) director = gameObject.AddComponent<PlayableDirector>();
                director.playOnAwake = false;
                director.extrapolationMode = DirectorWrapMode.None;
                var recv = GetComponent<C98_MarkerReceiver>();
                if (recv == null) recv = gameObject.AddComponent<C98_MarkerReceiver>();
                recv.Init(this);
            }
            return director;
        }

        IEnumerator RunTimelineAsset(TimelineAsset asset)
        {
            EnsureDirector();
            activeAsset = asset;
            director.playableAsset = asset;
            // 마커 트랙 알림이 이 GO의 수신기로 오도록 바인딩 (문서의 Binding Resolver 자리)
            foreach (var track in asset.GetOutputTracks())
                if (track is MarkerTrack) director.SetGenericBinding(track, gameObject);
            director.time = 0;
            director.RebuildGraph();
            director.Play();
            Log($"Timeline 에셋 재생: {asset.name} ({asset.duration:0.0}s)");

            while (true)
            {
                if (Ctx == null) break;
                if (Ctx.skip_requested) { director.Stop(); break; }
                if (pausedForDialogue) { yield return null; continue; }
                if (director.state != PlayState.Playing) break;
                yield return null;
            }

            pausedForDialogue = false;
            activeAsset = null;
            director.playableAsset = null;
        }

        // 마커 알림 — event_key로 중복 실행 차단 후 실행 (문서의 CutsceneEventMarker 계약)
        public void OnMarkerNotify(C98_EventMarker m)
        {
            if (Ctx == null || activeAsset == null) return;
            string key = m.ResolvedKey(activeAsset);
            if (!Ctx.fired_event_keys.Add(key)) return;
            if (Ctx.skip_requested) return;
            ExecuteMarker(m, duringSkip: false);
        }

        void ExecuteMarker(C98_EventMarker m, bool duringSkip)
        {
            switch (m.eventType)
            {
                case C98_MarkerType.Sfx: if (!duringSkip) audioFx.PlaySfx(m.payload); break;
                case C98_MarkerType.ScreenFlash: if (!duringSkip) fx.Flash(m.duration); break;
                case C98_MarkerType.FadeFromBlack: if (!duringSkip) fx.FadeFromBlack(m.duration); break;
                case C98_MarkerType.FadeToBlack: if (!duringSkip) fx.FadeToBlack(m.duration); break;
                case C98_MarkerType.CameraShake: if (!duringSkip) camDir.Shake(m.value, m.duration); break;
                case C98_MarkerType.ActorShow: reg.Get<C98_Actor>(m.targetId)?.SetVisible(true); break;
                case C98_MarkerType.ActorHide: reg.Get<C98_Actor>(m.targetId)?.SetVisible(false); break;
                case C98_MarkerType.ActorExpression:
                {
                    var a = reg.Get<C98_Actor>(m.targetId);
                    if (a != null && !duringSkip) a.SetExpression(m.payload, this);
                    break;
                }
                case C98_MarkerType.Bark:
                {
                    if (duringSkip) break;
                    var sc = bundle.DialogueScene(m.payload);
                    if (sc != null) StartCoroutine(ui.RunDialogueScene(sc, () => Ctx != null && Ctx.skip_requested));
                    else Debug.LogWarning($"[C98] 마커 bark 대사 씬 없음: {m.payload}");
                    break;
                }
                case C98_MarkerType.Dialogue:
                    if (!duringSkip) StartCoroutine(MarkerDialogueCo(m.payload, m.pauseTimeline));
                    break;
            }
        }

        void SetDirectorSpeed(double speed)
        {
            if (director != null && director.playableGraph.IsValid())
                director.playableGraph.GetRootPlayable(0).SetSpeed(speed);
        }

        IEnumerator MarkerDialogueCo(string sceneId, bool pause)
        {
            var scene = bundle.DialogueScene(sceneId);
            if (scene == null) { Debug.LogError($"[C98] 마커 대사 씬 없음: {sceneId}"); yield break; }
            if (!pause)
            {
                StartCoroutine(ui.RunDialogueScene(scene, () => Ctx != null && Ctx.skip_requested));
                yield break;
            }
            // 대사 완료까지 Timeline 클록 정지 — 완료 후 같은 지점에서 재개
            pausedForDialogue = true;
            SetDirectorSpeed(0);
            var prev = State;
            SetState(C98_State.ShowingDialogue);
            yield return ui.RunDialogueScene(scene, () => Ctx != null && Ctx.skip_requested);
            SetState(prev);
            SetDirectorSpeed(1);
            pausedForDialogue = false;
        }

        // 에디터에서 만든 타임라인을 단독 컷씬으로 재생 — 편집→테스트 루프용 (T키)
        public bool PlayAdhocTimeline(string key)
        {
            if (State != C98_State.Idle) return false;
            var cs = new C98_Cutscene
            {
                id = "adhoc_" + key,
                scene_id = "stage_lab",
                play_mode = "repeatable",
                sequence = new List<C98_SeqStep> { new C98_SeqStep { id = "st_tl", type = "timeline", timeline_key = key } },
                end_state = new C98_EndState { light_preset = "light_default", bgm_id = "" },
            };
            StartCoroutine(PlayCo(cs));
            return true;
        }

        static IEnumerable<C98_EventMarker> AllMarkers(TimelineAsset asset)
        {
            var seen = new HashSet<IMarker>();
            if (asset.markerTrack != null)
                foreach (var m in asset.markerTrack.GetMarkers())
                    if (m is C98_EventMarker em && seen.Add(m)) yield return em;
            foreach (var tr in asset.GetOutputTracks())
                foreach (var m in tr.GetMarkers())
                    if (m is C98_EventMarker em && seen.Add(m)) yield return em;
        }

        // ───────────────────────── Skip 필수 이벤트 ─────────────────────────

        // 현재 스텝 이후(현재 타임라인의 미발화 포함) fire_on_skip 이벤트만 실행
        void FirePendingSkipEvents(C98_Cutscene cs, int fromStep)
        {
            for (int i = fromStep; i < cs.sequence.Count; i++)
            {
                var st = cs.sequence[i];
                if (st.type != "timeline") continue;

                // Unity Timeline 에셋 — fire_on_skip 마커만 실행
                var asset = LoadTimelineAsset(st.timeline_key);
                if (asset != null)
                {
                    foreach (var m in AllMarkers(asset))
                    {
                        if (!m.fireOnSkip) continue;
                        if (m.eventType == C98_MarkerType.Dialogue || m.eventType == C98_MarkerType.Bark) continue;
                        string mkey = m.ResolvedKey(asset);
                        if (!Ctx.fired_event_keys.Add(mkey)) continue;
                        ExecuteMarker(m, duringSkip: true);
                    }
                    continue;
                }

                var tl = bundle.Timeline(st.timeline_key);
                if (tl == null) continue;
                for (int e = 0; e < tl.events.Count; e++)
                {
                    var ev = tl.events[e];
                    if (!ev.fire_on_skip) continue;
                    if (ev.event_type == "dialogue" || ev.event_type == "bark") continue; // 대사는 스킵 대상
                    string key = EventKey(tl, ev, e);
                    if (Ctx.fired_event_keys.Contains(key)) continue;
                    Ctx.fired_event_keys.Add(key);
                    ExecuteEvent(ev, duringSkip: true);
                }
            }
        }

        // ───────────────────────── 이벤트·커맨드 실행 ─────────────────────────

        public static string EventKey(C98_Timeline tl, C98_TlEvent ev, int index)
            => string.IsNullOrEmpty(ev.event_key) ? $"{tl.key}#{index}" : ev.event_key;

        public bool TryMarkFired(C98_Timeline tl, C98_TlEvent ev, int index)
        {
            string key = EventKey(tl, ev, index);
            if (Ctx.fired_event_keys.Contains(key)) return false; // 중복 평가 차단
            Ctx.fired_event_keys.Add(key);
            return true;
        }

        // object_move payload 프리셋 — 오브젝트 최종 상태가 필요하면 end_state에 기록해야 한다
        static readonly Dictionary<string, Vector2> ObjectMoves = new Dictionary<string, Vector2>
        {
            { "door_open", new Vector2(0, 2.7f) },
            { "door_close", new Vector2(0, -2.7f) },
            { "cover_fall", new Vector2(0.35f, -0.9f) },
        };

        public void ExecuteEvent(C98_TlEvent ev, bool duringSkip = false)
        {
            switch (ev.event_type)
            {
                case "light_preset": fx.SetLightPreset(ev.payload_id); break;
                case "sfx": if (!duringSkip) audioFx.PlaySfx(ev.payload_id); break;
                case "camera_move":
                {
                    var anchor = reg.Get<C98_Anchor>(ev.payload_id);
                    if (anchor != null && !duringSkip) camDir.MoveTo(anchor.transform.position, ev.duration);
                    break;
                }
                case "camera_zoom": if (!duringSkip) camDir.ZoomTo(ev.value, ev.duration); break;
                case "camera_shake": if (!duringSkip) camDir.Shake(ev.value, ev.duration); break;
                case "actor_move":
                {
                    var actor = reg.Get<C98_Actor>(ev.target_id);
                    var anchor = reg.Get<C98_Anchor>(ev.payload_id);
                    if (actor == null || anchor == null) break;
                    if (duringSkip) actor.TeleportTo(anchor);
                    else actor.MoveTo(anchor, ev.duration, this);
                    break;
                }
                case "actor_show": reg.Get<C98_Actor>(ev.target_id)?.SetVisible(true); break;
                case "actor_hide": reg.Get<C98_Actor>(ev.target_id)?.SetVisible(false); break;
                case "actor_face": reg.Get<C98_Actor>(ev.target_id)?.SetFacing(ev.payload_id); break;
                case "actor_expression":
                {
                    var actor = reg.Get<C98_Actor>(ev.target_id);
                    if (actor != null && !duringSkip) actor.SetExpression(ev.payload_id, this);
                    break;
                }
                case "object_move":
                {
                    var obj = reg.Get<C98_SceneObject>(ev.target_id);
                    if (obj == null) break;
                    if (!ObjectMoves.TryGetValue(ev.payload_id, out var offset))
                    { Debug.LogWarning($"[C98] 알 수 없는 object_move payload: {ev.payload_id}"); break; }
                    if (duringSkip) obj.SlideImmediate(offset);
                    else obj.Slide(offset, Mathf.Max(0.1f, ev.duration), this);
                    break;
                }
                case "screen_fade":
                    if (duringSkip) break;
                    if (ev.payload_id == "from_black") fx.FadeFromBlack(ev.duration);
                    else fx.FadeToBlack(ev.duration);
                    break;
                case "screen_flash": if (!duringSkip) fx.Flash(ev.duration); break;
                case "bark":
                {
                    var scene = bundle.DialogueScene(ev.dialogue_scene);
                    if (scene != null && !duringSkip)
                        StartCoroutine(ui.RunDialogueScene(scene, () => Ctx != null && Ctx.skip_requested));
                    break;
                }
                // dialogue(wait_mode=pause_timeline)는 러너가 직접 처리
            }
        }

        public IEnumerator RunPausedDialogue(string sceneId)
        {
            var prev = State;
            SetState(C98_State.ShowingDialogue);
            var scene = bundle.DialogueScene(sceneId);
            yield return ui.RunDialogueScene(scene, () => Ctx != null && Ctx.skip_requested);
            SetState(prev);
        }

        void ExecuteCommand(C98_SeqStep st)
        {
            ExecuteEvent(new C98_TlEvent
            {
                event_type = st.command,
                target_id = st.target_id,
                payload_id = st.payload_id,
                duration = st.seconds,
                value = st.seconds
            });
        }

        // ───────────────────────── 바인딩 스캔 ─────────────────────────

        List<string> CollectMissingBindings(C98_Cutscene cs)
        {
            var required = new HashSet<string>();
            void Need(string id) { if (!string.IsNullOrEmpty(id)) required.Add(id); }

            foreach (var st in cs.sequence)
            {
                if (st.type == "timeline")
                {
                    // Unity Timeline 에셋 스텝은 트랙이 스스로 대상을 해석·경고한다 — JSON만 사전 스캔
                    if (LoadTimelineAsset(st.timeline_key) != null) continue;
                    var tl = bundle.Timeline(st.timeline_key);
                    if (tl == null) continue;
                    foreach (var ev in tl.events)
                    {
                        if (ev.event_type.StartsWith("actor_") || ev.event_type == "object_move") Need(ev.target_id);
                        if (ev.event_type == "actor_move" || ev.event_type == "camera_move") Need(ev.payload_id);
                    }
                }
                if (st.type == "command") Need(st.target_id);
            }
            foreach (var es in cs.end_state.actor_states) { Need(es.actor); Need(es.anchor); }

            var missing = new List<string>();
            foreach (var id in required)
                if (!reg.Has(id)) missing.Add(id);
            return missing;
        }
    }

    // ───────────────────────── MicroTimeline 실행기 ─────────────────────────

    public class C98_MicroTimelineRunner
    {
        readonly C98_Timeline tl;
        readonly C98_CutsceneManager mgr;
        public float Clock { get; private set; }
        public bool Paused { get; private set; }

        public C98_MicroTimelineRunner(C98_Timeline tl, C98_CutsceneManager mgr)
        {
            this.tl = tl; this.mgr = mgr;
        }

        public string Key => tl.key;

        public IEnumerator Run()
        {
            Clock = 0;
            // t 오름차순으로 평가 — 같은 t는 데이터 순서 유지
            var order = new List<int>();
            for (int i = 0; i < tl.events.Count; i++) order.Add(i);
            order.Sort((a, b) =>
            {
                int c = tl.events[a].t.CompareTo(tl.events[b].t);
                return c != 0 ? c : a.CompareTo(b);
            });
            int cursor = 0;

            while (Clock < tl.duration)
            {
                if (mgr.Ctx == null || mgr.Ctx.skip_requested) yield break;

                while (cursor < order.Count && tl.events[order[cursor]].t <= Clock)
                {
                    int idx = order[cursor];
                    var ev = tl.events[idx];
                    cursor++;
                    if (!mgr.TryMarkFired(tl, ev, idx)) continue; // event_key 중복 차단

                    if (ev.event_type == "dialogue" && ev.wait_mode == "pause_timeline")
                    {
                        // 대사 완료까지 클록 정지 — 완료 후 같은 지점에서 재개
                        Paused = true;
                        yield return mgr.RunPausedDialogue(ev.dialogue_scene);
                        Paused = false;
                        if (mgr.Ctx == null || mgr.Ctx.skip_requested) yield break;
                    }
                    else
                    {
                        mgr.ExecuteEvent(ev);
                    }
                }

                Clock += Time.deltaTime;
                yield return null;
            }
        }
    }
}
