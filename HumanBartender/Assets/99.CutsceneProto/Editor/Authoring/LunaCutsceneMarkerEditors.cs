using System;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor.Timeline;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    [CustomTimelineEditor(typeof(LunaDialogueMarker))]
    public sealed class LunaDialogueMarkerEditor : MarkerEditor
    {
        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            if (marker is not LunaDialogueMarker dialogue)
                return base.GetMarkerOptions(marker);
            string speaker = string.IsNullOrWhiteSpace(dialogue.line?.speakerKo) ? "무명" : dialogue.line.speakerKo;
            string body = dialogue.line?.textKo ?? string.Empty;
            string error = string.IsNullOrWhiteSpace(dialogue.dialogueId) || string.IsNullOrWhiteSpace(body)
                ? "대사 ID와 한국어·영어 본문을 입력하세요."
                : string.Empty;
            return new MarkerDrawOptions
            {
                tooltip = $"[대사] {speaker}: {body}\nID: {dialogue.dialogueId}",
                errorText = error
            };
        }
    }

    [CustomTimelineEditor(typeof(LunaEffectMarker))]
    public sealed class LunaEffectMarkerEditor : MarkerEditor
    {
        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            if (marker is not LunaEffectMarker effect)
                return base.GetMarkerOptions(marker);
            string target = string.IsNullOrWhiteSpace(effect.targetId) ? "공용" : effect.targetId;
            string error = effect.fireOnSkip && string.IsNullOrWhiteSpace(effect.eventKey)
                ? "스킵 필수 효과에는 event_key가 필요합니다."
                : string.Empty;
            return new MarkerDrawOptions
            {
                tooltip = $"[효과] {effect.effectType} → {target}\nEvent: {effect.eventKey}",
                errorText = error
            };
        }
    }

    [CustomTimelineEditor(typeof(LunaSpriteSwapMarker))]
    public sealed class LunaSpriteSwapMarkerEditor : MarkerEditor
    {
        public override MarkerDrawOptions GetMarkerOptions(IMarker marker)
        {
            if (marker is not LunaSpriteSwapMarker sprite)
                return base.GetMarkerOptions(marker);
            string error = string.IsNullOrWhiteSpace(sprite.targetId) || sprite.sprite == null
                ? "대상 Binding ID와 교체할 Sprite가 필요합니다."
                : string.Empty;
            return new MarkerDrawOptions
            {
                tooltip = $"[스프라이트 교체] {sprite.targetId} → {(sprite.sprite != null ? sprite.sprite.name : "None")}",
                errorText = error
            };
        }
    }
}
