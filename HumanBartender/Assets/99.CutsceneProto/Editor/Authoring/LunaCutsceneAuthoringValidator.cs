using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    public static class LunaCutsceneAuthoringValidator
    {
        public sealed class Result
        {
            public readonly List<string> errors = new();
            public readonly List<string> warnings = new();
            public int dialogueCount;
            public int effectCount;
            public int spriteSwapCount;
            public int movementCount;
            public bool IsValid => errors.Count == 0;

            public string ToReport()
            {
                StringBuilder builder = new();
                builder.AppendLine(IsValid ? "VALIDATION OK" : "VALIDATION FAILED");
                builder.AppendLine($"대사 {dialogueCount} · 효과 {effectCount} · 스프라이트 교체 {spriteSwapCount} · 이동 {movementCount}");
                if (errors.Count > 0)
                {
                    builder.AppendLine("\n오류");
                    foreach (string error in errors) builder.AppendLine("- " + error);
                }
                if (warnings.Count > 0)
                {
                    builder.AppendLine("\n확인 권장");
                    foreach (string warning in warnings) builder.AppendLine("- " + warning);
                }
                return builder.ToString().TrimEnd();
            }
        }

        [MenuItem("Project L.U.N.A/Cutscene Authoring/Validate Current Scene")]
        public static void ValidateSceneAndLog()
        {
            LunaCutsceneDirector runtime = FindInScene<LunaCutsceneDirector>(SceneManager.GetActiveScene());
            Result result = Validate(runtime);
            if (!result.IsValid)
                throw new InvalidOperationException(result.ToReport());
            Debug.Log("[LunaCutsceneAuthoring]\n" + result.ToReport(), runtime);
        }

        public static Result Validate(LunaCutsceneDirector runtime)
        {
            Result result = new();
            if (runtime == null)
            {
                result.errors.Add("현재 씬에 LunaCutsceneDirector가 없습니다.");
                return result;
            }

            PlayableDirector director = runtime.Director != null ? runtime.Director : runtime.GetComponent<PlayableDirector>();
            LunaCutsceneDefinition definition = runtime.Definition;
            LunaCutsceneBindingRegistry registry = runtime.BindingRegistry;
            if (definition == null) result.errors.Add("Cutscene Definition이 연결되지 않았습니다.");
            if (director == null) result.errors.Add("PlayableDirector가 연결되지 않았습니다.");
            if (registry == null) result.errors.Add("Binding Registry가 연결되지 않았습니다.");
            if (runtime.DialogueUI == null) result.errors.Add("Dialogue UI가 연결되지 않았습니다.");
            else ValidateDialogueUi(runtime.DialogueUI, result);
            if (director == null || director.playableAsset is not TimelineAsset timeline)
            {
                result.errors.Add("PlayableDirector에 실제 TimelineAsset이 연결되지 않았습니다.");
                return result;
            }

            ValidateBindings(director, timeline, registry, result);
            ValidateAnchorsAndMovement(director, timeline, result);
            ValidateMarkers(timeline, registry, definition, result);
            ValidateDefinition(definition, registry, result);
            ValidateDefinitionIds(definition, result);
            return result;
        }

        private static void ValidateBindings(
            PlayableDirector director,
            TimelineAsset timeline,
            LunaCutsceneBindingRegistry registry,
            Result result)
        {
            HashSet<string> ids = new(StringComparer.Ordinal);
            if (registry != null)
            {
                foreach (LunaCutsceneBindingEntry entry in registry.Bindings)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                    {
                        result.errors.Add("Binding Registry에 ID가 비어 있는 항목이 있습니다.");
                        continue;
                    }
                    if (entry.target == null)
                        result.errors.Add($"Binding '{entry.id}'의 대상 GameObject가 없습니다.");
                    if (!ids.Add(entry.id))
                        result.errors.Add($"Binding ID가 중복됩니다: {entry.id}");
                }
            }

            foreach (TrackAsset track in LunaCutsceneAuthoringBuilder.EnumerateTracks(timeline))
            {
                if (track is MarkerTrack || track is GroupTrack)
                    continue;
                if (director.GetGenericBinding(track) == null)
                    result.warnings.Add($"트랙 '{track.name}'의 Scene Binding이 비어 있습니다.");
            }

            foreach (AnimationTrack animationTrack in LunaCutsceneAuthoringBuilder.EnumerateTracks(timeline).OfType<AnimationTrack>())
            {
                GroupTrack group = animationTrack.GetGroup() as GroupTrack;
                if (group == null || !group.GetChildTracks().OfType<LunaActorMoveTrack>().Any())
                    continue;
                foreach (TimelineClip clip in animationTrack.GetClips())
                {
                    AnimationClip animation = clip.animationClip;
                    if (animation == null)
                        continue;
                    bool writesPosition = AnimationUtility.GetCurveBindings(animation)
                        .Any(binding => binding.type == typeof(Transform)
                                        && binding.propertyName.StartsWith("m_LocalPosition", StringComparison.Ordinal));
                    if (writesPosition)
                        result.warnings.Add($"배우 그룹 '{group.name}'의 '{clip.displayName}'이 위치 커브를 포함합니다. 위치는 Move Track, 동작은 제자리 AnimationClip으로 분리하세요.");
                }
            }
        }

        private static void ValidateAnchorsAndMovement(
            PlayableDirector director,
            TimelineAsset timeline,
            Result result)
        {
            Dictionary<string, LunaCutsceneAnchor> anchors = new(StringComparer.Ordinal);
            foreach (LunaCutsceneAnchor anchor in UnityEngine.Object.FindObjectsByType<LunaCutsceneAnchor>(FindObjectsSortMode.None))
            {
                if (string.IsNullOrWhiteSpace(anchor.AnchorId))
                    result.errors.Add($"앵커 '{anchor.name}'의 anchor_id가 비어 있습니다.");
                else if (!anchors.TryAdd(anchor.AnchorId, anchor))
                    result.errors.Add($"앵커 ID가 중복됩니다: {anchor.AnchorId}");
            }

            foreach (LunaActorMoveTrack track in LunaCutsceneAuthoringBuilder.EnumerateTracks(timeline).OfType<LunaActorMoveTrack>())
            {
                if (director.GetGenericBinding(track) is not Transform)
                    result.errors.Add($"이동 트랙 '{track.name}'에 Transform 바인딩이 없습니다.");
                TimelineClip previous = null;
                foreach (TimelineClip clip in track.GetClips().OrderBy(value => value.start))
                {
                    result.movementCount++;
                    if (clip.asset is not LunaActorMoveClip move)
                    {
                        result.errors.Add($"'{track.name}'의 '{clip.displayName}'이 L.U.N.A 이동 클립이 아닙니다.");
                        continue;
                    }
                    Transform from = move.ResolveFrom(director);
                    Transform to = move.ResolveTo(director);
                    if (from == null || to == null)
                        result.errors.Add($"'{clip.displayName}'의 시작 또는 도착 앵커 참조가 비어 있습니다.");
                    else if (from == to)
                        result.warnings.Add($"'{clip.displayName}'의 시작과 도착 앵커가 같습니다.");
                    if (clip.duration <= 0.01d)
                        result.errors.Add($"'{clip.displayName}'의 길이가 0입니다.");
                    if (previous != null && clip.start < previous.end - 0.0001d)
                        result.errors.Add($"이동 트랙 '{track.name}'에서 '{previous.displayName}'과 '{clip.displayName}'이 겹칩니다.");
                    previous = clip;
                }
            }

            foreach (AnimationTrack cameraTrack in LunaCutsceneAuthoringBuilder.EnumerateTracks(timeline)
                         .OfType<AnimationTrack>()
                         .Where(track => track.name.Contains("Camera", StringComparison.OrdinalIgnoreCase)))
            {
                TimelineClip previous = null;
                foreach (TimelineClip clip in cameraTrack.GetClips().OrderBy(value => value.start))
                {
                    if (previous != null && clip.start < previous.end - 0.0001d)
                        result.warnings.Add($"카메라 트랙 '{cameraTrack.name}'의 클립이 겹칩니다. 의도한 블렌딩인지 확인하세요.");
                    previous = clip;
                }
            }
        }

        private static void ValidateMarkers(
            TimelineAsset timeline,
            LunaCutsceneBindingRegistry registry,
            LunaCutsceneDefinition definition,
            Result result)
        {
            if (timeline.markerTrack == null)
            {
                result.errors.Add("Timeline에 Marker Track이 없습니다.");
                return;
            }

            HashSet<string> dialogueIds = new(StringComparer.Ordinal);
            HashSet<string> eventKeys = new(StringComparer.Ordinal);
            foreach (IMarker marker in timeline.markerTrack.GetMarkers().OrderBy(value => value.time))
            {
                if (marker.time < 0d || marker.time > timeline.duration + 0.0001d)
                    result.errors.Add($"{marker.time:0.00}s 마커가 Timeline 길이 {timeline.duration:0.00}s 밖에 있습니다.");
                switch (marker)
                {
                    case LunaDialogueMarker dialogue:
                        result.dialogueCount++;
                        if (string.IsNullOrWhiteSpace(dialogue.dialogueId))
                            result.errors.Add($"{dialogue.time:0.00}s 대사에 dialogue_id가 없습니다.");
                        else if (!dialogueIds.Add(dialogue.dialogueId))
                            result.errors.Add($"dialogue_id가 중복됩니다: {dialogue.dialogueId}");
                        if (dialogue.line == null
                            || string.IsNullOrWhiteSpace(dialogue.line.textKo)
                            || string.IsNullOrWhiteSpace(dialogue.line.textEn))
                            result.errors.Add($"{dialogue.time:0.00}s 대사의 한국어 또는 영어 본문이 비어 있습니다.");
                        if (dialogue.line != null
                            && string.IsNullOrWhiteSpace(dialogue.line.speakerKo) != string.IsNullOrWhiteSpace(dialogue.line.speakerEn))
                            result.errors.Add($"{dialogue.time:0.00}s 대사의 화자명은 한국어·영어를 함께 입력해야 합니다.");
                        if (dialogue.line != null
                            && (string.IsNullOrWhiteSpace(dialogue.line.speakerKo) == false
                                || string.IsNullOrWhiteSpace(dialogue.line.speakerEn) == false)
                            && string.IsNullOrWhiteSpace(dialogue.line.speakerId))
                            result.errors.Add($"{dialogue.time:0.00}s 캐릭터 대사에 speaker_id가 없습니다.");
                        if (dialogue.line != null
                            && !string.IsNullOrWhiteSpace(dialogue.line.speakerId)
                            && !HasBinding(registry, dialogue.line.speakerId))
                            result.errors.Add(
                                $"{dialogue.time:0.00}s 대사의 speaker_id가 Binding Registry에 없습니다: {dialogue.line.speakerId}");
                        else if (dialogue.line != null
                                 && !string.IsNullOrWhiteSpace(dialogue.line.speakerId)
                                 && registry != null
                                 && registry.TryGet(dialogue.line.speakerId, out GameObject speaker)
                                 && speaker.GetComponentInChildren<LunaCutsceneSpeechAnchor>(true) == null)
                            result.warnings.Add(
                                $"{dialogue.time:0.00}s 화자 '{dialogue.line.speakerId}'에 SpeechAnchor가 없어 루트 위치를 사용합니다.");
                        break;

                    case LunaEffectMarker effect:
                        result.effectCount++;
                        if (EffectNeedsTarget(effect.effectType) && !HasBinding(registry, effect.targetId))
                            result.errors.Add($"{effect.time:0.00}s {effect.effectType} 효과의 target_id가 없거나 유효하지 않습니다: {effect.targetId}");
                        ValidateEventKey(effect.time, effect.fireOnSkip, effect.eventKey, eventKeys, result);
                        if (effect.effectType == LunaCutsceneEffectType.SetFlag && string.IsNullOrWhiteSpace(effect.stringValue))
                            result.errors.Add($"{effect.time:0.00}s SetFlag 효과의 string_value가 비어 있습니다.");
                        break;

                    case LunaSpriteSwapMarker spriteSwap:
                        result.spriteSwapCount++;
                        if (!HasBinding(registry, spriteSwap.targetId))
                            result.errors.Add($"{spriteSwap.time:0.00}s 스프라이트 교체 대상이 유효하지 않습니다: {spriteSwap.targetId}");
                        if (spriteSwap.sprite == null)
                            result.errors.Add($"{spriteSwap.time:0.00}s 스프라이트 교체 이미지가 없습니다.");
                        ValidateEventKey(spriteSwap.time, spriteSwap.fireOnSkip, spriteSwap.eventKey, eventKeys, result);
                        break;
                }
            }

            int supportedMarkerCount = result.dialogueCount + result.effectCount + result.spriteSwapCount;
            int bakedCount = definition?.bakedEvents?.Length ?? 0;
            if (supportedMarkerCount != bakedCount)
                result.errors.Add($"Timeline 마커 {supportedMarkerCount}개와 런타임 베이크 {bakedCount}개가 다릅니다. Studio에서 다시 베이크하세요.");
        }

        private static void ValidateDefinitionIds(LunaCutsceneDefinition current, Result result)
        {
            if (current == null || string.IsNullOrWhiteSpace(current.cutsceneId))
                return;
            int sameIdCount = AssetDatabase.FindAssets("t:LunaCutsceneDefinition", new[] { LunaCutsceneAuthoringBuilder.Root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<LunaCutsceneDefinition>)
                .Count(definition => definition != null && definition.cutsceneId == current.cutsceneId);
            if (sameIdCount > 1)
                result.errors.Add($"cutscene_id가 다른 Definition과 중복됩니다: {current.cutsceneId}");
        }

        private static void ValidateDialogueUi(LunaCutsceneDialogueUI ui, Result result)
        {
            if (ui.BubbleLayer == null)
                result.errors.Add("Dialogue UI에 SpeechBubbleLayer가 연결되지 않았습니다.");
            if (ui.BubblePrefab == null)
                result.errors.Add("Dialogue UI에 CutsceneSpeechBubble Prefab이 연결되지 않았습니다.");
            if (ui.BubbleStyle == null)
            {
                result.errors.Add("Dialogue UI에 CutsceneSpeechBubbleStyle 에셋이 연결되지 않았습니다.");
                return;
            }
            if (ui.BubbleStyle.font == null)
                result.errors.Add("CutsceneSpeechBubbleStyle의 TMP Font Asset이 비어 있습니다.");
            if (ui.BubbleStyle.maxWidth < ui.BubbleStyle.minWidth
                || ui.BubbleStyle.maxHeight < ui.BubbleStyle.minHeight)
                result.errors.Add("말풍선 Style의 최대 크기가 최소 크기보다 작습니다.");
        }

        private static void ValidateDefinition(
            LunaCutsceneDefinition definition,
            LunaCutsceneBindingRegistry registry,
            Result result)
        {
            if (definition == null)
                return;
            if (string.IsNullOrWhiteSpace(definition.cutsceneId))
                result.errors.Add("Definition의 cutscene_id가 비어 있습니다.");
            if (string.IsNullOrWhiteSpace(definition.titleKo) || string.IsNullOrWhiteSpace(definition.titleEn))
                result.errors.Add("Definition의 한국어·영어 제목을 모두 입력해야 합니다.");
            if (definition.endBindings == null)
                return;
            foreach (LunaCutsceneEndBinding endBinding in definition.endBindings)
            {
                if (endBinding == null || !HasBinding(registry, endBinding.targetId))
                    result.errors.Add($"Definition 종료 상태 대상이 유효하지 않습니다: {endBinding?.targetId}");
            }
        }

        private static void ValidateEventKey(
            double time,
            bool fireOnSkip,
            string eventKey,
            HashSet<string> eventKeys,
            Result result)
        {
            if (fireOnSkip && string.IsNullOrWhiteSpace(eventKey))
                result.errors.Add($"{time:0.00}s 스킵 필수 마커에 event_key가 없습니다.");
            if (!string.IsNullOrWhiteSpace(eventKey) && !eventKeys.Add(eventKey))
                result.errors.Add($"event_key가 중복됩니다: {eventKey}");
        }

        private static bool EffectNeedsTarget(LunaCutsceneEffectType type)
        {
            return type == LunaCutsceneEffectType.CameraShake
                   || type == LunaCutsceneEffectType.SetActive
                   || type == LunaCutsceneEffectType.SetSpriteColor;
        }

        private static bool HasBinding(LunaCutsceneBindingRegistry registry, string id)
        {
            return registry != null && !string.IsNullOrWhiteSpace(id) && registry.Contains(id);
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return null;
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
