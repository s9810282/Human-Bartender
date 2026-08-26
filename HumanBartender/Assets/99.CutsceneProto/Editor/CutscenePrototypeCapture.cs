using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Editor
{
    [InitializeOnLoad]
    public static class CutscenePrototypeCapture
    {
        private const string ScenePath = "Assets/99.CutsceneProto/CutscenePrototype_Lab.unity";
        private const string CapturePath = "Assets/99.CutsceneProto/CutscenePrototype_Preview.png";
        private const string PendingKey = "LUNA.CutscenePrototype.Capture.Pending";
        private const string StartedKey = "LUNA.CutscenePrototype.Capture.Started";
        private const string CapturedKey = "LUNA.CutscenePrototype.Capture.Captured";
        private const string LastAdvanceKey = "LUNA.CutscenePrototype.Capture.LastAdvance";
        private const string ExitCodeKey = "LUNA.CutscenePrototype.Capture.ExitCode";
        private const string ModeKey = "LUNA.CutscenePrototype.Capture.Mode";
        private const string SkipSentKey = "LUNA.CutscenePrototype.Capture.SkipSent";

        static CutscenePrototypeCapture()
        {
            if (SessionState.GetBool(PendingKey, false))
                AttachCallbacks();
        }

        [MenuItem("Project L.U.N.A/Cutscene Prototype/Play And Capture Preview")]
        public static void PlayAndCapturePreview()
        {
            BeginTest("normal");
        }

        public static void PlayAndVerifySecondChoice()
        {
            BeginTest("second_choice");
        }

        public static void PlayAndVerifySkip()
        {
            BeginTest("skip");
        }

        private static void BeginTest(string mode)
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(CapturedKey, false);
            SessionState.SetBool(SkipSentKey, false);
            SessionState.SetInt(ExitCodeKey, 0);
            SessionState.SetString(ModeKey, mode);
            AttachCallbacks();
            EditorApplication.EnterPlaymode();
        }

        private static void AttachCallbacks()
        {
            EditorApplication.update -= UpdateCapture;
            EditorApplication.update += UpdateCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(PendingKey, false))
                return;

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                double now = EditorApplication.timeSinceStartup;
                SetDouble(StartedKey, now);
                SetDouble(LastAdvanceKey, now);
                AttachCallbacks();
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                int exitCode = SessionState.GetInt(ExitCodeKey, 0);
                CleanupSession();
                AssetDatabase.Refresh();
                if (exitCode == 0)
                    Debug.Log($"[CutscenePrototype] RUNTIME_OK / CAPTURE_OK — {CapturePath}");
                else if (exitCode == 3)
                    Debug.LogError("[CutscenePrototype] RUNTIME_ASSERT_FAILED — 분기·스킵·end_state 결과가 예상과 다릅니다.");
                else
                    Debug.LogError("[CutscenePrototype] RUNTIME_TIMEOUT — 20초 안에 완료되지 않았습니다.");

                if (Application.isBatchMode)
                    EditorApplication.Exit(exitCode);
            }
        }

        private static void UpdateCapture()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying)
                return;

            double now = EditorApplication.timeSinceStartup;
            double elapsed = now - GetDouble(StartedKey);
            bool captured = SessionState.GetBool(CapturedKey, false);
            string mode = SessionState.GetString(ModeKey, "normal");

            PrototypeCutsceneRunner runner = UnityEngine.Object.FindFirstObjectByType<PrototypeCutsceneRunner>();
            if (mode == "skip" && runner != null && elapsed >= 1d && !SessionState.GetBool(SkipSentKey, false))
            {
                SessionState.SetBool(SkipSentKey, true);
                runner.SkipForAutomation();
            }

            if (!captured && elapsed >= 3.8d)
            {
                SessionState.SetBool(CapturedKey, true);
                if (Application.isBatchMode)
                    Debug.Log("[CutscenePrototype] CAPTURE_SKIPPED — batch mode에서는 URP Camera.Render를 호출하지 않습니다.");
                else
                    CaptureGameFrame();
                return;
            }

            if (captured && runner != null && now - GetDouble(LastAdvanceKey) >= 0.45d)
            {
                SetDouble(LastAdvanceKey, now);
                if (mode == "second_choice" && runner.State == PrototypeCutsceneState.ShowingChoice)
                    runner.SelectChoiceForAutomation(1);
                else
                    runner.AdvanceForAutomation();
            }

            if (captured && runner != null && runner.State == PrototypeCutsceneState.Completed)
            {
                bool expectedSkip = mode == "skip";
                bool routeOkay = mode == "normal"
                    ? runner.HasFlag("route_hide")
                    : mode == "second_choice"
                        ? runner.HasFlag("route_run")
                        : true;
                bool resultOkay = runner.LastCompletionWasSkipped == expectedSkip
                                    && runner.HasFlag("lab_escape_seen")
                                    && routeOkay;
                SessionState.SetInt(ExitCodeKey, resultOkay ? 0 : 3);
                EditorApplication.ExitPlaymode();
                return;
            }

            if (elapsed >= 20d)
            {
                SessionState.SetInt(ExitCodeKey, 2);
                EditorApplication.ExitPlaymode();
            }
        }

        private static void CleanupSession()
        {
            EditorApplication.update -= UpdateCapture;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SessionState.EraseBool(PendingKey);
            SessionState.EraseBool(CapturedKey);
            SessionState.EraseString(StartedKey);
            SessionState.EraseString(LastAdvanceKey);
            SessionState.EraseInt(ExitCodeKey);
            SessionState.EraseString(ModeKey);
            SessionState.EraseBool(SkipSentKey);
        }

        private static void SetDouble(string key, double value)
        {
            SessionState.SetString(key, value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static double GetDouble(string key)
        {
            string value = SessionState.GetString(key, "0");
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed)
                ? parsed
                : 0d;
        }

        private static void CaptureGameFrame()
        {
            Camera camera = Camera.main;
            if (camera == null)
                throw new InvalidOperationException("미리보기를 캡처할 Main Camera가 없습니다.");

            const int width = 1280;
            const int height = 720;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;

            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();

                Texture2D texture = new(width, height, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                texture.Apply();
                byte[] png = texture.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(texture);

                string absolutePath = Path.GetFullPath(CapturePath);
                File.WriteAllBytes(absolutePath, png);
                Debug.Log($"[CutscenePrototype] PREVIEW_WRITTEN — {absolutePath}");
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }
    }
}
