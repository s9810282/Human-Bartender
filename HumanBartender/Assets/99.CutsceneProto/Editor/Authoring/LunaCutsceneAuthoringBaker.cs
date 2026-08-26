using System;
using System.Collections.Generic;
using System.Linq;
using ProjectLuna.CutscenePrototype.Authoring;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    public static class LunaCutsceneAuthoringBaker
    {
        public static int Bake(LunaCutsceneDirector runtime)
        {
            if (runtime == null || runtime.Definition == null || runtime.Director == null)
                throw new InvalidOperationException("베이크할 Cutscene Director·Definition·PlayableDirector 연결이 없습니다.");
            if (runtime.Director.playableAsset is not TimelineAsset timeline)
                throw new InvalidOperationException("PlayableDirector에 실제 TimelineAsset이 연결되지 않았습니다.");
            return Bake(timeline, runtime.Definition);
        }

        public static int Bake(TimelineAsset timeline, LunaCutsceneDefinition definition)
        {
            if (timeline == null || definition == null)
                throw new InvalidOperationException("Timeline 또는 Definition이 없습니다.");

            List<LunaBakedCutsceneEvent> events = new();
            if (timeline.markerTrack != null)
            {
                foreach (IMarker marker in timeline.markerTrack.GetMarkers().OrderBy(value => value.time))
                {
                    LunaBakedCutsceneEvent baked = marker switch
                    {
                        LunaDialogueMarker dialogue => BakeDialogue(dialogue),
                        LunaEffectMarker effect => BakeEffect(effect),
                        LunaSpriteSwapMarker sprite => BakeSprite(sprite),
                        _ => null
                    };
                    if (baked != null)
                        events.Add(baked);
                }
            }

            Undo.RecordObject(definition, "Bake L.U.N.A Cutscene Timeline");
            definition.SetBakedEvents(events.ToArray(), timeline.duration);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LunaCutsceneAuthoring] BAKE_OK id={definition.cutsceneId}, events={events.Count}, duration={timeline.duration:0.00}", definition);
            return events.Count;
        }

        private static LunaBakedCutsceneEvent BakeDialogue(LunaDialogueMarker marker)
        {
            return new LunaBakedCutsceneEvent
            {
                time = marker.time,
                kind = LunaBakedEventKind.Dialogue,
                line = CloneLine(marker.line),
                pauseTimeline = marker.pauseTimeline,
                advanceMode = marker.advanceMode,
                autoDelay = marker.autoDelay,
                dialogueId = marker.dialogueId
            };
        }

        private static LunaBakedCutsceneEvent BakeEffect(LunaEffectMarker marker)
        {
            return new LunaBakedCutsceneEvent
            {
                time = marker.time,
                kind = LunaBakedEventKind.Effect,
                effectType = marker.effectType,
                targetId = marker.targetId,
                stringValue = marker.stringValue,
                color = marker.color,
                duration = marker.duration,
                strength = marker.strength,
                audioClip = marker.audioClip,
                fireOnSkip = marker.fireOnSkip,
                eventKey = marker.eventKey
            };
        }

        private static LunaBakedCutsceneEvent BakeSprite(LunaSpriteSwapMarker marker)
        {
            return new LunaBakedCutsceneEvent
            {
                time = marker.time,
                kind = LunaBakedEventKind.SpriteSwap,
                targetId = marker.targetId,
                sprite = marker.sprite,
                flipX = marker.flipX,
                fireOnSkip = marker.fireOnSkip,
                eventKey = marker.eventKey
            };
        }

        private static LunaLocalizedLine CloneLine(LunaLocalizedLine line)
        {
            if (line == null)
                return new LunaLocalizedLine();
            return new LunaLocalizedLine
            {
                speakerId = line.speakerId,
                speakerKo = line.speakerKo,
                speakerEn = line.speakerEn,
                textKo = line.textKo,
                textEn = line.textEn
            };
        }
    }
}
