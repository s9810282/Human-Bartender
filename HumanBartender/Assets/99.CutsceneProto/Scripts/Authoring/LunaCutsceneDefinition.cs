using System;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public enum LunaCutscenePlayMode
    {
        Once,
        Repeatable
    }

    public enum LunaBakedEventKind
    {
        Dialogue,
        Effect,
        SpriteSwap
    }

    [Serializable]
    public sealed class LunaBakedCutsceneEvent
    {
        public double time;
        public LunaBakedEventKind kind;

        public LunaLocalizedLine line;
        public bool pauseTimeline;
        public LunaDialogueAdvanceMode advanceMode;
        public float autoDelay;
        public string dialogueId;

        public LunaCutsceneEffectType effectType;
        public string targetId;
        public string stringValue;
        public Color color = Color.white;
        public float duration;
        public float strength;
        public AudioClip audioClip;
        public bool fireOnSkip;
        public string eventKey;

        public Sprite sprite;
        public bool flipX;
    }

    [Serializable]
    public sealed class LunaCutsceneEndBinding
    {
        [LunaBindingId]
        public string targetId;
        public bool applyActive = true;
        public bool active = true;
        public bool applyPosition = true;
        public Vector3 localPosition;
        public bool applySprite;
        public Sprite sprite;
    }

    [CreateAssetMenu(menuName = "Project L.U.N.A/Cutscene/Definition", fileName = "CutsceneDefinition")]
    public sealed class LunaCutsceneDefinition : ScriptableObject
    {
        [Header("식별")]
        public string cutsceneId = "cutscene_new";
        public string titleKo = "새 컷씬";
        public string titleEn = "New Cutscene";

        [Header("재생")]
        public LunaCutscenePlayMode playMode = LunaCutscenePlayMode.Once;
        public bool autoPlay = true;
        public bool skippable = true;
        public bool startFromBlack = true;
        public string requiredWhen;

        [Header("Cutscene Presentation")]
        [Tooltip("Reusable letterbox, pixel-perfect zoom and end fade settings.")]
        public LunaCutscenePresentationPreset presentationPreset;

        [Header("종료 상태")]
        public LunaCutsceneEndBinding[] endBindings = Array.Empty<LunaCutsceneEndBinding>();
        public bool fadeToBlackOnEnd;
        public string completionFlag;

        [Header("Timeline 런타임 베이크")]
        [Tooltip("Cutscene Authoring Studio의 검사 버튼이 Timeline 마커에서 자동 생성합니다. 직접 편집하지 않습니다.")]
        public LunaBakedCutsceneEvent[] bakedEvents = Array.Empty<LunaBakedCutsceneEvent>();
        public double bakedTimelineDuration;
        [HideInInspector] public string bakedTimelineHash;

        public void Configure(string id, string ko, string en)
        {
            cutsceneId = id;
            titleKo = ko;
            titleEn = en;
        }

        public void SetBakedEvents(LunaBakedCutsceneEvent[] events, double duration, string contentHash)
        {
            bakedEvents = events ?? Array.Empty<LunaBakedCutsceneEvent>();
            bakedTimelineDuration = Math.Max(0d, duration);
            bakedTimelineHash = contentHash ?? string.Empty;
        }
    }
}
