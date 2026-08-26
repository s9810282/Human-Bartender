using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectLuna.CutscenePrototype.Authoring;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    public static class LunaCutsceneAuthoringBuilder
    {
        public sealed class ActorTrackSet
        {
            public GroupTrack group;
            public LunaActorMoveTrack move;
            public AnimationTrack animation;
            public ActivationTrack activation;
        }

        public const string Root = "Assets/99.CutsceneProto";
        public const string AuthoringScenePath = Root + "/CutsceneAuthoring_Lab.unity";
        public const string ExampleTimelinePath = Root + "/Authoring/Timelines/LabResearchAuthoring.playable";
        public const string ExampleDefinitionPath = Root + "/Authoring/Definitions/LabResearchDefinition.asset";
        private const string ClipFolder = Root + "/Authoring/AnimationClips";
        private static readonly Dictionary<string, TrackAsset> CreatedExampleTracks = new(StringComparer.Ordinal);

        [MenuItem("Project L.U.N.A/Cutscene Authoring/Create or Open Lab")]
        public static void CreateOrOpenAuthoringLab()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AuthoringScenePath) != null)
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(AuthoringScenePath, OpenSceneMode.Single);
                    EnsureSceneAuthoringHelpers();
                }
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            BuildExampleLab();
        }

        public static void BuildExampleLab()
        {
            EnsureFolders();
            TimelineAsset timeline = CreateExampleTimelineIfMissing();
            LunaCutsceneDefinition definition = CreateExampleDefinitionIfMissing();
            LunaCutsceneAuthoringBaker.Bake(timeline, definition);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "CutsceneAuthoring_Lab";

            GameObject root = new("LUNA_CutsceneAuthoringRoot");
            PlayableDirector playableDirector = root.AddComponent<PlayableDirector>();
            LunaCutsceneBindingRegistry registry = root.AddComponent<LunaCutsceneBindingRegistry>();
            LunaCutsceneDirector runtime = root.AddComponent<LunaCutsceneDirector>();
            playableDirector.playableAsset = timeline;
            playableDirector.playOnAwake = false;
            playableDirector.extrapolationMode = DirectorWrapMode.Hold;
            playableDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;

            GameObject stage = new("STAGE");
            stage.transform.SetParent(root.transform, false);
            Sprite placeholder = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            GameObject background = CreateSpriteObject(stage.transform, "Laboratory_Background", "lab_background", placeholder,
                new Vector3(0f, 0f, 3f), new Vector3(18f, 10f, 1f), new Color(0.035f, 0.07f, 0.09f), -20);
            GameObject windowGlow = CreateSpriteObject(stage.transform, "Window_Glow", "window_glow", placeholder,
                new Vector3(4.6f, 1.1f, 2f), new Vector3(3.2f, 4.2f, 1f), new Color(0.08f, 0.52f, 0.58f, 0.65f), -10);
            GameObject console = CreateSpriteObject(stage.transform, "Lab_Console", "lab_console", placeholder,
                new Vector3(0f, -3.15f, 1f), new Vector3(12f, 1.25f, 1f), new Color(0.11f, 0.16f, 0.18f), 1);
            GameObject luna = CreateCharacter(stage.transform, "Luna", "luna", placeholder,
                new Vector3(-4.2f, -1.2f, 0f), new Color(0.58f, 0.9f, 0.94f), 10);
            GameObject researcher = CreateCharacter(stage.transform, "Researcher", "researcher", placeholder,
                new Vector3(3.6f, -1.2f, 0f), new Color(0.92f, 0.74f, 0.5f), 9);
            GameObject intruder = CreateCharacter(stage.transform, "Intruder", "intruder", placeholder,
                new Vector3(7.5f, -1.2f, 0f), new Color(0.9f, 0.18f, 0.3f), 11);

            GameObject cameraRig = new("CameraRig");
            cameraRig.transform.SetParent(root.transform, false);
            cameraRig.AddComponent<Animator>();
            AddBindingId(cameraRig, "camera_rig");
            GameObject cameraObject = new("CutsceneCamera", typeof(Camera));
            cameraObject.transform.SetParent(cameraRig.transform, false);
            cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.backgroundColor = new Color(0.008f, 0.014f, 0.02f);
            cameraObject.AddComponent<LunaCutsceneCameraGuide>();

            Transform anchors = new GameObject("ANCHORS").transform;
            anchors.SetParent(stage.transform, false);
            CreateAnchor(anchors, "luna_start", luna.transform.position);
            CreateAnchor(anchors, "luna_end", new Vector3(-2.8f, -1.2f, 0f));
            CreateAnchor(anchors, "researcher_start", researcher.transform.position);
            CreateAnchor(anchors, "intruder_start", intruder.transform.position);
            CreateAnchor(anchors, "intruder_end", new Vector3(4.8f, -1.2f, 0f));

            GameObject audioRoot = new("AUDIO");
            audioRoot.transform.SetParent(root.transform, false);
            AudioSource sfx = CreateAudioSource(audioRoot.transform, "SFX_Source", "sfx_source");
            AudioSource bgm = CreateAudioSource(audioRoot.transform, "BGM_Source", "bgm_source");
            bgm.loop = true;

            LunaCutsceneDialogueUI ui = CreateUi(root.transform);
            registry.RebuildFromChildren();
            runtime.Configure(definition, playableDirector, registry, ui);

            BindTrack(playableDirector, timeline, "Luna Animation", luna.GetComponent<Animator>());
            BindTrack(playableDirector, timeline, "Researcher Animation", researcher.GetComponent<Animator>());
            BindTrack(playableDirector, timeline, "Intruder Animation", intruder.GetComponent<Animator>());
            BindTrack(playableDirector, timeline, "Camera Animation", cameraRig.GetComponent<Animator>());
            BindTrack(playableDirector, timeline, "SFX", sfx);
            BindTrack(playableDirector, timeline, "BGM", bgm);

            GameObject note = new("README__OPEN_PROJECT_LUNA_CUTSCENE_AUTHORING_STUDIO");
            note.transform.SetParent(root.transform, false);

            foreach (UnityEngine.Object dirty in new UnityEngine.Object[]
                     {
                         root, playableDirector, registry, runtime, background, windowGlow, console, luna, researcher,
                         intruder, cameraRig, ui
                     })
            {
                if (dirty != null) EditorUtility.SetDirty(dirty);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, AuthoringScenePath))
                throw new InvalidOperationException($"컷씬 제작 씬 저장 실패: {AuthoringScenePath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log("[LunaCutsceneAuthoring] BUILD_OK — Window > Project L.U.N.A > Cutscene Authoring Studio를 여세요.");
        }

        public static void BuildExampleLabForBatch()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(AuthoringScenePath) == null)
                BuildExampleLab();
            else
            {
                EnsureFolders();
                TimelineAsset timeline = CreateExampleTimelineIfMissing();
                LunaCutsceneDefinition definition = CreateExampleDefinitionIfMissing();
                LunaCutsceneAuthoringBaker.Bake(timeline, definition);
            }
            EditorSceneManager.OpenScene(AuthoringScenePath, OpenSceneMode.Single);
            EnsureSceneAuthoringHelpers();
            LunaCutsceneAuthoringValidator.ValidateSceneAndLog();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), AuthoringScenePath);
            AssetDatabase.SaveAssets();
        }

        public static (TimelineAsset timeline, LunaCutsceneDefinition definition) CreateBlankAssets(string rawId)
        {
            EnsureFolders();
            string id = SanitizeId(rawId);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("컷씬 ID를 입력하세요.");

            string timelinePath = $"{Root}/Authoring/Timelines/{id}.playable";
            string definitionPath = $"{Root}/Authoring/Definitions/{id}.asset";
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(timelinePath) != null
                || AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(definitionPath) != null)
                throw new InvalidOperationException($"같은 ID의 제작 에셋이 이미 있습니다: {id}");

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = id;
            AssetDatabase.CreateAsset(timeline, timelinePath);
            timeline.CreateMarkerTrack();
            timeline.CreateTrack<GroupTrack>(null, "CHARACTERS");
            timeline.CreateTrack<GroupTrack>(null, "CAMERA");
            timeline.CreateTrack<GroupTrack>(null, "AUDIO");

            LunaCutsceneDefinition definition = ScriptableObject.CreateInstance<LunaCutsceneDefinition>();
            definition.Configure(id, id, id);
            definition.autoPlay = false;
            definition.playMode = LunaCutscenePlayMode.Repeatable;
            AssetDatabase.CreateAsset(definition, definitionPath);
            AssetDatabase.SaveAssets();
            return (timeline, definition);
        }

        public static void EnsureSceneAuthoringHelpers()
        {
            LunaCutsceneDirector runtime = UnityEngine.Object.FindFirstObjectByType<LunaCutsceneDirector>();
            if (runtime == null)
                return;
            runtime.BindingRegistry?.RebuildFromChildren();
            LunaCutsceneUiAssetBuilder.UpgradeSceneUi(runtime);

            Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (camera != null && camera.GetComponent<LunaCutsceneCameraGuide>() == null)
                Undo.AddComponent<LunaCutsceneCameraGuide>(camera.gameObject);

            Transform stage = runtime.transform.Find("STAGE");
            if (stage == null)
                return;
            Transform anchors = stage.Find("ANCHORS");
            if (anchors == null)
            {
                GameObject anchorRoot = new("ANCHORS");
                Undo.RegisterCreatedObjectUndo(anchorRoot, "Create L.U.N.A Anchor Root");
                anchorRoot.transform.SetParent(stage, false);
                anchors = anchorRoot.transform;
            }

            if (runtime.BindingRegistry == null)
                return;
            foreach (LunaCutsceneBindingEntry entry in runtime.BindingRegistry.Bindings)
            {
                if (entry?.target == null || string.IsNullOrWhiteSpace(entry.id))
                    continue;
                if (entry.target.GetComponent<Animator>() == null
                    || entry.target.GetComponentInChildren<SpriteRenderer>(true) == null)
                    continue;
                EnsureSpeechAnchor(entry.target, false);
                string id = entry.id + "_start";
                if (UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None)
                    .Any(anchor => anchor.AnchorId == id))
                    continue;
                CreateAnchor(anchors, id, entry.target.transform.position);
            }

            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        }

        public static LunaCutsceneSpeechAnchor EnsureSpeechAnchor(GameObject actor, bool recapturePosition)
        {
            if (actor == null)
                throw new InvalidOperationException("말풍선 앵커를 만들 배우가 필요합니다.");

            LunaCutsceneSpeechAnchor rootAnchor = actor.GetComponent<LunaCutsceneSpeechAnchor>();
            Transform anchorTransform = actor.transform.Find("SpeechAnchor");
            if (anchorTransform == null)
            {
                GameObject anchorObject = new("SpeechAnchor");
                if (!Application.isBatchMode)
                    Undo.RegisterCreatedObjectUndo(anchorObject, "Create L.U.N.A Speech Anchor");
                anchorObject.transform.SetParent(actor.transform, false);
                anchorTransform = anchorObject.transform;
                recapturePosition = true;
            }

            LunaCutsceneSpeechAnchor anchor = anchorTransform.GetComponent<LunaCutsceneSpeechAnchor>();
            if (anchor == null)
                anchor = Application.isBatchMode
                    ? anchorTransform.gameObject.AddComponent<LunaCutsceneSpeechAnchor>()
                    : Undo.AddComponent<LunaCutsceneSpeechAnchor>(anchorTransform.gameObject);

            if (recapturePosition)
                anchorTransform.position = ResolveActorSpeechWorldPosition(actor);
            anchor.Configure(Vector3.zero);
            EditorUtility.SetDirty(anchor);

            if (rootAnchor != null && rootAnchor != anchor)
            {
                if (Application.isBatchMode)
                    UnityEngine.Object.DestroyImmediate(rootAnchor);
                else
                    Undo.DestroyObjectImmediate(rootAnchor);
            }
            return anchor;
        }

        private static Vector3 ResolveActorSpeechWorldPosition(GameObject actor)
        {
            SpriteRenderer[] renderers = actor.GetComponentsInChildren<SpriteRenderer>(true);
            bool found = false;
            Bounds bounds = default;
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null || renderer.sprite == null)
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return found
                ? new Vector3(bounds.center.x, bounds.max.y + 0.2f, bounds.center.z)
                : actor.transform.position + Vector3.up * 2f;
        }

        public static LunaCutsceneBindingId EnsureBinding(
            LunaCutsceneDirector runtime,
            GameObject target,
            string rawId)
        {
            if (runtime == null || target == null)
                throw new InvalidOperationException("Cutscene Director와 대상 오브젝트가 필요합니다.");
            if (runtime.BindingRegistry == null)
                throw new InvalidOperationException("Cutscene Director에 Binding Registry가 연결되지 않았습니다.");
            string id = SanitizeId(rawId);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("Binding ID를 입력하세요.");
            LunaCutsceneBindingEntry duplicate = runtime.BindingRegistry.Bindings
                .FirstOrDefault(entry => entry != null && entry.id == id && entry.target != target);
            if (duplicate != null)
                throw new InvalidOperationException($"Binding ID '{id}'는 이미 '{duplicate.target.name}'에서 사용 중입니다.");

            LunaCutsceneBindingId binding = target.GetComponent<LunaCutsceneBindingId>();
            if (binding == null)
                binding = Undo.AddComponent<LunaCutsceneBindingId>(target);
            Undo.RecordObject(binding, "Configure L.U.N.A Binding ID");
            binding.Configure(id);
            runtime.BindingRegistry.RebuildFromChildren();
            EditorUtility.SetDirty(binding);
            EditorUtility.SetDirty(runtime.BindingRegistry);
            EditorSceneManager.MarkSceneDirty(target.scene);
            return binding;
        }

        public static LunaCutsceneAnchor CreateAnchor(Transform parent, string rawId, Vector3 worldPosition)
        {
            string id = SanitizeId(rawId);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("앵커 ID를 입력하세요.");
            LunaCutsceneAnchor duplicate = UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None)
                .FirstOrDefault(anchor => anchor.AnchorId == id);
            if (duplicate != null)
                throw new InvalidOperationException($"같은 앵커 ID가 이미 있습니다: {id}");
            GameObject target = new("ANCHOR_" + id);
            if (!Application.isBatchMode)
                Undo.RegisterCreatedObjectUndo(target, "Create L.U.N.A Cutscene Anchor");
            target.transform.SetParent(parent, true);
            target.transform.position = worldPosition;
            LunaCutsceneAnchor anchor = target.AddComponent<LunaCutsceneAnchor>();
            anchor.Configure(id);
            EditorUtility.SetDirty(anchor);
            return anchor;
        }

        public static ActorTrackSet CreateActorTrackSet(
            PlayableDirector director,
            TimelineAsset timeline,
            GameObject actor,
            string rawId)
        {
            if (director == null || timeline == null || actor == null)
                throw new InvalidOperationException("Director, Timeline, 배우 오브젝트가 필요합니다.");
            string id = SanitizeId(rawId);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("배우 Binding ID를 입력하세요.");

            Undo.RegisterCompleteObjectUndo(timeline, "Create L.U.N.A Actor Track Set");
            string groupName = "ACTOR_" + id;
            GroupTrack group = timeline.GetRootTracks().OfType<GroupTrack>()
                .FirstOrDefault(track => track.name == groupName);
            group ??= timeline.CreateTrack<GroupTrack>(null, groupName);

            LunaActorMoveTrack move = group.GetChildTracks().OfType<LunaActorMoveTrack>().FirstOrDefault()
                                      ?? timeline.CreateTrack<LunaActorMoveTrack>(group, id + " Move");
            AnimationTrack animation = group.GetChildTracks().OfType<AnimationTrack>().FirstOrDefault()
                                       ?? timeline.CreateTrack<AnimationTrack>(group, id + " Animation");
            ActivationTrack activation = group.GetChildTracks().OfType<ActivationTrack>().FirstOrDefault()
                                         ?? timeline.CreateTrack<ActivationTrack>(group, id + " Visibility");

            Animator animator = actor.GetComponent<Animator>() ?? Undo.AddComponent<Animator>(actor);
            director.SetGenericBinding(move, actor.transform);
            director.SetGenericBinding(animation, animator);
            director.SetGenericBinding(activation, actor);
            SaveTrackChange(director, timeline, group);
            return new ActorTrackSet { group = group, move = move, animation = animation, activation = activation };
        }

        public static TimelineClip AddMoveClip(
            PlayableDirector director,
            TimelineAsset timeline,
            LunaActorMoveTrack track,
            Transform from,
            Transform to,
            double start,
            float unitsPerSecond)
        {
            if (director == null || timeline == null || track == null || from == null || to == null)
                throw new InvalidOperationException("이동 클립에는 Move Track과 시작·도착 앵커가 필요합니다.");

            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Actor Move Clip");
            TimelineClip clip = track.CreateClip<LunaActorMoveClip>();
            LunaActorMoveClip asset = (LunaActorMoveClip)clip.asset;
            string token = Guid.NewGuid().ToString("N");
            PropertyName fromName = new("luna_move_from_" + token);
            PropertyName toName = new("luna_move_to_" + token);
            asset.fromAnchor.exposedName = fromName;
            asset.fromAnchor.defaultValue = from;
            asset.toAnchor.exposedName = toName;
            asset.toAnchor.defaultValue = to;
            director.SetReferenceValue(fromName, from);
            director.SetReferenceValue(toName, to);
            clip.start = Math.Max(0d, start);
            clip.duration = Math.Max(0.1d, Vector3.Distance(from.position, to.position) / Mathf.Max(0.01f, unitsPerSecond));
            clip.displayName = $"{from.GetComponent<LunaCutsceneAnchor>()?.AnchorId ?? from.name} → {to.GetComponent<LunaCutsceneAnchor>()?.AnchorId ?? to.name}";
            SaveTrackChange(director, timeline, track);
            return clip;
        }

        private static TimelineAsset CreateExampleTimelineIfMissing()
        {
            TimelineAsset existing = AssetDatabase.LoadAssetAtPath<TimelineAsset>(ExampleTimelinePath);
            if (existing != null)
            {
                existing.durationMode = TimelineAsset.DurationMode.FixedLength;
                existing.fixedDuration = 8.1d;
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                return existing;
            }

            CreatedExampleTracks.Clear();

            TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "LabResearchAuthoring";
            timeline.editorSettings.frameRate = 30f;
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = 8.1d;
            AssetDatabase.CreateAsset(timeline, ExampleTimelinePath);
            timeline.CreateMarkerTrack();

            GroupTrack characters = timeline.CreateTrack<GroupTrack>(null, "CHARACTERS");
            AnimationTrack lunaTrack = timeline.CreateTrack<AnimationTrack>(characters, "Luna Animation");
            AnimationTrack researcherTrack = timeline.CreateTrack<AnimationTrack>(characters, "Researcher Animation");
            AnimationTrack intruderTrack = timeline.CreateTrack<AnimationTrack>(characters, "Intruder Animation");
            GroupTrack cameraGroup = timeline.CreateTrack<GroupTrack>(null, "CAMERA");
            AnimationTrack cameraTrack = timeline.CreateTrack<AnimationTrack>(cameraGroup, "Camera Animation");
            GroupTrack audioGroup = timeline.CreateTrack<GroupTrack>(null, "AUDIO");
            AudioTrack bgmTrack = timeline.CreateTrack<AudioTrack>(audioGroup, "BGM");
            AudioTrack sfxTrack = timeline.CreateTrack<AudioTrack>(audioGroup, "SFX");

            foreach (TrackAsset track in new TrackAsset[]
                     {
                         lunaTrack, researcherTrack, intruderTrack, cameraTrack, bgmTrack, sfxTrack
                     })
                CreatedExampleTracks[track.name] = track;

            AddAnimationClip(lunaTrack, CreateMoveClip("Luna_Approach", new Vector3(-4.2f, -1.2f, 0f), new Vector3(-2.8f, -1.2f, 0f), 1.2f), 0.2d);
            AddAnimationClip(researcherTrack, CreateMoveClip("Researcher_Recoil", new Vector3(3.6f, -1.2f, 0f), new Vector3(4.15f, -1.2f, 0f), 0.35f), 4.2d);
            AddAnimationClip(intruderTrack, CreateMoveClip("Intruder_Enter", new Vector3(7.5f, -1.2f, 0f), new Vector3(4.8f, -1.2f, 0f), 0.7f), 3.8d);
            AddAnimationClip(cameraTrack, CreateMoveClip("Camera_Push", Vector3.zero, new Vector3(0.8f, 0.15f, 0f), 1.4f), 3.55d);

            LunaEffectMarker fadeIn = timeline.markerTrack.CreateMarker<LunaEffectMarker>(0.05d);
            fadeIn.effectType = LunaCutsceneEffectType.FadeIn;
            fadeIn.duration = 0.8f;
            fadeIn.eventKey = "lab_fade_in";

            LunaDialogueMarker firstLine = timeline.markerTrack.CreateMarker<LunaDialogueMarker>(1.5d);
            firstLine.dialogueId = "dlg_cutscene_lab_001";
            firstLine.line.speakerId = "researcher";
            firstLine.line.speakerKo = "연구원";
            firstLine.line.speakerEn = "Researcher";
            firstLine.line.textKo = "기억 동기화 수치가 안정권에 들어왔습니다.";
            firstLine.line.textEn = "The memory synchronization level has stabilized.";

            LunaEffectMarker flash = timeline.markerTrack.CreateMarker<LunaEffectMarker>(3.75d);
            flash.effectType = LunaCutsceneEffectType.Flash;
            flash.color = new Color(1f, 0.12f, 0.2f, 1f);
            flash.duration = 0.24f;
            flash.eventKey = "lab_alarm_flash";

            LunaEffectMarker shake = timeline.markerTrack.CreateMarker<LunaEffectMarker>(3.82d);
            shake.effectType = LunaCutsceneEffectType.CameraShake;
            shake.targetId = "camera_rig";
            shake.duration = 0.45f;
            shake.strength = 0.16f;
            shake.eventKey = "lab_alarm_shake";

            LunaDialogueMarker lunaLine = timeline.markerTrack.CreateMarker<LunaDialogueMarker>(5.3d);
            lunaLine.dialogueId = "dlg_cutscene_lab_002";
            lunaLine.line.speakerId = "luna";
            lunaLine.line.speakerKo = "루나";
            lunaLine.line.speakerEn = "L.U.N.A";
            lunaLine.line.textKo = "경고. 승인되지 않은 접근이 감지되었습니다.";
            lunaLine.line.textEn = "Warning. Unauthorized access detected.";

            LunaEffectMarker completionFlag = timeline.markerTrack.CreateMarker<LunaEffectMarker>(7.1d);
            completionFlag.effectType = LunaCutsceneEffectType.SetFlag;
            completionFlag.stringValue = "flag.lab_intrusion_seen = true";
            completionFlag.fireOnSkip = true;
            completionFlag.eventKey = "lab_intrusion_seen";

            LunaEffectMarker fadeOut = timeline.markerTrack.CreateMarker<LunaEffectMarker>(7.45d);
            fadeOut.effectType = LunaCutsceneEffectType.FadeOut;
            fadeOut.duration = 0.5f;
            fadeOut.fireOnSkip = true;
            fadeOut.eventKey = "lab_fade_out";

            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            return timeline;
        }

        private static LunaCutsceneDefinition CreateExampleDefinitionIfMissing()
        {
            LunaCutsceneDefinition existing = AssetDatabase.LoadAssetAtPath<LunaCutsceneDefinition>(ExampleDefinitionPath);
            if (existing != null)
                return existing;

            LunaCutsceneDefinition definition = ScriptableObject.CreateInstance<LunaCutsceneDefinition>();
            definition.Configure("cutscene_lab_research_authoring", "연구소 침입 테스트", "Laboratory Intrusion Test");
            definition.autoPlay = true;
            definition.skippable = true;
            definition.startFromBlack = true;
            definition.playMode = LunaCutscenePlayMode.Repeatable;
            definition.fadeToBlackOnEnd = true;
            definition.completionFlag = "flag.lab_cutscene_complete";
            definition.endBindings = new[]
            {
                new LunaCutsceneEndBinding
                {
                    targetId = "intruder", applyActive = true, active = true,
                    applyPosition = true, localPosition = new Vector3(4.8f, -1.2f, 0f)
                },
                new LunaCutsceneEndBinding
                {
                    targetId = "luna", applyActive = true, active = true,
                    applyPosition = true, localPosition = new Vector3(-2.8f, -1.2f, 0f)
                }
            };
            AssetDatabase.CreateAsset(definition, ExampleDefinitionPath);
            AssetDatabase.SaveAssets();
            return definition;
        }

        private static AnimationClip CreateMoveClip(string name, Vector3 from, Vector3 to, float duration)
        {
            string path = $"{ClipFolder}/{name}.anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null)
                return existing;

            AnimationClip clip = new() { name = name, frameRate = 30f };
            SetPositionCurve(clip, "m_LocalPosition.x", from.x, to.x, duration);
            SetPositionCurve(clip, "m_LocalPosition.y", from.y, to.y, duration);
            SetPositionCurve(clip, "m_LocalPosition.z", from.z, to.z, duration);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void SetPositionCurve(AnimationClip clip, string property, float from, float to, float duration)
        {
            AnimationCurve curve = AnimationCurve.EaseInOut(0f, from, duration, to);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), property), curve);
        }

        private static void AddAnimationClip(AnimationTrack track, AnimationClip clip, double start)
        {
            TimelineClip timelineClip = track.CreateClip(clip);
            timelineClip.start = start;
            timelineClip.duration = clip.length;
            timelineClip.displayName = clip.name;
        }

        private static GameObject CreateCharacter(Transform parent, string name, string id, Sprite sprite, Vector3 position, Color color, int order)
        {
            GameObject character = CreateSpriteObject(parent, name, id, sprite, position, new Vector3(1.45f, 3.8f, 1f), color, order);
            character.AddComponent<Animator>();
            EnsureSpeechAnchor(character, true);
            return character;
        }

        private static GameObject CreateSpriteObject(
            Transform parent, string name, string id, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
        {
            GameObject target = new(name, typeof(SpriteRenderer));
            target.transform.SetParent(parent, false);
            target.transform.localPosition = position;
            target.transform.localScale = scale;
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            AddBindingId(target, id);
            return target;
        }

        private static AudioSource CreateAudioSource(Transform parent, string name, string id)
        {
            GameObject target = new(name, typeof(AudioSource));
            target.transform.SetParent(parent, false);
            AudioSource source = target.GetComponent<AudioSource>();
            source.playOnAwake = false;
            AddBindingId(target, id);
            return source;
        }

        private static void AddBindingId(GameObject target, string id)
        {
            LunaCutsceneBindingId binding = target.AddComponent<LunaCutsceneBindingId>();
            binding.Configure(id);
        }

        private static LunaCutsceneDialogueUI CreateUi(Transform parent)
        {
            LunaCutsceneUiAssetBuilder.UiAssets assets = LunaCutsceneUiAssetBuilder.EnsureAssets();
            GameObject canvasObject = new("Cutscene_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(parent, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            Image fade = CreateImage(canvasObject.transform, "Fade", Stretch(), Color.black);
            fade.raycastTarget = false;
            Image flash = CreateImage(canvasObject.transform, "Flash", Stretch(), new Color(1f, 0f, 0f, 0f));
            flash.raycastTarget = false;
            RectTransform bubbleLayer = LunaCutsceneUiAssetBuilder.EnsureBubbleLayer(canvasObject.transform);
            TMP_Text status = CreateTmpText(canvasObject.transform, "Status",
                new RectLayout(new Vector2(0.018f, 0.84f), new Vector2(0.42f, 0.98f), Vector2.zero, Vector2.zero),
                16f, TextAlignmentOptions.TopLeft, new Color(0.55f, 0.95f, 0.92f), assets.style.font);

            flash.transform.SetAsLastSibling();
            fade.transform.SetAsLastSibling();

            LunaCutsceneDialogueUI ui = canvasObject.AddComponent<LunaCutsceneDialogueUI>();
            ui.Configure(bubbleLayer, assets.prefab, assets.style, fade, flash, status);
            return ui;
        }

        private static Image CreateImage(Transform parent, string name, RectLayout layout, Color color)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            ApplyRect(target.GetComponent<RectTransform>(), layout);
            Image image = target.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static TMP_Text CreateTmpText(
            Transform parent,
            string name,
            RectLayout layout,
            float size,
            TextAlignmentOptions alignment,
            Color color,
            TMP_FontAsset font)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            ApplyRect(target.GetComponent<RectTransform>(), layout);
            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static void ApplyRect(RectTransform rect, RectLayout layout)
        {
            rect.anchorMin = layout.anchorMin;
            rect.anchorMax = layout.anchorMax;
            rect.offsetMin = layout.offsetMin;
            rect.offsetMax = layout.offsetMax;
        }

        private static RectLayout Stretch()
        {
            return new RectLayout(Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void BindTrack(PlayableDirector director, TimelineAsset timeline, string trackName, UnityEngine.Object binding)
        {
            TrackAsset track = CreatedExampleTracks.TryGetValue(trackName, out TrackAsset created)
                ? created
                : EnumerateTracks(timeline).FirstOrDefault(value => value.name == trackName);
            if (track == null)
            {
                string available = string.Join(", ", EnumerateTracks(timeline).Select(value => $"{value.name}<{value.GetType().Name}>").ToArray());
                throw new InvalidOperationException($"Timeline 트랙을 찾을 수 없습니다: {trackName}. 현재 트랙: {available}");
            }
            director.SetGenericBinding(track, binding);
        }

        public static IEnumerable<TrackAsset> EnumerateTracks(TimelineAsset timeline)
        {
            if (timeline == null)
                yield break;
            HashSet<TrackAsset> yielded = new();
            string assetPath = AssetDatabase.GetAssetPath(timeline);
            if (!string.IsNullOrEmpty(assetPath))
            {
                foreach (TrackAsset track in AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<TrackAsset>())
                {
                    if (yielded.Add(track))
                        yield return track;
                }
            }
            foreach (TrackAsset track in Resources.FindObjectsOfTypeAll<TrackAsset>())
            {
                if (track != null && track.timelineAsset == timeline && yielded.Add(track))
                    yield return track;
            }
            foreach (TrackAsset root in timeline.GetRootTracks())
            {
                if (yielded.Add(root))
                    yield return root;
                foreach (TrackAsset child in EnumerateChildren(root))
                {
                    if (yielded.Add(child))
                        yield return child;
                }
            }
        }

        public static string MakeSafeId(string raw)
        {
            return SanitizeId(raw);
        }

        private static void SaveTrackChange(PlayableDirector director, TimelineAsset timeline, TrackAsset track)
        {
            EditorUtility.SetDirty(track);
            EditorUtility.SetDirty(timeline);
            EditorUtility.SetDirty(director);
            EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
            AssetDatabase.SaveAssets();
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }

        private static IEnumerable<TrackAsset> EnumerateChildren(TrackAsset parent)
        {
            foreach (TrackAsset child in parent.GetChildTracks())
            {
                yield return child;
                foreach (TrackAsset descendant in EnumerateChildren(child))
                    yield return descendant;
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder(Root, "Authoring");
            EnsureFolder(Root + "/Authoring", "Timelines");
            EnsureFolder(Root + "/Authoring", "Definitions");
            EnsureFolder(Root + "/Authoring", "AnimationClips");
            EnsureFolder(Root + "/Authoring", "Generated");
            EnsureFolder(Root + "/Authoring", "UI");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static string SanitizeId(string raw)
        {
            string value = (raw ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '_');
            return new string(value.Where(character => char.IsLetterOrDigit(character) || character == '_').ToArray());
        }

        private readonly struct RectLayout
        {
            public readonly Vector2 anchorMin;
            public readonly Vector2 anchorMax;
            public readonly Vector2 offsetMin;
            public readonly Vector2 offsetMax;

            public RectLayout(Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
            {
                this.anchorMin = anchorMin;
                this.anchorMax = anchorMax;
                this.offsetMin = offsetMin;
                this.offsetMax = offsetMax;
            }
        }
    }
}
