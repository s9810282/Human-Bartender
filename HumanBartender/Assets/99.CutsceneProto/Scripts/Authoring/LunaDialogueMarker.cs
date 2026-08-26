using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [DisplayName("L.U.N.A / 대사")]
    [Serializable]
    public sealed class LunaDialogueMarker : Marker, INotification, INotificationOptionProvider
    {
        public LunaLocalizedLine line = new();
        [Tooltip("활성화하면 마커에서 Timeline을 멈추고 대사가 끝난 뒤 같은 지점부터 재개합니다.")]
        public bool pauseTimeline = true;
        public LunaDialogueAdvanceMode advanceMode = LunaDialogueAdvanceMode.PlayerInput;
        [Min(0f)] public float autoDelay = 1.5f;
        [Tooltip("읽음 기록과 디버그 로그에 사용하는 고정 ID입니다.")]
        public string dialogueId;

        public PropertyName id => new("LUNA_DIALOGUE");
        public NotificationFlags flags => NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;
    }
}
