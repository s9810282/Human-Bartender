// 98.Claude 컷씬 프로토타입 — 데이터 모델 + 로더 + 검증기
// 이 폴더 밖의 어떤 코드/에셋도 참조하지 않는다 (독립 샌드박스).
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Claude98
{
    // ───────────────────────── 데이터 모델 (JsonUtility 직렬화) ─────────────────────────

    [Serializable]
    public class C98_Bundle
    {
        public int schema_version;
        public List<C98_Character> characters = new List<C98_Character>();
        public List<C98_Timeline> timelines = new List<C98_Timeline>();
        public List<C98_DialogueScene> dialogue_scenes = new List<C98_DialogueScene>();
        public List<C98_Choice> choices = new List<C98_Choice>();
        public List<C98_Cutscene> cutscenes = new List<C98_Cutscene>();

        public C98_Character Character(string id) => characters.Find(c => c.id == id);
        public C98_Timeline Timeline(string key) => timelines.Find(t => t.key == key);
        public C98_DialogueScene DialogueScene(string id) => dialogue_scenes.Find(d => d.id == id);
        public C98_Choice Choice(string id) => choices.Find(c => c.id == id);
        public C98_Cutscene Cutscene(string id) => cutscenes.Find(c => c.id == id);
    }

    [Serializable]
    public class C98_Character
    {
        public string id;
        public string name_ko;
        public string name_en;
        public string body_color = "#888888";
        public string name_color = "#ffffff";
    }

    [Serializable]
    public class C98_Timeline
    {
        public string key;
        public float duration;
        public List<C98_TlEvent> events = new List<C98_TlEvent>();
    }

    // MicroTimeline 이벤트 — 실전에서는 Unity Timeline의 CutsceneEventMarker에 해당하는 계약.
    [Serializable]
    public class C98_TlEvent
    {
        public float t;
        public string event_type;      // light_preset / sfx / camera_move / camera_zoom / camera_shake /
                                       // actor_move / actor_show / actor_hide / actor_face / actor_expression /
                                       // object_move / screen_fade / screen_flash / bark / dialogue
        public string event_key;       // 중복 실행 차단용 — 공란이면 "키+인덱스"로 자동 생성
        public string target_id;       // actor / object / anchor / camera 대상
        public string payload_id;      // 프리셋 · 앵커 · 대사 씬 · sfx id
        public string dialogue_scene;  // bark / dialogue 전용
        public string wait_mode = "none"; // none / pause_timeline (dialogue 이벤트 전용)
        public bool fire_on_skip;      // 스킵 중에도 반드시 실행
        public float duration;         // 이동·페이드 등 지속 시간
        public float value;            // 줌 크기 · 흔들림 강도 등
    }

    [Serializable]
    public class C98_DialogueScene
    {
        public string id;
        public List<C98_SayStep> steps = new List<C98_SayStep>();
    }

    [Serializable]
    public class C98_SayStep
    {
        public string actor;                 // 공란 = 무명(내레이션·사물)
        public string expression;            // neutral / alert / angry / fear / relief
        public string channel = "main";      // main / bark / narration
        public string bubble = "world";      // world / screen
        public string advance = "input";     // input / auto
        public float auto_delay = 1.4f;
        public string text_ko;
        public string text_en;
    }

    [Serializable]
    public class C98_Choice
    {
        public string id;
        public List<C98_Option> options = new List<C98_Option>();
    }

    [Serializable]
    public class C98_Option
    {
        public string text_ko;
        public string text_en;
        public string when;              // 미충족 시 회색 비활성 (숨기지 않는다 — 프로젝트 규칙)
        public string lock_reason_ko;    // when이 있으면 두 언어 모두 필수 (프로젝트 규칙)
        public string lock_reason_en;
        public string goto_step;         // 선택 후 이동할 시퀀스 스텝 id (공란 = 다음 스텝)
        public List<string> set_flags = new List<string>();
    }

    [Serializable]
    public class C98_SeqStep
    {
        public string id;
        public string type;              // timeline / dialogue / choice / command / wait / branch
        public string timeline_key;
        public string dialogue_scene;
        public string choice_id;
        public string when;              // branch 전용 조건
        public string true_next;
        public string false_next;
        public string next;              // 완료 후 점프 (공란 = 다음 스텝)
        public string command;           // command 전용: actor_expression / actor_face / sfx / light_preset
        public string target_id;
        public string payload_id;
        public float seconds;            // wait 전용
    }

    [Serializable]
    public class C98_ActorEndState
    {
        public string actor;
        public string anchor;
        public string facing = "right";
        public bool visible = true;
    }

    [Serializable]
    public class C98_EndState
    {
        public List<C98_ActorEndState> actor_states = new List<C98_ActorEndState>();
        public List<string> set_flags = new List<string>();
        public string light_preset;
        public string bgm_id;
    }

    [Serializable]
    public class C98_Cutscene
    {
        public string id;
        public string scene_id;
        public string play_mode = "once"; // once / repeatable
        public List<string> required_flags = new List<string>();
        public List<C98_SeqStep> sequence = new List<C98_SeqStep>();
        public C98_EndState end_state = new C98_EndState();
        public string next;
    }

    // ───────────────────────── 플래그 저장소 + when 평가 ─────────────────────────

    public static class C98_Flags
    {
        static readonly HashSet<string> set = new HashSet<string>();
        public static event Action Changed;

        public static void Set(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return;
            if (set.Add(flag)) { Debug.Log($"[C98] flag set: {flag}"); Changed?.Invoke(); }
        }

        public static bool Get(string flag) => set.Contains(flag);
        public static void Clear() { set.Clear(); Changed?.Invoke(); }
        public static IEnumerable<string> All() => set;

        // when 문법(프로토타입 축소판): "flag.x" / "!flag.x" — 실전은 when DSL 18계열 재사용.
        public static bool Eval(string when)
        {
            if (string.IsNullOrEmpty(when)) return true;
            var expr = when.Trim();
            bool negate = expr.StartsWith("!");
            if (negate) expr = expr.Substring(1).Trim();
            if (!expr.StartsWith("flag.")) { Debug.LogError($"[C98] when 문법 오류: {when}"); return false; }
            bool v = Get(expr.Substring("flag.".Length));
            return negate ? !v : v;
        }

        public static bool ValidSyntax(string when)
        {
            if (string.IsNullOrEmpty(when)) return true;
            var expr = when.Trim();
            if (expr.StartsWith("!")) expr = expr.Substring(1).Trim();
            return expr.StartsWith("flag.") && expr.Length > "flag.".Length;
        }
    }

    // ───────────────────────── 언어 ─────────────────────────

    public static class C98_Loc
    {
        public static bool English;
        public static string Pick(string ko, string en) => English && !string.IsNullOrEmpty(en) ? en : ko;
    }

    // ───────────────────────── 로더 + 검증기 ─────────────────────────

    public static class C98_Loader
    {
        public const string ResourcePath = "Claude98/cutscene_lab";

        public static C98_Bundle Load(TextAsset overrideAsset, out List<string> errors)
        {
            errors = new List<string>();
            TextAsset ta = overrideAsset != null ? overrideAsset : Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                errors.Add($"데이터 파일 없음: Resources/{ResourcePath}.json");
                return null;
            }
            C98_Bundle b;
            try { b = JsonUtility.FromJson<C98_Bundle>(ta.text); }
            catch (Exception e) { errors.Add($"JSON 파싱 실패: {e.Message}"); return null; }
            if (b == null) { errors.Add("JSON 파싱 결과가 비어 있음"); return null; }
            Validate(b, errors);
            return b;
        }

        static readonly HashSet<string> StepTypes = new HashSet<string>
            { "timeline", "dialogue", "choice", "command", "wait", "branch" };
        static readonly HashSet<string> EventTypes = new HashSet<string>
            { "light_preset", "sfx", "camera_move", "camera_zoom", "camera_shake",
              "actor_move", "actor_show", "actor_hide", "actor_face", "actor_expression",
              "object_move", "screen_fade", "screen_flash", "bark", "dialogue" };
        static readonly HashSet<string> Commands = new HashSet<string>
            { "actor_expression", "actor_face", "sfx", "light_preset", "screen_fade", "screen_flash" };

        // 검증 실패 시 재생을 거부한다 — build.py의 "검사 실패 시 json 미출력"과 같은 태도.
        public static void Validate(C98_Bundle b, List<string> errors)
        {
            var charIds = new HashSet<string>();
            foreach (var c in b.characters)
                if (!charIds.Add(c.id)) errors.Add($"[characters] 중복 id: {c.id}");

            var tlKeys = new HashSet<string>();
            foreach (var tl in b.timelines)
            {
                if (!tlKeys.Add(tl.key)) errors.Add($"[timelines] 중복 key: {tl.key}");
                if (tl.duration <= 0) errors.Add($"[timelines] {tl.key}: duration은 0보다 커야 함");
                var evKeys = new HashSet<string>();
                for (int i = 0; i < tl.events.Count; i++)
                {
                    var ev = tl.events[i];
                    string loc = $"[timelines] {tl.key} #{i}";
                    if (!EventTypes.Contains(ev.event_type)) errors.Add($"{loc}: 알 수 없는 event_type '{ev.event_type}'");
                    if (!string.IsNullOrEmpty(ev.event_key) && !evKeys.Add(ev.event_key))
                        errors.Add($"{loc}: 중복 event_key '{ev.event_key}'");
                    if (ev.event_type == "dialogue" && ev.wait_mode != "pause_timeline" && ev.wait_mode != "none")
                        errors.Add($"{loc}: dialogue의 wait_mode는 pause_timeline/none만 허용");
                    if ((ev.event_type == "dialogue" || ev.event_type == "bark") &&
                        b.DialogueScene(ev.dialogue_scene) == null)
                        errors.Add($"{loc}: dialogue_scene '{ev.dialogue_scene}' 없음");
                    if ((ev.event_type.StartsWith("actor_") || ev.event_type == "object_move") &&
                        string.IsNullOrEmpty(ev.target_id))
                        errors.Add($"{loc}: {ev.event_type}는 target_id 필수");
                    if (ev.event_type == "actor_move" && string.IsNullOrEmpty(ev.payload_id))
                        errors.Add($"{loc}: actor_move는 payload_id(앵커) 필수");
                    if (ev.t < 0 || ev.t > tl.duration)
                        errors.Add($"{loc}: t={ev.t}가 duration({tl.duration}) 범위 밖");
                }
            }

            var sceneIds = new HashSet<string>();
            foreach (var ds in b.dialogue_scenes)
            {
                if (!sceneIds.Add(ds.id)) errors.Add($"[dialogue_scenes] 중복 id: {ds.id}");
                for (int i = 0; i < ds.steps.Count; i++)
                {
                    var st = ds.steps[i];
                    string loc = $"[dialogue_scenes] {ds.id} #{i}";
                    if (!string.IsNullOrEmpty(st.actor) && b.Character(st.actor) == null)
                        errors.Add($"{loc}: 화자 '{st.actor}'가 characters에 없음");
                    if (string.IsNullOrEmpty(st.text_ko)) errors.Add($"{loc}: text_ko 누락");
                    if (string.IsNullOrEmpty(st.text_en)) errors.Add($"{loc}: text_en 누락 (L10N 필수)");
                    if (st.channel != "main" && st.channel != "bark" && st.channel != "narration")
                        errors.Add($"{loc}: channel '{st.channel}' 불허");
                    if (st.advance != "input" && st.advance != "auto")
                        errors.Add($"{loc}: advance '{st.advance}' 불허");
                }
            }

            var choiceIds = new HashSet<string>();
            foreach (var ch in b.choices)
            {
                if (!choiceIds.Add(ch.id)) errors.Add($"[choices] 중복 id: {ch.id}");
                if (ch.options.Count < 2 || ch.options.Count > 4)
                    errors.Add($"[choices] {ch.id}: 선택지는 2~4개 (현재 {ch.options.Count})");
                bool hasUnconditional = false;
                for (int i = 0; i < ch.options.Count; i++)
                {
                    var op = ch.options[i];
                    string loc = $"[choices] {ch.id} #{i}";
                    if (string.IsNullOrEmpty(op.when)) hasUnconditional = true;
                    else
                    {
                        // when ↔ lock_reason 상호 필수 — 본 프로젝트 Choices 규칙과 동일.
                        if (string.IsNullOrEmpty(op.lock_reason_ko) || string.IsNullOrEmpty(op.lock_reason_en))
                            errors.Add($"{loc}: when이 있으면 lock_reason_ko/en 둘 다 필수");
                        if (!C98_Flags.ValidSyntax(op.when)) errors.Add($"{loc}: when 문법 오류 '{op.when}'");
                    }
                    if (string.IsNullOrEmpty(op.text_ko) || string.IsNullOrEmpty(op.text_en))
                        errors.Add($"{loc}: text_ko/en 필수");
                }
                if (!hasUnconditional) errors.Add($"[choices] {ch.id}: when 없는 항목이 최소 1개 필요");
            }

            var csIds = new HashSet<string>();
            foreach (var cs in b.cutscenes)
            {
                if (!csIds.Add(cs.id)) errors.Add($"[cutscenes] 중복 id: {cs.id}");
                if (cs.play_mode != "once" && cs.play_mode != "repeatable")
                    errors.Add($"[cutscenes] {cs.id}: play_mode '{cs.play_mode}' 불허");
                if (cs.sequence.Count == 0) errors.Add($"[cutscenes] {cs.id}: sequence 비어 있음");

                var stepIds = new HashSet<string>();
                foreach (var st in cs.sequence)
                    if (!string.IsNullOrEmpty(st.id) && !stepIds.Add(st.id))
                        errors.Add($"[cutscenes] {cs.id}: 중복 스텝 id '{st.id}'");

                void CheckTarget(string loc, string target)
                {
                    if (!string.IsNullOrEmpty(target) && !stepIds.Contains(target))
                        errors.Add($"{loc}: 점프 대상 스텝 '{target}' 없음");
                }

                for (int i = 0; i < cs.sequence.Count; i++)
                {
                    var st = cs.sequence[i];
                    string loc = $"[cutscenes] {cs.id} #{i}({st.id})";
                    if (!StepTypes.Contains(st.type)) { errors.Add($"{loc}: 알 수 없는 type '{st.type}'"); continue; }
                    switch (st.type)
                    {
                        case "timeline":
                            if (b.Timeline(st.timeline_key) == null) errors.Add($"{loc}: timeline_key '{st.timeline_key}' 없음");
                            break;
                        case "dialogue":
                            if (b.DialogueScene(st.dialogue_scene) == null) errors.Add($"{loc}: dialogue_scene '{st.dialogue_scene}' 없음");
                            break;
                        case "choice":
                            var ch = b.Choice(st.choice_id);
                            if (ch == null) errors.Add($"{loc}: choice_id '{st.choice_id}' 없음");
                            else foreach (var op in ch.options) CheckTarget(loc, op.goto_step);
                            break;
                        case "branch":
                            if (!C98_Flags.ValidSyntax(st.when) || string.IsNullOrEmpty(st.when))
                                errors.Add($"{loc}: branch when 필수/문법 오류 '{st.when}'");
                            if (string.IsNullOrEmpty(st.true_next) || string.IsNullOrEmpty(st.false_next))
                                errors.Add($"{loc}: branch는 true_next/false_next 필수");
                            CheckTarget(loc, st.true_next);
                            CheckTarget(loc, st.false_next);
                            break;
                        case "command":
                            if (!Commands.Contains(st.command)) errors.Add($"{loc}: 알 수 없는 command '{st.command}'");
                            break;
                        case "wait":
                            if (st.seconds <= 0) errors.Add($"{loc}: wait seconds는 0보다 커야 함");
                            break;
                    }
                    CheckTarget(loc, st.next);
                }

                foreach (var es in cs.end_state.actor_states)
                    if (b.Character(es.actor) == null)
                        errors.Add($"[cutscenes] {cs.id} end_state: 배우 '{es.actor}'가 characters에 없음");
            }
        }
    }
}
