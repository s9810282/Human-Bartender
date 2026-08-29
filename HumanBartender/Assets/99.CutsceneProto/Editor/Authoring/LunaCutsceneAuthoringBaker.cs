using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
            string contentHash = ComputeTimelineHash(timeline);
            definition.SetBakedEvents(events.ToArray(), timeline.duration, contentHash);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LunaCutsceneAuthoring] BAKE_OK id={definition.cutsceneId}, events={events.Count}, duration={timeline.duration:0.00}", definition);
            return events.Count;
        }

        public static string ComputeTimelineHash(TimelineAsset timeline)
        {
            if (timeline == null)
                return string.Empty;

            StringBuilder builder = new();
            builder.Append(timeline.duration.ToString("R", CultureInfo.InvariantCulture));
            if (timeline.markerTrack != null)
            {
                foreach (IMarker marker in timeline.markerTrack.GetMarkers().OrderBy(value => value.time))
                {
                    if (marker is not LunaDialogueMarker
                        && marker is not LunaEffectMarker
                        && marker is not LunaSpriteSwapMarker)
                        continue;
                    AppendHashToken(builder, marker.GetType().FullName);
                    AppendHashToken(builder, marker.time.ToString("R", CultureInfo.InvariantCulture));
                    switch (marker)
                    {
                        case LunaDialogueMarker dialogue:
                            AppendHashToken(builder, dialogue.dialogueId);
                            AppendHashToken(builder, dialogue.line?.speakerId);
                            AppendHashToken(builder, dialogue.line?.speakerKo);
                            AppendHashToken(builder, dialogue.line?.speakerEn);
                            AppendHashToken(builder, dialogue.line?.textKo);
                            AppendHashToken(builder, dialogue.line?.textEn);
                            AppendHashToken(builder, dialogue.pauseTimeline.ToString());
                            AppendHashToken(builder, ((int)dialogue.advanceMode).ToString(CultureInfo.InvariantCulture));
                            AppendHashToken(builder, dialogue.autoDelay.ToString("R", CultureInfo.InvariantCulture));
                            break;
                        case LunaEffectMarker effect:
                            AppendHashToken(builder, ((int)effect.effectType).ToString(CultureInfo.InvariantCulture));
                            AppendHashToken(builder, effect.targetId);
                            AppendHashToken(builder, effect.stringValue);
                            AppendColor(builder, effect.color);
                            AppendHashToken(builder, effect.duration.ToString("R", CultureInfo.InvariantCulture));
                            AppendHashToken(builder, effect.strength.ToString("R", CultureInfo.InvariantCulture));
                            AppendHashToken(builder, GetAssetIdentity(effect.audioClip));
                            AppendHashToken(builder, effect.fireOnSkip.ToString());
                            AppendHashToken(builder, effect.eventKey);
                            break;
                        case LunaSpriteSwapMarker sprite:
                            AppendHashToken(builder, sprite.targetId);
                            AppendHashToken(builder, GetAssetIdentity(sprite.sprite));
                            AppendHashToken(builder, sprite.flipX.ToString());
                            AppendHashToken(builder, sprite.fireOnSkip.ToString());
                            AppendHashToken(builder, sprite.eventKey);
                            break;
                    }
                }
            }
            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendColor(StringBuilder builder, Color value)
        {
            AppendHashToken(builder, value.r.ToString("R", CultureInfo.InvariantCulture));
            AppendHashToken(builder, value.g.ToString("R", CultureInfo.InvariantCulture));
            AppendHashToken(builder, value.b.ToString("R", CultureInfo.InvariantCulture));
            AppendHashToken(builder, value.a.ToString("R", CultureInfo.InvariantCulture));
        }

        private static string GetAssetIdentity(UnityEngine.Object asset)
        {
            if (asset == null)
                return string.Empty;
            return AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long localId)
                ? $"{guid}:{localId}"
                : asset.name;
        }

        private static void AppendHashToken(StringBuilder builder, string value)
        {
            string safe = value ?? string.Empty;
            builder.Append('|').Append(safe.Length).Append(':').Append(safe);
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
