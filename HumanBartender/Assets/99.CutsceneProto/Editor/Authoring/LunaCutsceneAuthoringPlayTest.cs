using System.Globalization;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    [InitializeOnLoad]
    public static class LunaCutsceneAuthoringPlayTest
    {
        private const string PendingKey = "LUNA.CutsceneAuthoring.Test.Pending";
        private const string ModeKey = "LUNA.CutsceneAuthoring.Test.Mode";
        private const string StartedKey = "LUNA.CutsceneAuthoring.Test.Started";
        private const string LastAdvanceKey = "LUNA.CutsceneAuthoring.Test.LastAdvance";
        private const string SkipSentKey = "LUNA.CutsceneAuthoring.Test.SkipSent";
        private const string ExitCodeKey = "LUNA.CutsceneAuthoring.Test.ExitCode";
        private const string BubbleObservedKey = "LUNA.CutsceneAuthoring.Test.BubbleObserved";

        static LunaCutsceneAuthoringPlayTest()
        {
            if (SessionState.GetBool(PendingKey, false))
                Attach();
        }

        public static void PlayAndVerifyNormal()
        {
            Begin("normal");
        }

        public static void PlayAndVerifySkip()
        {
            Begin("skip");
        }

        private static void Begin(string mode)
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            EditorSceneManager.OpenScene(LunaCutsceneAuthoringBuilder.AuthoringScenePath, OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetString(ModeKey, mode);
            SessionState.SetBool(SkipSentKey, false);
            SessionState.SetBool(BubbleObservedKey, false);
            SessionState.SetInt(ExitCodeKey, 0);
            Attach();
            EditorApplication.EnterPlaymode();
        }

        private static void Attach()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false))
                return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                double now = EditorApplication.timeSinceStartup;
                SetDouble(StartedKey, now);
                SetDouble(LastAdvanceKey, now);
                Attach();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                int exitCode = SessionState.GetInt(ExitCodeKey, 0);
                string mode = SessionState.GetString(ModeKey, "normal");
                Cleanup();
                if (exitCode == 0)
                    Debug.Log($"[LunaCutsceneAuthoring] RUNTIME_{mode.ToUpperInvariant()}_OK");
                else
                    Debug.LogError($"[LunaCutsceneAuthoring] RUNTIME_{mode.ToUpperInvariant()}_FAILED code={exitCode}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(exitCode);
            }
        }

        private static void Update()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying)
                return;

            LunaCutsceneDirector runtime = Object.FindFirstObjectByType<LunaCutsceneDirector>();
            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - GetDouble(StartedKey);
            string mode = SessionState.GetString(ModeKey, "normal");
            if (runtime == null)
            {
                if (elapsed > 3d) Finish(4);
                return;
            }

            if (mode == "skip"
                && elapsed > 1d
                && !SessionState.GetBool(SkipSentKey, false))
            {
                SessionState.SetBool(SkipSentKey, true);
                runtime.SkipForAutomation();
            }

            if (runtime.State == LunaAuthoringCutsceneState.WaitingDialogue
                && now - GetDouble(LastAdvanceKey) > 0.18d)
            {
                if (mode == "normal" && !VerifySpeechBubble(runtime))
                {
                    Finish(6);
                    return;
                }
                SetDouble(LastAdvanceKey, now);
                runtime.AdvanceForAutomation();
            }

            if (runtime.State == LunaAuthoringCutsceneState.Completed)
            {
                bool markerFlag = runtime.HasFlag("lab_intrusion_seen");
                bool completionFlag = runtime.HasFlag("lab_cutscene_complete");
                bool bubbleOkay = mode != "normal" || SessionState.GetBool(BubbleObservedKey, false);
                bool resultOkay = markerFlag && completionFlag && bubbleOkay;
                Debug.Log($"[LunaCutsceneAuthoring] ASSERT marker_flag={markerFlag}, completion_flag={completionFlag}, bubble={bubbleOkay}, time={runtime.Director.time:0.00}");
                Finish(resultOkay ? 0 : 3);
                return;
            }

            if (runtime.State == LunaAuthoringCutsceneState.Failed)
            {
                Finish(5);
                return;
            }

            if (elapsed > 18d)
                Finish(2);
        }

        private static void Finish(int exitCode)
        {
            SessionState.SetInt(ExitCodeKey, exitCode);
            EditorApplication.ExitPlaymode();
        }

        private static void Cleanup()
        {
            EditorApplication.update -= Update;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseString(ModeKey);
            SessionState.EraseString(StartedKey);
            SessionState.EraseString(LastAdvanceKey);
            SessionState.EraseBool(SkipSentKey);
            SessionState.EraseBool(BubbleObservedKey);
            SessionState.EraseInt(ExitCodeKey);
        }

        private static bool VerifySpeechBubble(LunaCutsceneDirector runtime)
        {
            LunaCutsceneDialogueUI ui = runtime.DialogueUI;
            LunaCutsceneSpeechBubbleView bubble = ui != null ? ui.ActiveBubble : null;
            bool valid = ui != null
                         && ui.IsVisible
                         && bubble != null
                         && bubble.BubbleSize.x > 0f
                         && bubble.BubbleSize.y > 0f
                         && bubble.CharacterCount > 0
                         && bubble.IsTailVisible;
            if (valid)
            {
                SessionState.SetBool(BubbleObservedKey, true);
                return true;
            }

            Debug.LogError(
                "[LunaCutsceneAuthoring] TMP 말풍선 생성·크기 확정·꼬리 표시 검증에 실패했습니다. "
                + $"ui={ui != null}, visible={ui != null && ui.IsVisible}, bubble={bubble != null}, "
                + $"size={(bubble != null ? bubble.BubbleSize : Vector2.zero)}, "
                + $"characters={(bubble != null ? bubble.CharacterCount : 0)}, "
                + $"tail={bubble != null && bubble.IsTailVisible}");
            return false;
        }

        private static void SetDouble(string key, double value)
        {
            SessionState.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double GetDouble(string key)
        {
            return double.TryParse(
                SessionState.GetString(key, "0"),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double value)
                ? value
                : 0d;
        }
    }
}
