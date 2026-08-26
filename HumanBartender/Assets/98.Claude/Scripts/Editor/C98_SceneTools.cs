// 98.Claude 컷씬 프로토타입 — 에디터 도구
// PD의 컷씬 제작 루프: ① 무대 배치 → ② 새 타임라인 생성 → Timeline 창에서 드래그 편집·스크럽 미리보기
// → Play 모드 T키로 재생 테스트. 자세한 순서는 README 참조.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Claude98
{
    public static class C98_SceneTools
    {
        const string ScenePath = "Assets/98.Claude/Scenes/C98_CutsceneLab.unity";
        const string TimelineDir = "Assets/98.Claude/Resources/Claude98/Timelines";
        const string PreviewDirectorName = "C98_TimelinePreview";

        // ── ① 무대 배치 — Timeline 스크럽 미리보기가 보려면 무대가 씬에 있어야 한다 ──
        [MenuItem("Tools/98.Claude/1. 무대를 씬에 배치 (편집·미리보기용)")]
        public static void PlaceStage()
        {
            var existing = Object.FindFirstObjectByType<C98_StageRoot>();
            if (existing == null)
            {
                var bundle = C98_Loader.Load(null, out var errors);
                if (bundle == null || errors.Count > 0)
                {
                    foreach (var e in errors) Debug.LogError($"[C98] DATA_ERROR: {e}");
                    return;
                }
                var root = C98_StageBuilder.BuildLab(null, bundle);
                existing = root.GetComponent<C98_StageRoot>();
                Debug.Log("[C98] 무대 배치 완료 — 씬을 저장하면 유지된다");
            }

            // 미리보기용 카메라 — 게임 뷰로 스크럽 결과를 보는 데 필요
            if (Object.FindFirstObjectByType<Camera>() == null)
            {
                var camGo = new GameObject("C98_Camera");
                camGo.transform.SetParent(existing.transform, false);
                camGo.tag = "MainCamera";
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = 3.2f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
                camGo.transform.position = new Vector3(0, 0.6f, -10);
            }

            EnsurePreviewDirector();
            C98_Resolve.InvalidateCache();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = existing.gameObject;
        }

        static PlayableDirector EnsurePreviewDirector()
        {
            var dirGo = GameObject.Find(PreviewDirectorName);
            if (dirGo == null) dirGo = new GameObject(PreviewDirectorName);
            var director = dirGo.GetComponent<PlayableDirector>();
            if (director == null) director = dirGo.AddComponent<PlayableDirector>();
            director.playOnAwake = false; // 런타임 재생은 C98_CutsceneManager가 담당
            return director;
        }

        // ── ② 새 타임라인 생성 — 샘플 클립·마커가 채워진 상태로 Timeline 창이 열린다 ──
        [MenuItem("Tools/98.Claude/2. 새 컷씬 타임라인 만들기")]
        public static void CreateTimeline()
        {
            PlaceStage(); // 무대·카메라·미리보기 디렉터 보장

            System.IO.Directory.CreateDirectory(TimelineDir);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{TimelineDir}/tl_my_cutscene.playable");
            var asset = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(asset, path);

            // 00_EVENT — 대사·SFX·화면효과 마커 (미리보기 없음, 런타임 실행)
            var markers = asset.CreateTrack<MarkerTrack>(null, "00_EVENT");
            var sfx = markers.CreateMarker<C98_EventMarker>(0.2);
            sfx.eventType = C98_MarkerType.Sfx;
            sfx.payload = "alarm";
            var dlg = markers.CreateMarker<C98_EventMarker>(3.6);
            dlg.eventType = C98_MarkerType.Dialogue;
            dlg.payload = "sc_entry_pause";
            dlg.pauseTimeline = true;

            // 10_CAMERA — 스크럽하면 카메라 이동이 미리보기된다
            var camTrack = asset.CreateTrack<C98_CameraTrack>(null, "10_CAMERA");
            var camClip = camTrack.CreateClip<C98_CameraClip>();
            camClip.start = 0; camClip.duration = 2.0; camClip.displayName = "cam_home → cam_door";
            var camAsset = (C98_CameraClip)camClip.asset;
            camAsset.fromAnchor = "cam_home"; camAsset.toAnchor = "cam_door"; camAsset.toSize = 2.8f;

            // 20_ACTOR — 배우 이동
            var lunaTrack = asset.CreateTrack<C98_ActorMoveTrack>(null, "20_ACTOR");
            var mv = lunaTrack.CreateClip<C98_ActorMoveClip>();
            mv.start = 1.0; mv.duration = 1.6; mv.displayName = "luna → a_lab_center";
            var mvAsset = (C98_ActorMoveClip)mv.asset;
            mvAsset.actorId = "luna"; mvAsset.fromAnchor = "a_luna_start"; mvAsset.toAnchor = "a_lab_center";

            // 30_OBJECT — 문 열림
            var objTrack = asset.CreateTrack<C98_ObjectSlideTrack>(null, "30_OBJECT");
            var slide = objTrack.CreateClip<C98_ObjectSlideClip>();
            slide.start = 0.5; slide.duration = 0.7; slide.displayName = "lab_door 열림";
            var slideAsset = (C98_ObjectSlideClip)slide.asset;
            slideAsset.objectId = "lab_door"; slideAsset.offset = new Vector2(0, 2.7f);

            // 40_LIGHT — 조명 프리셋 (스크럽하면 색이 보인다)
            var lightTrack = asset.CreateTrack<C98_LightTrack>(null, "40_LIGHT");
            var lc = lightTrack.CreateClip<C98_LightPresetClip>();
            lc.start = 0.1; lc.duration = 4.0; lc.displayName = "light_lab_alarm";
            ((C98_LightPresetClip)lc.asset).preset = "light_lab_alarm";

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            var director = EnsurePreviewDirector();
            director.playableAsset = asset;
            Selection.activeGameObject = director.gameObject;
            EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            Debug.Log($"[C98] 타임라인 생성: {path}\n" +
                      "Timeline 창에서 클립을 드래그해 편집하고 스크럽으로 미리보기. " +
                      "Play 모드에서 T키로 재생 테스트. 파일명(tl_*)이 곧 timeline_key다.");
        }

        // ── ③ 무대 초기화 — 미리보기로 어질러진 배우·오브젝트·조명 원위치 ──
        [MenuItem("Tools/98.Claude/3. 무대 상태 초기화 (미리보기 정리)")]
        public static void ResetStage()
        {
            var root = Object.FindFirstObjectByType<C98_StageRoot>();
            if (root == null) { Debug.LogWarning("[C98] 씬에 무대 없음"); return; }
            C98_StageBuilder.ResetToDefaults(root.transform);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[C98] 무대 초기화 완료");
        }

        // ── 씬 자체가 깨졌을 때의 복구 ──
        [MenuItem("Tools/98.Claude/9. 프로토타입 씬 재생성 (복구용)")]
        public static void RebuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var go = new GameObject("C98_Bootstrap");
            go.AddComponent<C98_Bootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[C98] 씬 재생성 완료: {ScenePath} — 재생(Play)하면 컷씬 대기 화면이 뜬다");
        }
    }
}
