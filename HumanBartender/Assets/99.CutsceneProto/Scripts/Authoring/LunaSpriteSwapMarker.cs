using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [DisplayName("L.U.N.A / 스프라이트 교체")]
    [Serializable]
    public sealed class LunaSpriteSwapMarker : Marker, INotification, INotificationOptionProvider
    {
        [Tooltip("Binding Registry에 등록된 SpriteRenderer 대상 ID입니다.")]
        [LunaBindingId]
        public string targetId;
        public Sprite sprite;
        public bool flipX;
        public bool fireOnSkip;
        public string eventKey;

        public PropertyName id => new("LUNA_SPRITE_SWAP");
        public NotificationFlags flags => NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;
    }
}
