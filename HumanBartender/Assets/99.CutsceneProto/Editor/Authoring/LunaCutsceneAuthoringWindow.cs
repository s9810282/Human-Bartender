using System;
using System.Collections.Generic;
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
        private enum WorkflowStep
        {
            Setup,
            Timeline,
            Preview,
            Finish
        }

        private static readonly string[] WorkflowLabels =
        {
            "1. 씬·배우",
            "2. 이동·마커",
            "3. 미리보기",
            "4. 완료·검사"
        };

        private static readonly string[] BeginnerEffectLabels =
        {
            "장면 나타내기 (Fade In)",
            "장면 어둡게 덮기 (Fade Out)",
            "화면 번쩍임 (Flash)",
            "카메라 흔들기",
            "오브젝트 켜기·끄기",
            "스프라이트 색 바꾸기",
            "효과음 재생",
            "게임 상태 플래그 기록"
        };

        [SerializeField] private string newCutsceneId = "cutscene_new";
        [SerializeField] private string newBindingId = "actor_id";
        [SerializeField] private string newAnchorId = "actor_start";
        [SerializeField] private GameObject bindingTarget;
        [SerializeField] private LunaCutsceneAnchor fromAnchor;
        [SerializeField] private LunaCutsceneAnchor toAnchor;
        [SerializeField] private bool autoChainMove = true;
        [SerializeField] private float moveSpeed = 2.5f;
        [SerializeField] private bool previewLoop;
        [SerializeField] private float previewSpeed = 1f;
        [SerializeField] private double previewStart;
        [SerializeField] private double previewEnd = -1d;
        private Vector2 scroll;
        private string validationReport = "아직 검사하지 않았습니다.";
        private MessageType validationType = MessageType.Info;
        [SerializeField] private WorkflowStep workflowStep;
        [SerializeField] private bool followHierarchySelection = true;
        [SerializeField] private bool beginnerMode = true;
        [SerializeField] private bool showAdvancedSetup;
        [SerializeField] private bool showAdvancedTrackButtons;

        [MenuItem("Window/Project L.U.N.A/Cutscene Authoring Studio")]
        [MenuItem("Project L.U.N.A/Cutscene Authoring/Open Studio")]
        public static void Open()
        {
            LunaCutsceneAuthoringWindow window = GetWindow<LunaCutsceneAuthoringWindow>("L.U.N.A Cutscene");
            window.minSize = new Vector2(520f, 540f);
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlaying)
                    LunaCutsceneAuthoringBuilder.EnsureSceneAuthoringHelpers();
            };
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            beginnerMode = EditorGUILayout.ToggleLeft(
                new GUIContent("초보자 안내 모드", "쉬운 용어, 단계별 준비 상태, Studio 내부 마커 편집기를 표시합니다."),
                beginnerMode);
            EditorGUILayout.Space(8f);

            LunaCutsceneDirector runtime = FindRuntime();
            PlayableDirector director = runtime != null ? runtime.Director : TimelineEditor.inspectedDirector;
            TimelineAsset timeline = director != null ? director.playableAsset as TimelineAsset : null;

            DrawWorkflowSummary(runtime, director, timeline);
            if (beginnerMode)
            {
                EditorGUILayout.Space(6f);
                DrawBeginnerChecklist(runtime, director, timeline);
            }
            EditorGUILayout.Space(6f);
            workflowStep = (WorkflowStep)GUILayout.Toolbar((int)workflowStep, WorkflowLabels, GUILayout.Height(28f));
            EditorGUILayout.Space(10f);

            switch (workflowStep)
            {
                case WorkflowStep.Setup:
                    if (!beginnerMode)
                    {
                        DrawConnectionSummary(runtime, director, timeline);
                        EditorGUILayout.Space(10f);
                    }
                    DrawProjectSetup(runtime, director);
                    EditorGUILayout.Space(10f);
                    using (new EditorGUI.DisabledScope(runtime == null || director == null || timeline == null))
                        DrawActorSetup(runtime, director, timeline);
                    break;
                case WorkflowStep.Timeline:
                    using (new EditorGUI.DisabledScope(runtime == null || director == null || timeline == null))
                    {
                        DrawMovementTools(runtime, director, timeline);
                        EditorGUILayout.Space(10f);
                        DrawPlayheadTools(runtime, director, timeline);
                    }
                    break;
                case WorkflowStep.Preview:
                    using (new EditorGUI.DisabledScope(runtime == null || director == null || timeline == null))
                        DrawPreviewTools(director, timeline);
                    break;
                case WorkflowStep.Finish:
                    using (new EditorGUI.DisabledScope(runtime == null || director == null || timeline == null))
                    {
                        DrawEndStateTools(runtime);
                        EditorGUILayout.Space(10f);
                        DrawValidation(runtime);
                    }
                    break;
            }

            EditorGUILayout.EndScrollView();
        }

        private void OnDisable()
        {
            LunaCutscenePreviewController.StopAndRestore();
        }

        private void OnSelectionChange()
        {
            if (!followHierarchySelection || Selection.activeGameObject == null)
                return;
            GameObject selected = Selection.activeGameObject;
            if (selected.GetComponent<LunaCutsceneAnchor>() != null
                || selected.GetComponent<LunaCutsceneDirector>() != null)
                return;
            AdoptBindingTarget(selected);
            Repaint();
        }

        private static void DrawHeader()
        {
            GUIStyle title = new(EditorStyles.boldLabel) { fontSize = 17 };
            EditorGUILayout.LabelField("PROJECT L.U.N.A — CUTSCENE AUTHORING", title);
            EditorGUILayout.LabelField(
                "Timeline에서 배우 이동·애니메이션·대사·효과를 하나의 시간축으로 조립하는 독립 프로토타입 제작 도구입니다.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox(
                "권장 순서: 제작 씬 → 배우 ID 등록 → 배우 트랙 세트 → 앵커·이동 클립 → 대사·효과 마커 → 종료 상태 캡처 → 검사",
                MessageType.None);
        }

        private static void DrawWorkflowSummary(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            int bindingCount = runtime?.BindingRegistry?.Bindings?.Count ?? 0;
            int anchorCount = UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None).Length;
            int markerCount = timeline?.markerTrack?.GetMarkers().Count() ?? 0;
            int actorCount = timeline?.GetRootTracks()
                .OfType<GroupTrack>()
                .Count(track => track.name.StartsWith("ACTOR_", StringComparison.Ordinal)) ?? 0;
            double duration = timeline?.duration ?? 0d;

            string connection = runtime != null && director != null && timeline != null && runtime.Definition != null
                ? "연결 완료"
                : "연결 필요";
            EditorGUILayout.HelpBox(
                $"{connection}  ·  배우 세트 {actorCount}  ·  바인딩 {bindingCount}  ·  앵커 {anchorCount}  ·  마커 {markerCount}  ·  {duration:0.00}초",
                connection == "연결 완료" ? MessageType.Info : MessageType.Warning);

            string nextAction;
            if (runtime == null || director == null || timeline == null || runtime.Definition == null)
                nextAction = "다음 작업: 1단계에서 예제 씬을 열거나 '새 컷씬 한 번에 만들기'를 누르세요.";
            else if (bindingCount == 0 || actorCount == 0)
                nextAction = "다음 작업: 배우를 선택하고 '선택 배우 빠른 설정'을 누르세요.";
            else if (anchorCount < 2)
                nextAction = "다음 작업: 이동할 도착 위치에 앵커를 하나 더 만드세요.";
            else if (markerCount == 0)
                nextAction = "다음 작업: 재생 헤드에 첫 대사 또는 효과 마커를 추가하세요.";
            else if ((runtime.Definition.bakedEvents?.Length ?? 0) != markerCount
                     || !string.Equals(
                         LunaCutsceneAuthoringBaker.ComputeTimelineHash(timeline),
                         runtime.Definition.bakedTimelineHash,
                         StringComparison.Ordinal))
                nextAction = "다음 작업: Timeline이 바뀌었습니다. 4단계에서 저장·베이크·검사하세요.";
            else
                nextAction = "다음 작업: 편집 미리보기 후 정상 재생과 스킵 결과를 확인하세요.";
            EditorGUILayout.LabelField(nextAction, EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawBeginnerChecklist(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("처음 만드는 사람용 준비 상태", EditorStyles.boldLabel);

            bool coreReady = runtime != null
                             && director != null
                             && timeline != null
                             && runtime.Definition != null
                             && runtime.BindingRegistry != null
                             && runtime.DialogueUI != null;
            bool cameraReady = UnityEngine.Object.FindFirstObjectByType<Camera>() != null;
            IEnumerable<LunaCutsceneBindingEntry> bindingEntries =
                runtime?.BindingRegistry?.Bindings ?? Array.Empty<LunaCutsceneBindingEntry>();
            LunaCutsceneBindingEntry[] actorEntries = bindingEntries
                .Where(entry => entry?.target != null
                                && entry.target.GetComponent<Animator>() != null
                                && entry.target.GetComponentInChildren<SpriteRenderer>(true) != null)
                .ToArray();
            int actorCount = actorEntries.Length;
            bool actorsReady = actorCount > 0
                               && actorEntries.All(entry =>
                                   entry.target.GetComponentInChildren<LunaCutsceneSpeechAnchor>(true) != null);
            int anchorCount = UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None).Length;
            int markerCount = timeline?.markerTrack?.GetMarkers().Count() ?? 0;
            bool bakeReady = coreReady
                             && string.Equals(
                                 LunaCutsceneAuthoringBaker.ComputeTimelineHash(timeline),
                                 runtime.Definition.bakedTimelineHash,
                                 StringComparison.Ordinal);

            DrawChecklistRow(coreReady && cameraReady, "1. 제작 Scene 준비", coreReady && cameraReady
                ? "필수 연결과 픽셀 카메라가 준비됐습니다."
                : "Scene·Timeline·카메라·말풍선 연결이 필요합니다.");
            DrawChecklistRow(actorsReady, "2. 배우 준비", actorsReady
                ? $"배우 {actorCount}명과 머리 위 말풍선 위치가 준비됐습니다."
                : "배우를 선택하고 '선택 배우 빠른 설정'을 누르세요.");
            DrawChecklistRow(anchorCount >= 2, "3. 이동 위치", anchorCount >= 2
                ? $"이동 위치 {anchorCount}개가 있습니다."
                : "시작점과 도착점에 이동 위치를 만드세요.");
            DrawChecklistRow(markerCount > 0, "4. 대사·효과", markerCount > 0
                ? $"마커 {markerCount}개가 있습니다."
                : "Timeline 재생 헤드에서 첫 대사나 효과를 추가하세요.");
            DrawChecklistRow(bakeReady, "5. 실행 데이터", bakeReady
                ? "현재 Timeline과 실행 데이터가 일치합니다."
                : "마지막 단계에서 저장·실행 데이터 만들기·검사를 실행하세요.");

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("1단계로 이동"))
                    workflowStep = WorkflowStep.Setup;
                if (GUILayout.Button("2단계로 이동"))
                    workflowStep = WorkflowStep.Timeline;
                using (new EditorGUI.DisabledScope(runtime == null))
                {
                    if (GUILayout.Button("필수 연결 자동 복구"))
                        RunUserAction(
                            LunaCutsceneAuthoringBuilder.EnsureSceneAuthoringHelpers,
                            "UI·말풍선 앵커·픽셀 카메라 연결을 점검하고 복구했습니다.");
                }
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawChecklistRow(bool complete, string title, string description)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(complete ? "✓" : "!", GUILayout.Width(18f));
                EditorGUILayout.LabelField(title, complete ? EditorStyles.miniBoldLabel : EditorStyles.boldLabel, GUILayout.Width(112f));
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            }
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

            newCutsceneId = EditorGUILayout.TextField(
                new GUIContent(beginnerMode ? "새 컷씬 영문 ID" : "새 컷씬 ID", "파일명과 저장 데이터에 사용하는 고정 ID입니다. 영문 소문자와 숫자, 밑줄만 사용합니다."),
                newCutsceneId);
            if (GUILayout.Button("새 컷씬 한 번에 만들기", GUILayout.Height(34f)))
            {
                RunUserAction(() =>
                {
                    LunaCutsceneAuthoringBuilder.CreateNewAuthoringScene(newCutsceneId);
                    bindingTarget = null;
                    fromAnchor = null;
                    toAnchor = null;
                    workflowStep = WorkflowStep.Setup;
                }, $"새 컷씬 제작 Scene 준비 완료: {newCutsceneId}");
            }
            EditorGUILayout.HelpBox(
                "Scene·Timeline·컷씬 설정·픽셀 카메라·말풍선 UI·오디오를 한 번에 만들고 연결합니다. 생성 후 배경과 배우를 Scene에 배치하세요.",
                MessageType.Info);

            showAdvancedSetup = EditorGUILayout.Foldout(showAdvancedSetup, "고급: 현재 Scene에 에셋만 만들기·연결하기", true);
            if (showAdvancedSetup)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Timeline·Definition만 만들기", GUILayout.Width(210f)))
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
            }

            if (director != null && GUILayout.Button(beginnerMode ? "이 컷씬의 시간표 열기" : "이 Director를 Timeline 창에서 열기"))
            {
                Selection.activeGameObject = director.gameObject;
                EditorApplication.ExecuteMenuItem("Window/Sequencing/Timeline");
            }

            if (!beginnerMode || showAdvancedSetup)
            {
                if (runtime != null && GUILayout.Button("현재 씬 UI·말풍선 앵커·픽셀 카메라 보강"))
                    LunaCutsceneAuthoringBuilder.EnsureSceneAuthoringHelpers();

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("UI 프리팹·스타일 생성/복구"))
                        LunaCutsceneUiAssetBuilder.RepairAssetsAndScene();
                    if (GUILayout.Button("UI Style 열기"))
                    {
                        LunaCutsceneUiAssetBuilder.UiAssets assets = LunaCutsceneUiAssetBuilder.EnsureAssets();
                        Selection.activeObject = assets.style;
                        EditorGUIUtility.PingObject(assets.style);
                    }
                }

                if (GUILayout.Button("시네마틱 연출 Preset 열기"))
                {
                    LunaCutsceneUiAssetBuilder.UiAssets assets = LunaCutsceneUiAssetBuilder.EnsureAssets();
                    Selection.activeObject = assets.presentationPreset;
                    EditorGUIUtility.PingObject(assets.presentationPreset);
                }
            }

            if (GUILayout.Button("사용 가이드 에셋 보기"))
            {
                TextAsset guide = AssetDatabase.LoadAssetAtPath<TextAsset>(LunaCutsceneAuthoringBuilder.Root + "/AUTHORING_GUIDE.md");
                if (guide != null)
                {
                    Selection.activeObject = guide;
                    EditorGUIUtility.PingObject(guide);
                }
            }
        }

        private void DrawPreviewTools(PlayableDirector director, TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("편집 모드 미리보기", EditorStyles.boldLabel);
            double duration = Math.Max(0.01d, timeline.duration);
            if (previewEnd <= previewStart || previewEnd > duration)
                previewEnd = duration;

            double nextTime = EditorGUILayout.Slider("현재 시간", (float)director.time, 0f, (float)duration);
            if (Math.Abs(nextTime - director.time) > 0.0001d)
                LunaCutscenePreviewController.EvaluateAt(director, nextTime);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("◀ 1프레임"))
                    LunaCutscenePreviewController.Step(director, -1, (float)timeline.editorSettings.frameRate);
                if (GUILayout.Button("재생"))
                    LunaCutscenePreviewController.Play(director, previewStart, previewEnd, previewLoop, previewSpeed);
                if (GUILayout.Button("정지·원상복구"))
                    LunaCutscenePreviewController.StopAndRestore();
                if (GUILayout.Button("1프레임 ▶"))
                    LunaCutscenePreviewController.Step(director, 1, (float)timeline.editorSettings.frameRate);
            }

            previewLoop = EditorGUILayout.Toggle("구간 반복", previewLoop);
            previewStart = EditorGUILayout.DoubleField("구간 시작", previewStart);
            previewEnd = EditorGUILayout.DoubleField("구간 종료", previewEnd);
            previewSpeed = EditorGUILayout.Slider("미리보기 배속", previewSpeed, 0.1f, 4f);
            LunaCutsceneSceneTools.ShowGuides = EditorGUILayout.Toggle("씬 앵커·이동·카메라 가이드", LunaCutsceneSceneTools.ShowGuides);

            EditorGUILayout.HelpBox(
                "편집 미리보기는 이동·Animation·카메라를 확인합니다. 대사 정지·효과·스킵은 Play 모드 자동 검증으로 확인하세요.",
                MessageType.None);
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

            if (beginnerMode)
            {
                EditorGUILayout.Space(8f);
                DrawSelectedMarkerQuickEditor(runtime, timeline);
            }
        }

        private void DrawSelectedMarkerQuickEditor(LunaCutsceneDirector runtime, TimelineAsset timeline)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("선택한 대사·효과 바로 편집", EditorStyles.boldLabel);

            UnityEngine.Object[] markerObjects = timeline.markerTrack == null
                ? Array.Empty<UnityEngine.Object>()
                : timeline.markerTrack.GetMarkers()
                    .Where(marker => marker is LunaDialogueMarker
                                     || marker is LunaEffectMarker
                                     || marker is LunaSpriteSwapMarker)
                    .OrderBy(marker => marker.time)
                    .Cast<UnityEngine.Object>()
                    .ToArray();
            string[] markerLabels = new[] { "— 편집할 마커 선택 —" }
                .Concat(markerObjects.Select(GetMarkerLabel))
                .ToArray();
            int selectedIndex = Array.IndexOf(markerObjects, Selection.activeObject) + 1;
            if (selectedIndex < 0)
                selectedIndex = 0;
            int nextIndex = EditorGUILayout.Popup("마커 목록", selectedIndex, markerLabels);
            if (nextIndex != selectedIndex)
            {
                Selection.activeObject = nextIndex <= 0 ? null : markerObjects[nextIndex - 1];
                selectedIndex = nextIndex;
            }

            UnityEngine.Object selected = selectedIndex <= 0 ? null : markerObjects[selectedIndex - 1];
            switch (selected)
            {
                case LunaDialogueMarker dialogue:
                    DrawDialogueQuickEditor(runtime, timeline, dialogue);
                    break;
                case LunaEffectMarker effect:
                    DrawEffectQuickEditor(runtime, timeline, effect);
                    break;
                case LunaSpriteSwapMarker sprite:
                    DrawSpriteQuickEditor(runtime, timeline, sprite);
                    break;
                default:
                    EditorGUILayout.HelpBox(
                        "위 목록에서 마커를 고르거나 Timeline의 마커를 선택하세요. 새 마커를 추가하면 자동으로 선택됩니다.",
                        MessageType.None);
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        private static void DrawDialogueQuickEditor(
            LunaCutsceneDirector runtime,
            TimelineAsset timeline,
            LunaDialogueMarker marker)
        {
            SerializedObject serialized = new(marker);
            serialized.Update();
            SerializedProperty line = serialized.FindProperty("line");
            SerializedProperty speakerId = line.FindPropertyRelative("speakerId");
            SerializedProperty speakerKo = line.FindPropertyRelative("speakerKo");
            SerializedProperty speakerEn = line.FindPropertyRelative("speakerEn");
            SerializedProperty textKo = line.FindPropertyRelative("textKo");
            SerializedProperty textEn = line.FindPropertyRelative("textEn");
            SerializedProperty advanceMode = serialized.FindProperty("advanceMode");
            SerializedProperty autoDelay = serialized.FindProperty("autoDelay");

            EditorGUI.BeginChangeCheck();
            DrawBindingIdPopup(speakerId, runtime?.BindingRegistry, "말하는 배우");
            EditorGUILayout.PropertyField(speakerKo, new GUIContent("화자명 (한국어)"));
            EditorGUILayout.PropertyField(speakerEn, new GUIContent("화자명 (영어)"));
            EditorGUILayout.PropertyField(textKo, new GUIContent("한국어 대사"), true);
            EditorGUILayout.PropertyField(textEn, new GUIContent("영어 대사"), true);
            int mode = EditorGUILayout.Popup("넘어가는 방식", advanceMode.enumValueIndex, new[] { "플레이어 입력", "자동 진행" });
            advanceMode.enumValueIndex = mode;
            if ((LunaDialogueAdvanceMode)mode == LunaDialogueAdvanceMode.Auto)
                EditorGUILayout.PropertyField(autoDelay, new GUIContent("자동 진행 대기 시간 (초)"));
            EditorGUILayout.PropertyField(serialized.FindProperty("pauseTimeline"), new GUIContent("대사 중 연출 일시정지"));
            EditorGUILayout.PropertyField(serialized.FindProperty("dialogueId"), new GUIContent("대사 고정 ID"));
            ApplyQuickEditorChanges(serialized, marker, timeline, EditorGUI.EndChangeCheck());

            if (string.IsNullOrWhiteSpace(marker.line?.textKo) || string.IsNullOrWhiteSpace(marker.line?.textEn))
                EditorGUILayout.HelpBox("한국어와 영어 대사를 모두 입력해야 합니다.", MessageType.Error);
            else if (marker.line.textKo.Contains("입력하세요", StringComparison.Ordinal)
                     || marker.line.textEn.Contains("Enter the English", StringComparison.Ordinal))
                EditorGUILayout.HelpBox("기본 안내 문장이 남아 있습니다. 실제 대사로 교체하세요.", MessageType.Warning);
        }

        private static void DrawEffectQuickEditor(
            LunaCutsceneDirector runtime,
            TimelineAsset timeline,
            LunaEffectMarker marker)
        {
            SerializedObject serialized = new(marker);
            serialized.Update();
            SerializedProperty effectType = serialized.FindProperty("effectType");

            EditorGUI.BeginChangeCheck();
            int typeIndex = EditorGUILayout.Popup("효과 종류", effectType.enumValueIndex, BeginnerEffectLabels);
            effectType.enumValueIndex = typeIndex;
            LunaCutsceneEffectType type = (LunaCutsceneEffectType)typeIndex;
            SerializedProperty targetId = serialized.FindProperty("targetId");
            SerializedProperty stringValue = serialized.FindProperty("stringValue");
            SerializedProperty color = serialized.FindProperty("color");
            SerializedProperty duration = serialized.FindProperty("duration");

            switch (type)
            {
                case LunaCutsceneEffectType.FadeIn:
                case LunaCutsceneEffectType.FadeOut:
                    EditorGUILayout.PropertyField(duration, new GUIContent("진행 시간 (초)"));
                    break;
                case LunaCutsceneEffectType.Flash:
                    EditorGUILayout.PropertyField(color, new GUIContent("번쩍임 색"));
                    EditorGUILayout.PropertyField(duration, new GUIContent("진행 시간 (초)"));
                    break;
                case LunaCutsceneEffectType.CameraShake:
                    DrawBindingIdPopup(targetId, runtime?.BindingRegistry, "흔들 카메라");
                    EditorGUILayout.PropertyField(duration, new GUIContent("흔들 시간 (초)"));
                    EditorGUILayout.PropertyField(serialized.FindProperty("strength"), new GUIContent("흔들림 세기"));
                    break;
                case LunaCutsceneEffectType.SetActive:
                    DrawBindingIdPopup(targetId, runtime?.BindingRegistry, "대상 오브젝트");
                    bool active = !string.Equals(stringValue.stringValue, "false", StringComparison.OrdinalIgnoreCase);
                    stringValue.stringValue = EditorGUILayout.Toggle("오브젝트 켜기", active) ? "true" : "false";
                    break;
                case LunaCutsceneEffectType.SetSpriteColor:
                    DrawBindingIdPopup(targetId, runtime?.BindingRegistry, "대상 스프라이트");
                    EditorGUILayout.PropertyField(color, new GUIContent("바꿀 색"));
                    break;
                case LunaCutsceneEffectType.PlaySfx:
                    EditorGUILayout.PropertyField(serialized.FindProperty("audioClip"), new GUIContent("효과음 파일"));
                    DrawBindingIdPopup(targetId, runtime?.BindingRegistry, "재생 AudioSource (선택)" );
                    break;
                case LunaCutsceneEffectType.SetFlag:
                    EditorGUILayout.PropertyField(stringValue, new GUIContent("기록할 상태", "예: flag.lab_door_open = true"));
                    break;
            }

            EditorGUILayout.PropertyField(serialized.FindProperty("fireOnSkip"), new GUIContent("스킵해도 적용"));
            EditorGUILayout.PropertyField(serialized.FindProperty("eventKey"), new GUIContent("효과 고정 ID"));
            ApplyQuickEditorChanges(serialized, marker, timeline, EditorGUI.EndChangeCheck());

            if ((type == LunaCutsceneEffectType.CameraShake
                 || type == LunaCutsceneEffectType.SetActive
                 || type == LunaCutsceneEffectType.SetSpriteColor)
                && string.IsNullOrWhiteSpace(marker.targetId))
                EditorGUILayout.HelpBox("이 효과는 대상 오브젝트를 선택해야 합니다.", MessageType.Error);
            if (type == LunaCutsceneEffectType.PlaySfx && marker.audioClip == null)
                EditorGUILayout.HelpBox("재생할 효과음 파일을 선택하세요.", MessageType.Warning);
            if (type == LunaCutsceneEffectType.SetFlag && string.IsNullOrWhiteSpace(marker.stringValue))
                EditorGUILayout.HelpBox("기록할 상태 값을 입력하세요.", MessageType.Error);
        }

        private static void DrawSpriteQuickEditor(
            LunaCutsceneDirector runtime,
            TimelineAsset timeline,
            LunaSpriteSwapMarker marker)
        {
            SerializedObject serialized = new(marker);
            serialized.Update();
            EditorGUI.BeginChangeCheck();
            DrawBindingIdPopup(serialized.FindProperty("targetId"), runtime?.BindingRegistry, "바꿀 대상");
            EditorGUILayout.PropertyField(serialized.FindProperty("sprite"), new GUIContent("새 스프라이트"));
            EditorGUILayout.PropertyField(serialized.FindProperty("flipX"), new GUIContent("좌우 반전"));
            EditorGUILayout.PropertyField(serialized.FindProperty("fireOnSkip"), new GUIContent("스킵해도 적용"));
            EditorGUILayout.PropertyField(serialized.FindProperty("eventKey"), new GUIContent("교체 고정 ID"));
            ApplyQuickEditorChanges(serialized, marker, timeline, EditorGUI.EndChangeCheck());

            if (string.IsNullOrWhiteSpace(marker.targetId))
                EditorGUILayout.HelpBox("스프라이트를 바꿀 대상을 선택하세요.", MessageType.Error);
            if (marker.sprite == null)
                EditorGUILayout.HelpBox("새로 표시할 스프라이트를 선택하세요.", MessageType.Error);
        }

        private static void DrawBindingIdPopup(
            SerializedProperty property,
            LunaCutsceneBindingRegistry registry,
            string label)
        {
            IEnumerable<LunaCutsceneBindingEntry> entries =
                registry?.Bindings ?? Array.Empty<LunaCutsceneBindingEntry>();
            List<string> ids = entries
                .Where(entry => entry?.target != null && !string.IsNullOrWhiteSpace(entry.id))
                .Select(entry => entry.id)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            List<string> values = new() { string.Empty };
            values.AddRange(ids);
            if (!string.IsNullOrWhiteSpace(property.stringValue) && !values.Contains(property.stringValue))
                values.Add(property.stringValue);
            string[] labels = values.Select(value => string.IsNullOrEmpty(value)
                    ? "— 대상 없음 —"
                    : ids.Contains(value) ? value : $"⚠ {value} (등록되지 않음)")
                .ToArray();
            int current = Mathf.Max(0, values.IndexOf(property.stringValue));
            int next = EditorGUILayout.Popup(label, current, labels);
            property.stringValue = values[next];
        }

        private static void ApplyQuickEditorChanges(
            SerializedObject serialized,
            UnityEngine.Object marker,
            TimelineAsset timeline,
            bool changed)
        {
            if (!changed)
                return;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(marker);
            EditorUtility.SetDirty(timeline);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }

        private static string GetMarkerLabel(UnityEngine.Object marker)
        {
            return marker switch
            {
                LunaDialogueMarker dialogue => $"[{dialogue.time:0.00}s] 대사 · {dialogue.line?.speakerKo ?? "화자 없음"}",
                LunaEffectMarker effect => $"[{effect.time:0.00}s] 효과 · {BeginnerEffectLabels[(int)effect.effectType]}",
                LunaSpriteSwapMarker sprite => $"[{sprite.time:0.00}s] 스프라이트 교체 · {sprite.targetId}",
                _ => marker.name
            };
        }

        private void DrawActorSetup(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("배우와 오브젝트 준비", EditorStyles.boldLabel);
            followHierarchySelection = EditorGUILayout.ToggleLeft("Hierarchy 선택을 대상 오브젝트에 자동 반영", followHierarchySelection);
            GameObject nextTarget = (GameObject)EditorGUILayout.ObjectField("대상 오브젝트", bindingTarget, typeof(GameObject), true);
            if (nextTarget != bindingTarget)
                AdoptBindingTarget(nextTarget);
            newBindingId = EditorGUILayout.TextField(
                new GUIContent(beginnerMode ? "배우 고정 ID" : "고정 Binding ID", "대사·효과·스킵 결과가 이 배우를 다시 찾을 때 사용하는 영문 ID입니다."),
                newBindingId);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(bindingTarget == null))
                {
                    if (GUILayout.Button("ID 자동 제안", GUILayout.Width(105f)))
                        SuggestBindingId();
                }
                using (new EditorGUI.DisabledScope(bindingTarget == null || string.IsNullOrWhiteSpace(newBindingId)))
                {
                    if (GUILayout.Button("선택 배우 빠른 설정", GUILayout.Height(30f)))
                        RunUserAction(() => QuickSetupActor(runtime, director, timeline), "배우·트랙·시작 앵커 준비 완료");
                }
            }

            EditorGUILayout.HelpBox(
                "빠른 설정 한 번으로 Binding ID, Move·Animation·Visibility 트랙, 시작 앵커, 머리 위 고정 SpeechAnchor를 준비합니다.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(bindingTarget == null))
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("말풍선 앵커 위치 다시 캡처"))
                    RunUserAction(
                        () => LunaCutsceneAuthoringBuilder.EnsureSpeechAnchor(bindingTarget, true),
                        "현재 스프라이트 상단 기준으로 SpeechAnchor를 다시 배치했습니다.");
                if (GUILayout.Button("말풍선 앵커 보기"))
                {
                    LunaCutsceneSpeechAnchor anchor =
                        bindingTarget.GetComponentInChildren<LunaCutsceneSpeechAnchor>(true);
                    if (anchor != null)
                    {
                        Selection.activeGameObject = anchor.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                }
            }

            using (new EditorGUI.DisabledScope(bindingTarget == null || string.IsNullOrWhiteSpace(newBindingId)))
            {
                if (GUILayout.Button("선택 오브젝트를 Binding Registry에 등록"))
                    RunUserAction(
                        () => LunaCutsceneAuthoringBuilder.EnsureBinding(runtime, bindingTarget, newBindingId),
                        $"Binding 등록 완료: {newBindingId}");
            }

            showAdvancedTrackButtons = EditorGUILayout.Foldout(showAdvancedTrackButtons, "고급: 트랙을 따로 추가하기", true);
            if (showAdvancedTrackButtons)
            {
                using (new EditorGUILayout.HorizontalScope())
                using (new EditorGUI.DisabledScope(bindingTarget == null))
                {
                    if (GUILayout.Button("배우 트랙 세트"))
                        RunUserAction(() =>
                        {
                            LunaCutsceneAuthoringBuilder.EnsureBinding(runtime, bindingTarget, newBindingId);
                            LunaCutsceneAuthoringBuilder.CreateActorTrackSet(director, timeline, bindingTarget, newBindingId);
                        }, "배우 트랙 세트 준비 완료");
                    if (GUILayout.Button("Animation Track"))
                        RunUserAction(() => CreateAnimationTrack(director, timeline, bindingTarget), "Animation Track 생성 완료");
                    if (GUILayout.Button("Audio Track"))
                        RunUserAction(() => CreateAudioTrack(director, timeline, bindingTarget), "Audio Track 생성 완료");
                }
            }
        }

        private void DrawMovementTools(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            EditorGUILayout.LabelField("배우 이동", EditorStyles.boldLabel);
            GameObject nextTarget = (GameObject)EditorGUILayout.ObjectField("이동할 배우", bindingTarget, typeof(GameObject), true);
            if (nextTarget != bindingTarget)
                AdoptBindingTarget(nextTarget);
            if (bindingTarget != null)
                EditorGUILayout.LabelField("사용 Binding ID", ResolveBindingId(bindingTarget), EditorStyles.miniLabel);

            newAnchorId = EditorGUILayout.TextField(
                new GUIContent(beginnerMode ? "새 이동 위치 ID" : "새 앵커 ID", "배우가 출발하거나 도착할 Scene 위치의 영문 ID입니다."),
                newAnchorId);
            using (new EditorGUI.DisabledScope(bindingTarget == null || string.IsNullOrWhiteSpace(newAnchorId)))
            {
                if (GUILayout.Button("대상 위치에 앵커 생성"))
                    RunUserAction(() => CreateAnchorAtTarget(runtime, bindingTarget, newAnchorId), $"앵커 생성 완료: {newAnchorId}");
            }

            fromAnchor = DrawAnchorPopup(beginnerMode ? "시작 위치" : "시작 앵커", fromAnchor);
            toAnchor = DrawAnchorPopup(beginnerMode ? "도착 위치" : "도착 앵커", toAnchor);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("시작 ↔ 도착 바꾸기"))
                    (fromAnchor, toAnchor) = (toAnchor, fromAnchor);
                using (new EditorGUI.DisabledScope(toAnchor == null))
                {
                    if (GUILayout.Button("도착 앵커 보기"))
                    {
                        Selection.activeGameObject = toAnchor.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                }
            }
            autoChainMove = EditorGUILayout.Toggle("이전 이동에서 자동으로 잇기", autoChainMove);
            moveSpeed = Mathf.Max(0.01f, EditorGUILayout.FloatField("이동 속도 (unit/sec)", moveSpeed));
            using (new EditorGUI.DisabledScope(bindingTarget == null || toAnchor == null))
            {
                if (GUILayout.Button("현재 재생 헤드에 이동 클립 추가", GUILayout.Height(30f)))
                    RunUserAction(() => AddMovementClip(runtime, director, timeline), "이동 클립 추가 완료");
            }

            EditorGUILayout.HelpBox(
                "첫 이동은 시작·도착 앵커를 모두 고릅니다. 다음 이동부터 자동 잇기를 켜면 직전 도착점을 시작점으로 사용합니다.",
                MessageType.None);
        }

        private void DrawEndStateTools(LunaCutsceneDirector runtime)
        {
            EditorGUILayout.LabelField("정상 종료·스킵 종료 상태", EditorStyles.boldLabel);
            GameObject nextTarget = (GameObject)EditorGUILayout.ObjectField("캡처할 배우", bindingTarget, typeof(GameObject), true);
            if (nextTarget != bindingTarget)
                AdoptBindingTarget(nextTarget);
            using (new EditorGUI.DisabledScope(bindingTarget == null || runtime.Definition == null))
            {
                if (GUILayout.Button("선택 배우의 현재 상태를 종료 상태로 캡처", GUILayout.Height(28f)))
                    RunUserAction(() => CaptureEndState(runtime, bindingTarget), "선택 배우의 종료 상태를 저장했습니다.");
            }

            using (new EditorGUI.DisabledScope(runtime.BindingRegistry == null || runtime.Definition == null))
            {
                if (GUILayout.Button("등록된 모든 배우의 종료 상태 캡처"))
                    CaptureAllActorEndStates(runtime);
            }

            if (runtime.Definition != null && GUILayout.Button("Definition 에셋 선택"))
            {
                Selection.activeObject = runtime.Definition;
                EditorGUIUtility.PingObject(runtime.Definition);
            }
            EditorGUILayout.HelpBox(
                "컷씬 마지막 모습으로 Scene을 배치한 뒤 캡처하세요. 정상 재생과 중간 스킵이 같은 활성 상태·위치·스프라이트를 남깁니다.",
                MessageType.None);
        }

        private void DrawValidation(LunaCutsceneDirector runtime)
        {
            EditorGUILayout.LabelField(beginnerMode ? "저장·실행 데이터 만들기·전체 검사" : "저장·베이크·전체 검사", EditorStyles.boldLabel);
            if (GUILayout.Button(
                    beginnerMode ? "현재 컷씬 저장 + 실행 데이터 만들기 + 전체 검사" : "현재 씬 저장 + Timeline 베이크 + 전체 검사",
                    GUILayout.Height(34f)))
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(runtime.gameObject.scene.path))
                        EditorSceneManager.SaveScene(runtime.gameObject.scene);
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

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("예제 자동 재생 검사", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("정상 재생 검증"))
                    LunaCutsceneAuthoringPlayTest.PlayAndVerifyNormal();
                if (GUILayout.Button("스킵 재생 검증"))
                    LunaCutsceneAuthoringPlayTest.PlayAndVerifySkip();
            }
            EditorGUILayout.HelpBox(
                "자동 재생 버튼은 CutsceneAuthoring_Lab 예제 전용입니다. 새 컷씬은 Play 모드에서 정상 재생과 스킵을 직접 확인하세요.",
                MessageType.None);
        }

        private void AdoptBindingTarget(GameObject target)
        {
            bindingTarget = target;
            if (target == null)
                return;
            LunaCutsceneBindingId binding = target.GetComponent<LunaCutsceneBindingId>();
            newBindingId = binding != null && !string.IsNullOrWhiteSpace(binding.BindingId)
                ? binding.BindingId
                : LunaCutsceneAuthoringBuilder.MakeSafeId(target.name);
            if (string.IsNullOrWhiteSpace(newBindingId))
                newBindingId = "actor_id";
            newAnchorId = newBindingId + "_start";
        }

        private void SuggestBindingId()
        {
            if (bindingTarget == null)
                return;
            LunaCutsceneBindingId binding = bindingTarget.GetComponent<LunaCutsceneBindingId>();
            newBindingId = binding != null && !string.IsNullOrWhiteSpace(binding.BindingId)
                ? binding.BindingId
                : LunaCutsceneAuthoringBuilder.MakeSafeId(bindingTarget.name);
            newAnchorId = newBindingId + "_start";
        }

        private static string ResolveBindingId(GameObject target)
        {
            if (target == null)
                return "없음";
            LunaCutsceneBindingId binding = target.GetComponent<LunaCutsceneBindingId>();
            return binding != null && !string.IsNullOrWhiteSpace(binding.BindingId)
                ? binding.BindingId
                : "미등록";
        }

        private void QuickSetupActor(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            if (bindingTarget == null)
                throw new InvalidOperationException("Hierarchy에서 배우 오브젝트를 선택하세요.");
            string id = LunaCutsceneAuthoringBuilder.MakeSafeId(newBindingId);
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidOperationException("배우의 Binding ID를 입력하세요.");

            LunaCutsceneAuthoringBuilder.EnsureBinding(runtime, bindingTarget, id);
            LunaCutsceneAuthoringBuilder.EnsureSpeechAnchor(bindingTarget, true);
            LunaCutsceneAuthoringBuilder.CreateActorTrackSet(director, timeline, bindingTarget, id);
            LunaCutsceneAuthoringBuilder.EnsureSceneAuthoringHelpers();

            LunaCutsceneAnchor start = UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None)
                .FirstOrDefault(anchor => anchor.AnchorId == id + "_start");
            if (start == null)
            {
                Transform root = GetOrCreateAnchorRoot(runtime);
                start = LunaCutsceneAuthoringBuilder.CreateAnchor(root, id + "_start", bindingTarget.transform.position);
            }

            fromAnchor = start;
            toAnchor = null;
            newBindingId = id;
            newAnchorId = id + "_end";
            Selection.activeGameObject = bindingTarget;
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
            workflowStep = WorkflowStep.Timeline;
        }

        private static Transform GetOrCreateAnchorRoot(LunaCutsceneDirector runtime)
        {
            Transform stage = runtime.transform.Find("STAGE");
            if (stage == null)
                stage = runtime.transform;
            Transform root = stage.Find("ANCHORS");
            if (root != null)
                return root;
            GameObject anchorRoot = new("ANCHORS");
            Undo.RegisterCreatedObjectUndo(anchorRoot, "Create L.U.N.A Anchor Root");
            anchorRoot.transform.SetParent(stage, false);
            return anchorRoot.transform;
        }

        private static LunaCutsceneAnchor DrawAnchorPopup(string label, LunaCutsceneAnchor current)
        {
            LunaCutsceneAnchor[] anchors = UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None)
                .OrderBy(anchor => anchor.AnchorId, StringComparer.Ordinal)
                .ToArray();
            string[] labels = new[] { "— 선택 안 함 —" }
                .Concat(anchors.Select(anchor => anchor.AnchorId))
                .ToArray();
            int index = current == null ? 0 : Array.IndexOf(anchors, current) + 1;
            if (index < 0)
                index = 0;
            int next = EditorGUILayout.Popup(label, index, labels);
            return next <= 0 ? null : anchors[next - 1];
        }

        private void CaptureAllActorEndStates(LunaCutsceneDirector runtime)
        {
            if (!EditorUtility.DisplayDialog(
                    "모든 배우 종료 상태 캡처",
                    "현재 Scene에 배치된 등록 배우의 활성 상태·위치·스프라이트로 Definition을 갱신합니다. 계속할까요?",
                    "캡처",
                    "취소"))
                return;

            GameObject[] actors = runtime.BindingRegistry.Bindings
                .Where(entry => entry?.target != null
                                && entry.target.GetComponent<Animator>() != null
                                && entry.target.GetComponentInChildren<SpriteRenderer>(true) != null)
                .Select(entry => entry.target)
                .Distinct()
                .ToArray();
            if (actors.Length == 0)
            {
                SetFeedback("캡처할 등록 배우가 없습니다.", MessageType.Warning);
                return;
            }
            foreach (GameObject actor in actors)
                CaptureEndState(runtime, actor);
            SetFeedback($"등록 배우 {actors.Length}명의 종료 상태를 저장했습니다.", MessageType.Info);
        }

        private void RunUserAction(Action action, string successMessage)
        {
            try
            {
                action();
                SetFeedback(successMessage, MessageType.Info);
            }
            catch (Exception exception)
            {
                SetFeedback(exception.Message, MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private void SetFeedback(string message, MessageType type)
        {
            validationReport = message;
            validationType = type;
            Repaint();
        }

        private void CreateAnchorAtTarget(LunaCutsceneDirector runtime, GameObject target, string anchorId)
        {
            Transform root = GetOrCreateAnchorRoot(runtime);
            LunaCutsceneAnchor anchor = LunaCutsceneAuthoringBuilder.CreateAnchor(root, anchorId, target.transform.position);
            Selection.activeGameObject = anchor.gameObject;
            toAnchor = anchor;
            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        }

        private void AddMovementClip(
            LunaCutsceneDirector runtime,
            PlayableDirector director,
            TimelineAsset timeline)
        {
            LunaCutsceneBindingId binding = bindingTarget.GetComponent<LunaCutsceneBindingId>();
            string id = binding != null && !string.IsNullOrWhiteSpace(binding.BindingId)
                ? binding.BindingId
                : newBindingId;
            LunaCutsceneAuthoringBuilder.EnsureBinding(runtime, bindingTarget, id);
            LunaCutsceneAuthoringBuilder.ActorTrackSet tracks =
                LunaCutsceneAuthoringBuilder.CreateActorTrackSet(director, timeline, bindingTarget, id);

            Transform resolvedFrom = fromAnchor != null ? fromAnchor.transform : null;
            if (autoChainMove)
            {
                TimelineClip previous = tracks.move.GetClips()
                    .Where(clip => clip.end <= director.time + 0.0001d && clip.asset is LunaActorMoveClip)
                    .OrderByDescending(clip => clip.end)
                    .FirstOrDefault();
                if (previous?.asset is LunaActorMoveClip previousMove)
                    resolvedFrom = previousMove.ResolveTo(director);
            }
            if (resolvedFrom == null)
                throw new InvalidOperationException("첫 이동이면 시작 앵커를 지정하세요. 이후 이동은 '이전 이동에서 자동으로 잇기'로 연결할 수 있습니다.");

            TimelineClip created = LunaCutsceneAuthoringBuilder.AddMoveClip(
                director,
                timeline,
                tracks.move,
                resolvedFrom,
                toAnchor.transform,
                director.time,
                moveSpeed);
            Selection.activeObject = created.asset;
            TimelineEditor.selectedClip = created;
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);
        }

        private static void CaptureEndState(LunaCutsceneDirector runtime, GameObject target)
        {
            LunaCutsceneBindingId binding = target.GetComponent<LunaCutsceneBindingId>();
            if (binding == null || string.IsNullOrWhiteSpace(binding.BindingId))
                throw new InvalidOperationException("종료 상태를 캡처하려면 먼저 Binding ID를 등록하세요.");

            LunaCutsceneDefinition definition = runtime.Definition;
            Undo.RecordObject(definition, "Capture L.U.N.A Cutscene End State");
            List<LunaCutsceneEndBinding> bindings = definition.endBindings?.ToList()
                                                       ?? new List<LunaCutsceneEndBinding>();
            LunaCutsceneEndBinding end = bindings.FirstOrDefault(value => value != null && value.targetId == binding.BindingId);
            if (end == null)
            {
                end = new LunaCutsceneEndBinding { targetId = binding.BindingId };
                bindings.Add(end);
            }

            end.applyActive = true;
            end.active = target.activeSelf;
            end.applyPosition = true;
            end.localPosition = target.transform.localPosition;
            SpriteRenderer renderer = target.GetComponentInChildren<SpriteRenderer>(true);
            if (renderer != null)
            {
                end.applySprite = true;
                end.sprite = renderer.sprite;
            }
            else
            {
                end.applySprite = false;
                end.sprite = null;
            }

            definition.endBindings = bindings.ToArray();
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LunaCutsceneAuthoring] END_STATE_CAPTURED target={binding.BindingId}", definition);
        }

        private void CreateDialogueMarker(TimelineAsset timeline, double time)
        {
            PrepareMarkerTrack(timeline);
            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Dialogue Marker");
            LunaDialogueMarker marker = timeline.markerTrack.CreateMarker<LunaDialogueMarker>(time);
            marker.dialogueId = CreateUniqueMarkerId(timeline, $"dlg_{timeline.name.ToLowerInvariant()}", time);
            string bindingId = ResolveBindingId(bindingTarget);
            bool hasSpeaker = bindingTarget != null && bindingId != "미등록";
            marker.line.speakerId = hasSpeaker ? bindingId : "speaker_id";
            marker.line.speakerKo = hasSpeaker ? bindingTarget.name : "화자";
            marker.line.speakerEn = hasSpeaker ? bindingTarget.name : "Speaker";
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
            marker.eventKey = CreateUniqueMarkerId(timeline, "fx", time);
            FinishMarkerCreation(timeline, marker);
        }

        private void CreateSpriteMarker(TimelineAsset timeline, double time)
        {
            PrepareMarkerTrack(timeline);
            Undo.RegisterCompleteObjectUndo(timeline, "Add L.U.N.A Sprite Marker");
            LunaSpriteSwapMarker marker = timeline.markerTrack.CreateMarker<LunaSpriteSwapMarker>(time);
            string bindingId = ResolveBindingId(bindingTarget);
            marker.targetId = bindingId == "미등록" || bindingId == "없음" ? string.Empty : bindingId;
            marker.eventKey = CreateUniqueMarkerId(timeline, "sprite", time);
            FinishMarkerCreation(timeline, marker);
        }

        private static string CreateUniqueMarkerId(TimelineAsset timeline, string prefix, double time)
        {
            string safePrefix = LunaCutsceneAuthoringBuilder.MakeSafeId(prefix);
            string baseId = $"{safePrefix}_{Mathf.RoundToInt((float)(time * 1000d)):000000}";
            HashSet<string> used = new(StringComparer.Ordinal);
            if (timeline.markerTrack != null)
            {
                foreach (IMarker existing in timeline.markerTrack.GetMarkers())
                {
                    switch (existing)
                    {
                        case LunaDialogueMarker dialogue when !string.IsNullOrWhiteSpace(dialogue.dialogueId):
                            used.Add(dialogue.dialogueId);
                            break;
                        case LunaEffectMarker effect when !string.IsNullOrWhiteSpace(effect.eventKey):
                            used.Add(effect.eventKey);
                            break;
                        case LunaSpriteSwapMarker sprite when !string.IsNullOrWhiteSpace(sprite.eventKey):
                            used.Add(sprite.eventKey);
                            break;
                    }
                }
            }

            string candidate = baseId;
            int suffix = 2;
            while (used.Contains(candidate))
                candidate = $"{baseId}_{suffix++:00}";
            return candidate;
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
