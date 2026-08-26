// 98.Claude 컷씬 프로토타입 — 씬 진입점
// 씬에는 이 컴포넌트 하나만 있으면 된다: 카메라·무대·UI·매니저를 전부 코드로 구성한다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;

namespace Claude98
{
    public class C98_Bootstrap : MonoBehaviour
    {
        [Tooltip("비워두면 Resources/Claude98/cutscene_lab.json 사용")]
        public TextAsset dataOverride;

        public const string EntryCutscene = "cs_lab_raid_01";

        C98_Bundle bundle;
        List<string> dataErrors;
        C98_BindingRegistry reg;
        C98_CutsceneManager mgr;
        C98_DialogueUI ui;
        C98_DebugPanel debug;
        C98_StageRoot stageRoot;
        bool picking; // T키 타임라인 선택 중

        void Start()
        {
            bundle = C98_Loader.Load(dataOverride, out dataErrors);
            if (bundle == null || dataErrors.Count > 0)
            {
                // 검증 실패 = 재생 거부 (DATA_ERROR) — build.py의 "검사 실패 시 미출력"과 같은 태도
                foreach (var e in dataErrors) Debug.LogError($"[C98] DATA_ERROR: {e}");
                return;
            }

            // 무대 — 에디터 메뉴로 씬에 배치해 뒀으면 그대로 사용(Timeline 미리보기와 동일 무대),
            // 없으면 런타임에 짓는다
            stageRoot = FindFirstObjectByType<C98_StageRoot>();
            if (stageRoot == null)
                stageRoot = C98_StageBuilder.BuildLab(transform, bundle).GetComponent<C98_StageRoot>();
            else
                Debug.Log("[C98] 씬에 배치된 편집용 무대 사용");

            C98_Resolve.InvalidateCache();
            reg = new C98_BindingRegistry();
            reg.BuildFromScene();
            if (reg.errors.Count > 0)
            {
                foreach (var e in reg.errors) Debug.LogError($"[C98] 바인딩 오류: {e}");
                dataErrors.AddRange(reg.errors);
                return;
            }

            // 카메라 — 무대와 함께 배치된 것이 있으면 채택, 없으면 생성
            var existingCam = FindFirstObjectByType<Camera>();
            var camDir = existingCam != null ? C98_CameraDirector.Adopt(existingCam) : C98_CameraDirector.Create(transform);
            var fx = C98_ScreenFx.Create(transform);
            var audio = C98_AudioStub.Create(transform);

            // uGUI 버튼 입력용 EventSystem — 씬에 없으면 생성
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("C98_EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            ui = C98_DialogueUI.Create(transform, bundle, reg, camDir.Cam);
            mgr = gameObject.AddComponent<C98_CutsceneManager>();
            mgr.Init(bundle, reg, ui, camDir, fx, audio);
            debug = gameObject.AddComponent<C98_DebugPanel>();
            debug.Init(mgr);

            fx.SetLightPreset("light_default");
            ui.ShowIdle(IdleText());
        }

        string IdleText()
        {
            bool done = mgr != null && mgr.IsCompleted(EntryCutscene);
            if (C98_Loc.English)
                return "98.Claude — Cutscene System Prototype\n\n" +
                       "<b>[ Lab Raid — luna's dream ]</b>\n\n" +
                       (done ? "Completed (play_mode=once). Press R to reset.\n\n" : "") +
                       "Enter : Play cutscene\n" +
                       "T : Play a timeline you made in the editor\n" +
                       "Space / Click / E : Advance dialogue\n" +
                       "S : Skip   ·   1~3 : Choose option\n" +
                       "L : 한국어/English   ·   F1 : Debug panel   ·   R : Reset";
            return "98.Claude — 컷씬 시스템 프로토타입\n\n" +
                   "<b>[ 연구소 습격 — 루나의 꿈 ]</b>\n\n" +
                   (done ? "재생 완료 (play_mode=once). R로 초기화.\n\n" : "") +
                   "Enter : 컷씬 재생\n" +
                   "T : 에디터에서 만든 타임라인 재생\n" +
                   "Space / 클릭 / E : 대사 진행\n" +
                   "S : 스킵   ·   1~3 : 선택지 선택\n" +
                   "L : 한국어/English   ·   F1 : 디버그 패널   ·   R : 초기화";
        }

        void Update()
        {
            if (mgr == null) return;

            if (Input.GetKeyDown(KeyCode.F1)) debug.Visible = !debug.Visible;
            if (Input.GetKeyDown(KeyCode.L))
            {
                C98_Loc.English = !C98_Loc.English;
                if (mgr.State == C98_State.Idle) ui.ShowIdle(IdleText());
            }

            if (mgr.State != C98_State.Idle || picking) return;

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (mgr.Play(EntryCutscene)) { }
                else ui.ShowIdle(IdleText());
            }
            if (Input.GetKeyDown(KeyCode.T))
                StartCoroutine(PickTimelineCo());
            if (Input.GetKeyDown(KeyCode.R))
            {
                mgr.ResetProgress();
                if (stageRoot != null) C98_StageBuilder.ResetToDefaults(stageRoot.transform);
                ui.ShowIdle(IdleText());
            }
        }

        // 에디터에서 만든 Timeline 에셋(Resources/Claude98/Timelines)을 골라 바로 재생
        IEnumerator PickTimelineCo()
        {
            var assets = Resources.LoadAll<TimelineAsset>("Claude98/Timelines");
            if (assets == null || assets.Length == 0)
            {
                ui.Toast(C98_Loc.English
                    ? "No timelines yet — create one via Tools → 98.Claude in the editor."
                    : "타임라인 없음 — 에디터 메뉴(Tools → 98.Claude)로 먼저 만들어줘.");
                yield break;
            }

            picking = true;
            var choice = new C98_Choice { id = "adhoc_pick" };
            int n = Mathf.Min(assets.Length, 4);
            for (int i = 0; i < n; i++)
                choice.options.Add(new C98_Option { text_ko = assets[i].name, text_en = assets[i].name });
            if (assets.Length > 4)
                ui.Toast(C98_Loc.English ? "More than 4 timelines — showing first 4." : "타임라인 4개 초과 — 앞의 4개만 표시.");

            ui.HideIdle();
            yield return ui.ShowChoice(choice);
            picking = false;
            mgr.PlayAdhocTimeline(assets[ui.SelectedOption].name);
        }

        // 매니저가 Idle로 돌아오면 대기 화면을 다시 띄운다
        C98_State lastState = C98_State.Idle;
        void LateUpdate()
        {
            if (mgr == null) return;
            if (mgr.State == C98_State.Idle && lastState != C98_State.Idle)
                ui.ShowIdle(IdleText());
            lastState = mgr.State;
        }

        void OnGUI()
        {
            if (dataErrors == null || dataErrors.Count == 0) return;
            GUILayout.BeginArea(new Rect(10, 10, 640, 500), GUI.skin.box);
            GUILayout.Label("<b>DATA_ERROR — cutscene data validation failed (playback refused)</b>");
            foreach (var e in dataErrors) GUILayout.Label("· " + e);
            GUILayout.EndArea();
        }
    }
}
