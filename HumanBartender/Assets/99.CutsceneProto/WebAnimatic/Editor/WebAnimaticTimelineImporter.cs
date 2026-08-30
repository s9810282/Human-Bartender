using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace ProjectLuna.WebAnimatic.Editor
{
    /// <summary>
    /// WebAnimatic JSON 한 장면을 Timeline의 전용 클립 하나로 감싼다.
    /// 배우·카메라·대사·효과를 분해해 다시 재구축하지 않고, 웹의 결정적 시간 평가를
    /// Timeline 스크럽·클립 이동·배속에 그대로 연결한다.
    /// </summary>
    public static class WebAnimaticTimelineImporter
    {
        const string Root = "Assets/99.CutsceneProto/WebAnimatic";
        const string LibraryPath = Root + "/WebAnimaticLibrary.asset";
        const string TimelineFolder = Root + "/Timelines";
        const string TimelineScenePath = Root + "/WebAnimaticTimeline.unity";

        [MenuItem("Window/Project L.U.N.A/Web Animatic/Build Timeline Bridge")]
        public static void BuildTimelineBridgeMenu()
        {
            WebAnimaticLibrary library = AssetDatabase.LoadAssetAtPath<WebAnimaticLibrary>(LibraryPath);
            if (library == null)
                throw new InvalidOperationException("WebAnimaticLibrary.asset이 없습니다. Build All을 먼저 실행하세요.");
            BuildTimelineBridge(library);
        }

        public static void BuildTimelineBridge(WebAnimaticLibrary library)
        {
            EnsureFolder(TimelineFolder);
            if (library.scenes == null || library.scenes.Length == 0)
                throw new InvalidOperationException("WebAnimaticLibrary에 장면이 없습니다.");

            foreach (WebAnimaticLibrary.SceneEntry entry in library.scenes)
                CreateOrUpdateTimeline(entry);

            BuildPreviewScene(library);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[WebAnimatic] Timeline Bridge 완료 — {library.scenes.Length}개 Timeline + 미리보기 Scene");
        }

        static TimelineAsset CreateOrUpdateTimeline(WebAnimaticLibrary.SceneEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.id) || entry.json == null)
                throw new InvalidOperationException("WebAnimatic 장면 엔트리의 id 또는 json이 비어 있습니다.");

            string path = $"{TimelineFolder}/{entry.id.ToUpperInvariant()}_WebAnimatic.playable";
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
            if (timeline == null)
            {
                timeline = ScriptableObject.CreateInstance<TimelineAsset>();
                timeline.name = entry.id.ToUpperInvariant() + "_WebAnimatic";
                AssetDatabase.CreateAsset(timeline, path);
            }

            timeline.editorSettings.frameRate = 60d;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            SceneData data = SceneData.Parse(entry.json.text);
            timeline.fixedDuration = Math.Max(0.1d, data.end);

            WebAnimaticTimelineTrack track = timeline.GetOutputTracks()
                .OfType<WebAnimaticTimelineTrack>()
                .FirstOrDefault();
            if (track == null)
                track = timeline.CreateTrack<WebAnimaticTimelineTrack>(null, "WEB ANIMATIC · SOURCE");

            foreach (TimelineClip oldClip in track.GetClips().ToArray())
                timeline.DeleteClip(oldClip);

            TimelineClip clip = track.CreateClip<WebAnimaticTimelineClip>();
            clip.start = 0d;
            clip.duration = Math.Max(0.1d, data.end);
            clip.displayName = string.IsNullOrEmpty(entry.label) ? entry.id : entry.label;
            ((WebAnimaticTimelineClip)clip.asset).sceneId = entry.id;

            EditorUtility.SetDirty(track);
            EditorUtility.SetDirty(timeline);
            return timeline;
        }

        static void BuildPreviewScene(WebAnimaticLibrary library)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("WebAnimaticTimeline");
            WebAnimaticPlayer player = root.AddComponent<WebAnimaticPlayer>();
            player.library = library;
            player.externallyDriven = true;

            PlayableDirector director = root.AddComponent<PlayableDirector>();
            director.playOnAwake = true;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            TimelineAsset first = AssetDatabase.LoadAssetAtPath<TimelineAsset>(
                $"{TimelineFolder}/{library.scenes[0].id.ToUpperInvariant()}_WebAnimatic.playable");
            director.playableAsset = first;
            WebAnimaticTimelineTrack track = first.GetOutputTracks().OfType<WebAnimaticTimelineTrack>().First();
            director.SetGenericBinding(track, player);

            EditorSceneManager.SaveScene(scene, TimelineScenePath);
        }

        [MenuItem("Window/Project L.U.N.A/Web Animatic/Validate Timeline Bridge")]
        public static void ValidateTimelineBridge()
        {
            WebAnimaticLibrary library = AssetDatabase.LoadAssetAtPath<WebAnimaticLibrary>(LibraryPath);
            if (library == null || library.scenes == null)
                throw new InvalidOperationException("WebAnimaticLibrary를 찾을 수 없습니다.");

            if (!File.Exists(TimelineScenePath))
                throw new InvalidOperationException($"미리보기 Scene 누락: {TimelineScenePath}");

            Scene preview = EditorSceneManager.OpenScene(TimelineScenePath, OpenSceneMode.Single);
            GameObject root = preview.GetRootGameObjects().FirstOrDefault(go => go.name == "WebAnimaticTimeline");
            WebAnimaticPlayer player = root != null ? root.GetComponent<WebAnimaticPlayer>() : null;
            PlayableDirector director = root != null ? root.GetComponent<PlayableDirector>() : null;
            if (player == null || director == null)
                throw new InvalidOperationException("Timeline 미리보기 Scene의 Player 또는 PlayableDirector가 누락됐습니다.");

            foreach (WebAnimaticLibrary.SceneEntry entry in library.scenes)
            {
                string path = $"{TimelineFolder}/{entry.id.ToUpperInvariant()}_WebAnimatic.playable";
                TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
                if (timeline == null)
                    throw new InvalidOperationException($"Timeline 누락: {path}");
                WebAnimaticTimelineTrack track = timeline.GetOutputTracks().OfType<WebAnimaticTimelineTrack>().FirstOrDefault();
                TimelineClip clip = track?.GetClips().SingleOrDefault();
                if (clip?.asset is not WebAnimaticTimelineClip source || source.sceneId != entry.id)
                    throw new InvalidOperationException($"Timeline 소스 연결 불일치: {entry.id}");
                float expected = SceneData.Parse(entry.json.text).end;
                if (Math.Abs(clip.duration - expected) > 0.001d)
                    throw new InvalidOperationException($"Timeline 길이 불일치: {entry.id} ({clip.duration} != {expected})");

                director.playableAsset = timeline;
                director.SetGenericBinding(track, player);
                director.RebuildGraph();
                double[] samples = { 0d, expected * 0.5d, Math.Max(0d, expected - 0.01d) };
                foreach (double sample in samples)
                {
                    director.time = sample;
                    director.Evaluate();
                    if (!string.Equals(player.LoadedSceneId, entry.id, StringComparison.Ordinal))
                        throw new InvalidOperationException($"Timeline 평가 장면 불일치: {entry.id} @ {sample:F2}s");
                    if (player.CaptureCamera == null)
                        throw new InvalidOperationException($"Timeline 평가 카메라 누락: {entry.id} @ {sample:F2}s");
                }
                director.Stop();
            }

            Debug.Log($"[WebAnimatic] TIMELINE_BRIDGE_OK — scenes={library.scenes.Length}, samples={library.scenes.Length * 3}");
        }

        static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
