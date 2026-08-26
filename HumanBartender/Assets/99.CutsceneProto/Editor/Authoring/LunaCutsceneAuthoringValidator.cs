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
            public bool IsValid => errors.Count == 0;

            public string ToReport()
            {
                StringBuilder builder = new();
                builder.AppendLine(IsValid ? "VALIDATION OK" : "VALIDATION FAILED");
                builder.AppendLine($"대사 {dialogueCount} · 효과 {effectCount} · 스프라이트 교체 {spriteSwapCount}");
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
            if (director == null || director.playableAsset is not TimelineAsset timeline)
            {
                result.errors.Add("PlayableDirector에 실제 TimelineAsset이 연결되지 않았습니다.");
                return result;
            }

            ValidateBindings(director, timeline, registry, result);
            ValidateMarkers(timeline, registry, definition, result);
            ValidateDefinition(definition, registry, result);
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
