using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 쉐이커 인터랙션 (v6)
    /// 
    /// ★ 핵심: 용기를 움직이면 ApplyContainerMotion()으로 
    ///   액체에 관성(반대 방향 힘)을 가함 → 액체가 출렁임
    /// ★ 빠르게 흔들면 추가로 ShakeAll()로 강한 혼합 발생
    /// </summary>
    [RequireComponent(typeof(LiquidContainer))]
    public class ShakerInteraction : MonoBehaviour
    {
        [Header("Shake")]
        [SerializeField] private float shakeThreshold = 1.5f;
        [SerializeField] private float maxIntensity = 5f;
        [SerializeField] private KeyCode grabKey = KeyCode.Mouse1;
        [SerializeField] private KeyCode directShakeKey = KeyCode.S;

        [Header("Drag")]
        [SerializeField] private float dragSmooth = 15f;
        [SerializeField] private float returnSpeed = 5f;

        [Header("Visual")]
        [SerializeField] private float rotAmount = 15f;
        [SerializeField] private float rotSpeed = 12f;

        private LiquidContainer container;
        private Camera cam;
        private bool grabbed = false;
        private Vector3 originPos;
        private float intensity = 0f;

        // 이동 추적
        private Vector3 prevPos;
        private Vector3 containerVelocity;
        private Vector3[] velHistory = new Vector3[10];
        private int vi = 0;

        private void Awake()
        {
            container = GetComponent<LiquidContainer>();
            cam = Camera.main;
            originPos = transform.position;
            prevPos = transform.position;
        }

        private void Update()
        {
            if (cam == null) return;
            DoGrab();
            DoMove();
            DoContainerInertia();  // ★ 매 프레임 관성 효과
            DoShakeDetect();       // 빠른 흔들기 감지
            DoDirectShake();
            DoAnim();
        }

        private void DoGrab()
        {
            if (Input.GetKeyDown(grabKey))
            {
                Vector2 mw = cam.ScreenToWorldPoint(Input.mousePosition);
                if (IsOver(mw)) grabbed = true;
            }
            if (Input.GetKeyUp(grabKey)) grabbed = false;
        }

        private bool IsOver(Vector2 wp)
        {
            var c = GetComponent<Collider2D>();
            if (c != null) return c.OverlapPoint(wp);
            var s = GetComponent<SpriteRenderer>();
            if (s != null) return s.bounds.Contains((Vector3)wp);
            Vector3 p = transform.position;
            return wp.x > p.x - 1.5f && wp.x < p.x + 1.5f
                && wp.y > p.y - 2f && wp.y < p.y + 2f;
        }

        private void DoMove()
        {
            Vector3 old = transform.position;

            if (grabbed)
            {
                Vector3 t = cam.ScreenToWorldPoint(Input.mousePosition);
                t.z = transform.position.z;
                transform.position = Vector3.Lerp(transform.position, t, Time.deltaTime * dragSmooth);
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, originPos, Time.deltaTime * returnSpeed);
            }

            // 속도 계산
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            containerVelocity = (transform.position - old) / dt;

            velHistory[vi] = containerVelocity;
            vi = (vi + 1) % velHistory.Length;

            prevPos = old;
        }

        /// <summary>
        /// ★ 용기 이동 시 액체에 관성 효과 적용
        /// 잡고 있든 아니든, 용기가 움직이면 액체가 반응
        /// </summary>
        private void DoContainerInertia()
        {
            if (container.Grid == null) return;

            float speed = containerVelocity.magnitude;
            if (speed > 0.3f)
            {
                // 용기 이동 속도를 그리드에 전달
                container.Grid.ApplyContainerMotion(
                    new Vector2(containerVelocity.x, containerVelocity.y)
                );
            }
        }

        /// <summary>
        /// 빠른 흔들기 감지: 방향 전환이 잦으면 ShakeAll 호출 (강한 혼합)
        /// </summary>
        private void DoShakeDetect()
        {
            if (!grabbed) { intensity *= 0.9f; return; }

            float totalSpd = 0f;
            int dirChanges = 0;
            for (int i = 0; i < velHistory.Length; i++)
            {
                totalSpd += velHistory[i].magnitude;
                if (i > 0)
                {
                    float dot = Vector3.Dot(velHistory[i].normalized, velHistory[i - 1].normalized);
                    if (dot < -0.1f) dirChanges++;
                }
            }
            float avg = totalSpd / velHistory.Length;

            if (avg > shakeThreshold && dirChanges >= 1)
            {
                float target = Mathf.Clamp(avg * (1f + dirChanges * 0.4f) * 0.3f, 0.5f, maxIntensity);
                intensity = Mathf.Lerp(intensity, target, Time.deltaTime * 8f);
                // 추가 혼합 (관성 효과와 별개로)
                container.Shake(intensity * Time.deltaTime * 3f);
            }
            else
            {
                intensity *= 0.92f;
            }
        }

        /// <summary>
        /// S키: 직접 흔들기
        /// </summary>
        private void DoDirectShake()
        {
            if (Input.GetKey(directShakeKey))
            {
                intensity = 3f;
                container.Shake(1.2f * Time.deltaTime * 5f);

                // S키로도 관성 효과 시뮬레이션
                if (container.Grid != null)
                {
                    float fakeVel = Mathf.Sin(Time.time * 12f) * 8f;
                    container.Grid.ApplyContainerMotion(new Vector2(0f, fakeVel));
                }
            }
        }

        private void DoAnim()
        {
            // ★ 흔들기 중일 때만 회전 애니메이션 적용
            // 그렇지 않으면 수동 기울기(따르기)를 방해하지 않음
            if (intensity < 0.1f) return;

            float n = Mathf.Clamp01(intensity / maxIntensity);
            float tgt = Mathf.Sin(Time.time * rotSpeed * (1f + n)) * rotAmount * n;
            float cur = transform.eulerAngles.z;
            if (cur > 180f) cur -= 360f;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(cur, tgt, Time.deltaTime * rotSpeed));
        }

        public float GetNormalizedShakeIntensity() => Mathf.Clamp01(intensity / maxIntensity);
        public bool IsGrabbed => grabbed;
    }
}
