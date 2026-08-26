using System;
using System.Linq;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    public sealed class LunaCutsceneAuthoringWindow : EditorWindow
    {
        private string newCutsceneId = "cutscene_new";
        private string newBindingId = "actor_id";
        private GameObject bindingTarget;
        private Vector2 scroll;
        private string validationReport = "아직 검사하지 않았습니다.";
        private MessageType validationType = MessageType.Info;

        [MenuItem("Window/Project L.U.N.A/Cutscene Authoring Studio")]
        [MenuItem("Project L.U.N.A/Cutscene Authoring/Open Studio")]
        public static void Open()
        {
            GetWindow<LunaCutsceneAuthoringWindow>("L.U.N.A Cutscene");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            EditorGUILayout.Space(8f);

            LunaCutsceneDirector runtime = FindRuntime();
            PlayableDirector director = runtime != null ? runtime.Director : TimelineEditor.inspectedDirector;
            TimelineAsset timeline = director != null ? director.playableAsset as TimelineAsset : null;

            DrawConnectionSummary(runtime, director, timeline);
            EditorGUILayout.Space(10f);
            DrawProjectSetup(runtime, director);
            EditorGUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(runtime == null || director == null || timeline == null))
            {
                DrawPlayheadTools(runtime, director, timeline);
                EditorGUILayout.Space(10f);
                DrawBindingTools(runtime, director, timeline);
                EditorGUILayout.Space(10f);
                DrawValidation(runtime);
            }

            EditorGUILayout.EndScrollView();
        }

        private static void DrawHeader()
        {
            GUIStyle title = new(EditorStyles.boldLabel) { fontSize = 17 };
            EditorGUILayout.LabelField("PROJECT L.U.N.A — CUTSCENE AUTHORING", title);
            EditorGUILayout.LabelField(
                "Timeline 위에서 애니메이션 클립을 배치하고, 현재 재생 헤드에 대사·효과·스프라이트 교체를 추가하는 제작 도구입니다.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox(
                "권장 순서: 제작 씬 열기 → Timeline 열기 → 트랙에 오브젝트 바인딩 → 클립 배치 → 마커 추가 → 검사 → Play 재생",
                MessageType.None);
        }

        private static void DrawConnectionSummary(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("현재 연결", EditorStyles.boldLabel);
            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.ObjectField("Cutscene Director", runtime, typeof(LunaCutsceneDirector), true);
                EditorGUILayout.ObjectField("Playable Director", director, typeof(PlayableDirector), true);
                EditorGUILayout.ObjectField("Timeline Asset", timeline, typeof(TimelineAsset), false);
                EditorGUILayout.ObjectField("Definition", runtime != null ? runtime.Definition : null, typeof(LunaCutsceneDefinition), false);
            }
        }

        private void DrawProjectSetup(LunaCutsceneDirector runtime, PlayableDirector director)
        {
            EditorGUILayout.LabelField("제작 씬과 에셋", EditorStyles.boldLabel);
            if (GUILayout.Button("예제 제작 씬 생성 / 열기", GUILayout.Height(30f)))
                LunaCutsceneAuthoringBuilder.CreateOrOpenAuthoringLab();

            using (new EditorGUILayout.HorizontalScope())
            {
                newCutsceneId = EditorGUILayout.TextField("새 컷씬 ID", newCutsceneId);
                if (GUILayout.Button("빈 에셋 만들기", GUILayout.Width(120f)))
                {
                    try
                    {
                        (TimelineAsset timeline, LunaCutsceneDefinition definition) =
                            LunaCutsceneAuthoringBuilder.CreateBlankAssets(newCutsceneId);
                        if (runtime != null && director != null)
                        {
                            Undo.RecordObject(director, "Assign L.U.N.A Cutscene Timeline");
                            Undo.RecordObject(runtime, "Assign L.U.N.A Cutscene Definition");
                            director.playableAsset = timeline;
                            runtime.Configure(definition, director, runtime.BindingRegistry, runtime.DialogueUI);
                            EditorUtility.SetDirty(director);
                            EditorUtility.SetDirty(runtime);
                            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
                        }
                        Selection.activeObject = timeline;
                        validationReport = $"생성 완료: {timeline.name}";
                        validationType = MessageType.Info;
                    }
                    catch (Exception exception)
                    {
                        validationReport = exception.Message;
                        validationType = MessageType.Error;
                    }
                }
            }

            if (director != null && GUILayout.Button("이 Director를 Timeline 창에서 열기"))
            {
                Selection.activeGameObject = director.gameObject;
                EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            }
        }

        private void DrawPlayheadTools(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("재생 헤드에 마커 추가", EditorStyles.boldLabel);
            double time = director.time;
            EditorGUILayout.LabelField("현재 시간", $"{time:0.000} sec");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("＋ 대사", GUILayout.Height(28f)))
                    CreateDialogueMarker(timeline, time);
                if (GUILayout.Button("＋ 효과", GUILayout.Height(28f)))
                    CreateEffectMarker(timeline, time);
                if (GUILayout.Button("＋ 스프라이트", GUILayout.Height(28f)))
                    CreateSpriteMarker(timeline, time);
            }

            EditorGUILayout.HelpBox(
                "대사 마커는 Timeline을 멈추고 입력 후 같은 시간부터 재개합니다. 스킵 후에도 남아야 하는 플래그·최종 상태 효과만 fire_on_skip을 사용하세요.",
                MessageType.Info);
        }

        private void DrawBindingTools(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("게임 오브젝트 바인딩", EditorStyles.boldLabel);
            bindingTarget = (GameObject)EditorGUILayout.ObjectField("대상 오브젝트", bindingTarget, typeof(GameObject), true);
            newBindingId = EditorGUILayout.TextField("고정 Binding ID", newBindingId);

            using (new EditorGUI.DisabledScope(bindingTarget == null || string.IsNullOrWhiteSpace(newBindingId)))
            {
                if (GUILayout.Button("선택 오브젝트를 Binding Registry에 등록"))
                    RegisterBinding(runtime, bindingTarget, newBindingId);
            }

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(bindingTarget == null))
            {
                if (GUILayout.Button("Animation Track 생성 + 연결"))
                    CreateAnimationTrack(director, timeline, bindingTarget);
                if (GUILayout.Button("Audio Track 생성 + 연결"))
                    CreateAudioTrack(director, timeline, bindingTarget);
            }
            EditorGUILayout.HelpBox(
                "Binding ID는 대사와 별개입니다. 효과·스프라이트 마커의 target_id 및 Definition의 종료 상태가 씬 오브젝트를 찾을 때 사용합니다.",
                MessageType.None);
        }

        private void DrawValidation(LunaCutsceneDirector runtime)
        {
            EditorGUILayout.LabelField("제작 데이터 검사", EditorStyles.boldLabel);
            if (GUILayout.Button("Timeline 베이크 + 현재 컷씬 전체 검사", GUILayout.Height(30f)))
            {
                try
                {
                    LunaCutsceneAuthoringBaker.Bake(runtime);
                    LunaCutsceneAuthoringValidator.Result result = LunaCutsceneAuthoringValidator.Validate(runtime);
                    validationReport = result.ToReport();
                    validationType = result.IsValid ? MessageType.Info : MessageType.Error;
                }
                catch (Exception exception)
                {
                    validationReport = exception.Message;
                    validationType = MessageType.Error;
                }
            }
            EditorGUILayout.HelpBox(validationReport, validationType);
        }

        private static void CreateDialogueMarker(TimelineAsset timeline, double time)
        {
            PrepareMarkerTrack(timeline);
            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Dialogue Marker");
            LunaDialogueMarker marker = timeline.markerTrack.CreateMarker<LunaDialogueMarker>(time);
            marker.dialogueId = $"dlg_{timeline.name.ToLowerInvariant()}_{Mathf.RoundToInt((float)(time * 1000d)):000000}";
            marker.line.speakerId = "speaker_id";
            marker.line.speakerKo = "화자";
            marker.line.speakerEn = "Speaker";
            marker.line.textKo = "한국어 대사를 입력하세요.";
            marker.line.textEn = "Enter the English dialogue.";
            FinishMarkerCreation(timeline, marker);
        }

        private static void CreateEffectMarker(TimelineAsset timeline, double time)
        {
            PrepareMarkerTrack(timeline);
            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Effect Marker");
            LunaEffectMarker marker = timeline.markerTrack.CreateMarker<LunaEffectMarker>(time);
            marker.effectType = LunaCutsceneEffectType.Flash;
            marker.color = Color.white;
            marker.duration = 0.2f;
            marker.eventKey = $"fx_{Mathf.RoundToInt((float)(time * 1000d)):000000}";
            FinishMarkerCreation(timeline, marker);
        }

        private static void CreateSpriteMarker(TimelineAsset timeline, double time)
        {
            PrepareMarkerTrack(timeline);
            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Sprite Marker");
            LunaSpriteSwapMarker marker = timeline.markerTrack.CreateMarker<LunaSpriteSwapMarker>(time);
            marker.targetId = "actor_id";
            marker.eventKey = $"sprite_{Mathf.RoundToInt((float)(time * 1000d)):000000}";
            FinishMarkerCreation(timeline, marker);
        }

        private static void PrepareMarkerTrack(TimelineAsset timeline)
        {
            if (timeline.markerTrack == null)
                timeline.CreateMarkerTrack();
        }

        private static void FinishMarkerCreation(TimelineAsset timeline, UnityEngine.Object marker)
        {
            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            Selection.activeObject = marker;
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }

        private static void RegisterBinding(LunaCutsceneDirector runtime, GameObject target, string id)
        {
            LunaCutsceneBindingId binding = target.GetComponent<LunaCutsceneBindingId>();
            if (binding == null)
                binding = Undo.AddComponent<LunaCutsceneBindingId>(target);
            Undo.RecordObject(binding, "Configure L.U.N.A Binding ID");
            binding.Configure(id.Trim());
            runtime.BindingRegistry.RebuildFromChildren();
            EditorUtility.SetDirty(binding);
            EditorUtility.SetDirty(runtime.BindingRegistry);
            EditorSceneManager.MarkSceneDirty(target.scene);
        }

        private static void CreateAnimationTrack(PlayableDirector director, TimelineAsset timeline, GameObject target)
        {
            Animator animator = target.GetComponent<Animator>();
            if (animator == null)
                animator = Undo.AddComponent<Animator>(target);
            Undo.RegisterCompleteObjectUndo(timeline, "Create L.U.N.A Animation Track");
            AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, target.name + " Animation");
            director.SetGenericBinding(track, animator);
            SaveTrackChange(director, timeline, track);
        }

        private static void CreateAudioTrack(PlayableDirector director, TimelineAsset timeline, GameObject target)
        {
            AudioSource source = target.GetComponent<AudioSource>();
            if (source == null)
                source = Undo.AddComponent<AudioSource>(target);
            source.playOnAwake = false;
            Undo.RegisterCompleteObjectUndo(timeline, "Create L.U.N.A Audio Track");
            AudioTrack track = timeline.CreateTrack<AudioTrack>(null, target.name + " Audio");
            director.SetGenericBinding(track, source);
            SaveTrackChange(director, timeline, track);
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

        private static LunaCutsceneDirector FindRuntime()
        {
            LunaCutsceneDirector selected = Selection.activeGameObject != null
                ? Selection.activeGameObject.GetComponentInParent<LunaCutsceneDirector>()
                : null;
            if (selected != null)
                return selected;
            return UnityEngine.Object.FindFirstObjectByType<LunaCutsceneDirector>();
        }
    }
}
