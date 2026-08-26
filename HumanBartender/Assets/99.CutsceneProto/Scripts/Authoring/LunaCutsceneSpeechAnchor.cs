using UnityEngine;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    /// <summary>
    /// 캐릭터 애니메이션과 무관하게 고정된 말풍선 월드 위치를 제공한다.
    /// 캐릭터 루트 아래의 SpeechAnchor 자식에 붙이는 것을 기본으로 한다.
    /// </summary>
    public sealed class LunaCutsceneSpeechAnchor : MonoBehaviour
    {
        [SerializeField] private Vector3 localOffset;

        public Vector3 WorldPosition => transform.TransformPoint(localOffset);
        public Vector3 LocalOffset => localOffset;

        public void Configure(Vector3 offset)
        {
            localOffset = offset;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.25f, 1f, 0.92f, 0.9f);
            Gizmos.DrawWireSphere(WorldPosition, 0.08f);
            if (localOffset.sqrMagnitude > 0.0001f)
                Gizmos.DrawLine(transform.position, WorldPosition);
        }
    }
}
