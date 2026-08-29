using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProjectLuna.WebAnimatic.Editor
{
    /// <summary>
    /// 플레이 모드 없이 특정 시각의 프레임을 렌더해 PNG로 저장한다.
    /// 플레이어가 t의 순수 함수라서 Seek만으로 어느 시점이든 재현된다 — 웹 애니매틱과 비교 검증용.
    /// </summary>
    public static class WebAnimaticCapture
    {
        const string ScenePath = "Assets/99.CutsceneProto/WebAnimatic/WebAnimatic.unity";
        const string OutDir = "Assets/99.CutsceneProto/WebAnimatic/Captures";

        static readonly (int scene, float t, string name)[] Shots =
        {
            (0, 3.2f,  "s1_est"),
            (0, 31.0f, "s1_radio"),
            (0, 45.6f, "s1_break"),
            (0, 51.8f, "s1_fire"),
            (0, 55.6f, "s1_jump"),
            (1, 2.7f,  "s2_speaker"),
            (1, 12.4f, "s2_torso"),
            (1, 18.3f, "s2_run"),
            (2, 7.0f,  "s3_arrive"),
            (2, 17.0f, "s3_open"),
            (2, 38.0f, "s3_talk"),
            (2, 73.5f, "s3_break"),
            (2, 80.0f, "s3_wide"),
            (2, 114.5f, "s3_fall"),
            (3, 2.8f,  "s5_frag1"),
            (3, 11.0f, "s5_hound"),
            (3, 20.5f, "s5_fall"),
        };

        [MenuItem("Window/Project L.U.N.A/Web Animatic/Capture Keyframes")]
        public static void CaptureAll()
        {
            EditorSceneManager.OpenScene(ScenePath);
            var player = Object.FindFirstObjectByType<WebAnimaticPlayer>();
            if (player == null) { Debug.LogError("[WebAnimatic] 플레이어 없음 — Build All 먼저"); return; }
            Directory.CreateDirectory(OutDir);

            player.EnsureInit();
            RenderTexture rt = new(480, 270, 24) { filterMode = FilterMode.Point };
            int cur = -1;
            foreach (var s in Shots)
            {
                if (s.scene != cur) { player.LoadScene(s.scene); cur = s.scene; }
                player.Seek(s.t);
                Camera cam = player.CaptureCamera;
                cam.targetTexture = rt;
                Canvas.ForceUpdateCanvases();
                foreach (TMP_Text tmp in player.GetComponentsInChildren<TMP_Text>(true))
                    tmp.ForceMeshUpdate();
                Render(cam, rt);
                cam.targetTexture = null;

                RenderTexture.active = rt;
                Texture2D tex = new(480, 270, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, 480, 270), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes($"{OutDir}/{s.name}.png", tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            rt.Release();
            AssetDatabase.Refresh();
            Debug.Log($"[WebAnimatic] 캡처 {Shots.Length}장 완료 → {OutDir}");
        }

        static void Render(Camera cam, RenderTexture rt)
        {
            RenderPipeline.StandardRequest req = new() { destination = rt };
            if (RenderPipeline.SupportsRenderRequest(cam, req))
                RenderPipeline.SubmitRenderRequest(cam, req);
            else
                cam.Render();
        }
    }
}
