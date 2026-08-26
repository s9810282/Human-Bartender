using System;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    [DisplayName("L.U.N.A / 효과")]
    [Serializable]
    public sealed class LunaEffectMarker : Marker, INotification, INotificationOptionProvider
    {
        public LunaCutsceneEffectType effectType;
        [Tooltip("Binding Registry에 등록된 대상 ID입니다. 대상이 필요 없는 효과는 비워둡니다.")]
        public string targetId;
        public string stringValue;
        public Color color = Color.white;
        [Min(0f)] public float duration = 0.25f;
        [Min(0f)] public float strength = 0.2f;
        public AudioClip audioClip;
        [Tooltip("스킵해도 반드시 실행해야 하는 상태 변경에만 활성화합니다.")]
        public bool fireOnSkip;
        [Tooltip("중복 실행을 막는 ID입니다. fireOnSkip 사용 시 필수입니다.")]
        public string eventKey;

        public PropertyName id => new("LUNA_EFFECT");
        public NotificationFlags flags => NotificationFlags.TriggerOnce | NotificationFlags.Retroactive;
    }
}
