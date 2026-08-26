// 98.Claude 컷씬 프로토타입 — 디버그 패널 (F1)
// IMGUI 기본 폰트는 한글이 없어 패널 문구는 영어를 쓴다.
using UnityEngine;

namespace Claude98
{
    public class C98_DebugPanel : MonoBehaviour
    {
        public bool Visible;
        C98_CutsceneManager mgr;

        public void Init(C98_CutsceneManager m) => mgr = m;

        void OnGUI()
        {
            if (!Visible || mgr == null) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 400), GUI.skin.box);
            GUILayout.Label("<b>C98 Cutscene Debug</b>");
            GUILayout.Label($"State: {mgr.State}");
            GUILayout.Label($"Input locked: {mgr.InputLocked}");

            var ctx = mgr.Ctx;
            if (ctx != null)
            {
                GUILayout.Label($"Cutscene: {ctx.cutscene.id}");
                var st = ctx.step_index < ctx.cutscene.sequence.Count ? ctx.cutscene.sequence[ctx.step_index] : null;
                GUILayout.Label($"Step: #{ctx.step_index} {(st != null ? st.id + " (" + st.type + ")" : "-")}");
                if (ctx.active_runner != null)
                    GUILayout.Label($"Timeline: {ctx.active_runner.Key}  t={ctx.active_runner.Clock:0.00}" +
                                    (ctx.active_runner.Paused ? " (paused)" : ""));
                GUILayout.Label($"Fired events: {ctx.fired_event_keys.Count}");
            }
            else GUILayout.Label("Cutscene: (none)");

            GUILayout.Space(6);
            GUILayout.Label("Flags:");
            foreach (var f in C98_Flags.All()) GUILayout.Label($"  · {f}");

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Skip (S)")) mgr.RequestSkip();
            if (GUILayout.Button($"Speed x{Time.timeScale:0}"))
                Time.timeScale = Time.timeScale >= 4f ? 1f : Time.timeScale * 2f;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset (R)")) mgr.ResetProgress();
            GUILayout.EndHorizontal();

            GUILayout.Space(4);
            GUILayout.Label($"Log: {mgr.LastLog}");
            GUILayout.EndArea();
        }
    }
}
