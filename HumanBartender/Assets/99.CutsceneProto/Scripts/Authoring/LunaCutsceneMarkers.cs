using System;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public enum LunaDialogueAdvanceMode
    {
        PlayerInput,
        Auto
    }

    public enum LunaCutsceneEffectType
    {
        FadeIn,
        FadeOut,
        Flash,
        CameraShake,
        SetActive,
        SetSpriteColor,
        PlaySfx,
        SetFlag
    }

    [Serializable]
    public sealed class LunaLocalizedLine
    {
        public string speakerId;
        public string speakerKo;
        public string speakerEn;
        [TextArea(2, 6)] public string textKo;
        [TextArea(2, 6)] public string textEn;
    }

}
