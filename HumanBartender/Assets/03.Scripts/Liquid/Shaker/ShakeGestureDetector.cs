using UnityEngine;

namespace LiquidSimulation
{
    public class ShakeGestureDetector : MonoBehaviour
    {
        [Header("SO Events")]
        [SerializeField] private VoidEvent onShakeEvent;
        [SerializeField] private VoidEvent onPenaltyEvent;
        [SerializeField] private IntEvent shakeAnimEvent;
        [SerializeField] private VoidEvent shakeAnimResetEvent;

        [Header("Dot Layout")]
        [SerializeField] private Vector2 dotTop = new Vector2(0.6f, 0.8f);
        [SerializeField] private Vector2 dotMid = new Vector2(-0.3f, 0f);
        [SerializeField] private Vector2 dotBot = new Vector2(0.6f, -0.8f);
        [SerializeField] private float dotRadius = 0.4f;


        [Header("Detection")]
        [SerializeField] private float activationRadius = 2.0f;

        // 상태
        private bool isDragging = false;
        private Camera cam;


        // 시퀀스 추적
        // phase:
        //   0 = 미시작 (중앙 대기)
        //   1 = 끝점(0 or 2)으로 이동 중
        //   2 = 끝점 도달, 중앙(1)으로 복귀 중
        private int phase = 0;
        private int targetDot = -1;       // 현재 향하는 점
        private int lastReached = -1;     // 마지막 도달 점
        private int lastEndDot = -1;      // 마지막으로 도달한 끝점 (0 or 2)

        private Camera Cam => cam != null ? cam : (cam = Camera.main);

        private void Start()
        {
            cam = Camera.main;
        }

        private void Update()
        {
            if (Cam == null) return;

            Vector2 mouseWorld = Cam.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                float dist = Vector2.Distance(mouseWorld, (Vector2)transform.position);
                if (dist < activationRadius)
                {
                    isDragging = true;
                    FullReset();
                }
            }
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
                FullReset();
            }

            if (!isDragging) return;

            CheckDotHit(mouseWorld);
        }



        private void CheckDotHit(Vector2 mousePos)
        {
            for (int i = 0; i < 3; i++) //상단, 중앙, 하단 순
            {
                float dist = Vector2.Distance(mousePos, GetDotWorldPosition(i));
                if (dist > dotRadius) continue; // 거리체크
                if (i == lastReached) continue; // 같은 점 체류

                //도달 완.

                switch (phase)
                {
                    case 0: // 미시작 → 중앙(1) 터치로 시작
                        if (i == 1)
                        {
                            shakeAnimResetEvent?.Raise(new Void());
                            lastReached = 1;
                            targetDot = 0;   // 첫 이동: 위로
                            phase = 1;
                        }
                        break;

                    case 1: // 끝점으로 이동 중
                        if (i == targetDot && (i == 0 || i == 2))
                        {
                            // ★ 끝점 도달
                            lastReached = i;
                            lastEndDot = i;
                            targetDot = 1;   // 중앙으로 복귀
                            phase = 2;
                            shakeAnimEvent?.Raise(phase);
                        }
                        else if (i == 1)
                        {
                            // 아직 끝점 안 갔는데 중앙 → 무시 (통과)
                        }
                        
                        break;

                    case 2: // 중앙으로 복귀 중
                        if (i == 1)
                        {
                            // ★ 중앙 복귀 = 1회 쉐이킹!
                            lastReached = 1;
                            if (onShakeEvent != null) onShakeEvent?.Raise(new Void());

                            // 다음: 반대쪽 끝점으로
                            targetDot = (lastEndDot == 0) ? 2 : 0;
                            phase = 1;

                            shakeAnimEvent?.Raise(phase);
                        }
                        
                        break;
                }
            }
        }

        private void FullReset()
        {
            phase = 0;
            targetDot = -1;
            lastReached = -1;
            lastEndDot = -1;
        }

        // ============================================================
        // 외부 접근
        // ============================================================

        public Vector2 GetDotWorldPosition(int index)
        {
            Vector2 local;
            switch (index)
            {
                case 0: local = dotTop; break;
                case 1: local = dotMid; break;
                case 2: local = dotBot; break;
                default: return (Vector2)transform.position;
            }
            return (Vector2)transform.TransformPoint(local);
        }

        public bool IsDragging => isDragging;
        public int DotCount => 3;
        public int CurrentTarget => targetDot;
        public int LastReached => lastReached;
        public int LastEndDot => lastEndDot;
        public int Phase => phase;

        /// <summary>
        /// 현재 구간의 진행도 (0~1).
        /// lastReached → targetDot 구간에서 마우스가 얼마나 진행했는지.
        /// </summary>
        public float GetProgress(Vector2 mouseWorldPos)
        {
            if (lastReached < 0 || targetDot < 0) return 0f;

            Vector2 from = GetDotWorldPosition(lastReached);
            Vector2 to = GetDotWorldPosition(targetDot);
            Vector2 dir = to - from;
            float segLen = dir.magnitude;
            if (segLen < 0.01f) return 0f;

            float t = Vector2.Dot(mouseWorldPos - from, dir.normalized) / segLen;
            return Mathf.Clamp01(t);
        }
    }
}
