using System;
using System.IO;
using ProjectLuna.CutscenePrototype.Editor.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace ProjectLuna.CutscenePrototype.Editor
{
    public static class CutscenePrototypeBuilder
    {
        private const string Root = "Assets/99.CutsceneProto";
        private const string ScenePath = Root + "/CutscenePrototype_Lab.unity";
        private const string DataPath = Root + "/Data/cutscene_test.json";
        private const string TimelineFolder = Root + "/GeneratedTimelines";

        [MenuItem("Project L.U.N.A/Cutscene Prototype/Build Test Scene")]
        public static void BuildTestScene()
        {
            EnsureFolder(Root, "GeneratedTimelines");

            PrototypeTimelineAsset entry = CreateOrUpdateTimeline(
                TimelineFolder + "/LabEntryTimeline.asset",
                PrototypeTimelineSegment.LabEntry,
                1.8f);
            PrototypeTimelineAsset attack = CreateOrUpdateTimeline(
                TimelineFolder + "/LabAttackTimeline.asset",
                PrototypeTimelineSegment.LabAttack,
                1.65f);
            PrototypeTimelineAsset escape = CreateOrUpdateTimeline(
                TimelineFolder + "/LabEscapeTimeline.asset",
                PrototypeTimelineSegment.LabEscape,
                2.2f);

            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (json == null)
                throw new InvalidOperationException($"TextAsset을 찾을 수 없습니다: {DataPath}");

            Scene previousScene = SceneManager.GetActiveScene();
            bool preservePreviousScene = previousScene.IsValid()
                                         && previousScene.isLoaded
                                         && !string.IsNullOrEmpty(previousScene.path);
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                preservePreviousScene ? NewSceneMode.Additive : NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            scene.name = "CutscenePrototype_Lab";

            GameObject cameraObject = new("Prototype Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.008f, 0.017f, 0.027f, 1f);
            LunaCutscenePixelCameraUtility.ApplyProjectPreset(camera);

            GameObject root = new("CutscenePrototypeRoot");
            PrototypeCutsceneView view = root.AddComponent<PrototypeCutsceneView>();
            view.Configure(camera);
            PlayableDirector director = root.AddComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            PrototypeCutsceneRunner runner = root.AddComponent<PrototypeCutsceneRunner>();
            runner.Configure(
                json,
                new[]
                {
                    new PrototypeTimelineEntry { key = "lab_entry", asset = entry },
                    new PrototypeTimelineEntry { key = "lab_attack", asset = attack },
                    new PrototypeTimelineEntry { key = "lab_escape", asset = escape }
                },
                true);

            GameObject note = new("README__PLAY_SCENE_TO_RUN");
            note.transform.SetParent(root.transform, false);

            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(runner);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(scene);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new InvalidOperationException($"씬 저장 실패: {ScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (preservePreviousScene && previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
                EditorSceneManager.CloseScene(scene, true);
            }

            ValidateBuiltScene();
            Debug.Log("[CutscenePrototype] BUILD_OK — CutscenePrototype_Lab.unity를 열고 Play를 누르세요.");
        }

        [MenuItem("Project L.U.N.A/Cutscene Prototype/Validate Test Scene")]
        public static void ValidateBuiltScene()
        {
            TextAsset json = AssetDatabase.LoadAssetAtPath<TextAsset>(DataPath);
            if (!CutscenePrototypeLoader.TryLoad(json, out PrototypeCutsceneData data, out string error))
                throw new InvalidOperationException($"데이터 검증 실패: {error}");

            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool openedForValidation = !scene.IsValid() || !scene.isLoaded;
            bool validationIsAdditive = false;
            if (openedForValidation)
            {
                Scene active = SceneManager.GetActiveScene();
                validationIsAdditive = active.IsValid() && active.isLoaded && !string.IsNullOrEmpty(active.path);
                scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    validationIsAdditive ? OpenSceneMode.Additive : OpenSceneMode.Single);
            }
            PrototypeCutsceneRunner runner = FindInScene<PrototypeCutsceneRunner>(scene);
            PrototypeCutsceneView view = FindInScene<PrototypeCutsceneView>(scene);
            PlayableDirector director = FindInScene<PlayableDirector>(scene);
            Camera camera = FindInScene<Camera>(scene);

            if (!scene.IsValid() || runner == null || view == null || director == null || camera == null)
                throw new InvalidOperationException("테스트 씬 필수 구성요소가 누락됐습니다.");
            if (!LunaCutscenePixelCameraUtility.MatchesProjectPreset(camera, out string pixelCameraReason))
                throw new InvalidOperationException($"테스트 씬 픽셀 카메라 설정 불일치: {pixelCameraReason}");

            string[] expectedTimelinePaths =
            {
                TimelineFolder + "/LabEntryTimeline.asset",
                TimelineFolder + "/LabAttackTimeline.asset",
                TimelineFolder + "/LabEscapeTimeline.asset"
            };
            foreach (string path in expectedTimelinePaths)
            {
                if (AssetDatabase.LoadAssetAtPath<PrototypeTimelineAsset>(path) == null)
                    throw new InvalidOperationException($"Timeline 세그먼트 누락: {path}");
            }

            Debug.Log($"[CutscenePrototype] VALIDATION_OK — id={data.cutscene_id}, steps={data.steps.Length}, locale=ko/en, scene={scene.name}");
            if (openedForValidation && validationIsAdditive)
                EditorSceneManager.CloseScene(scene, true);
        }

        private static PrototypeTimelineAsset CreateOrUpdateTimeline(
            string path,
            PrototypeTimelineSegment segment,
            float duration)
        {
            PrototypeTimelineAsset asset = AssetDatabase.LoadAssetAtPath<PrototypeTimelineAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PrototypeTimelineAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.Configure(segment, duration);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }
            return null;
        }
    }
}
