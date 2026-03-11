using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// ★ 병 인터랙션 (v10)
    /// 
    /// 바텐더가 병을 사용하는 전체 흐름:
    /// 
    /// 1. 좌클릭으로 병을 잡음
    /// 2. 드래그로 타겟 용기 위로 이동
    /// 3. 타겟 근처에서 자동으로 기울기 시작 (또는 스크롤 휠로 수동 조절)
    /// 4. 기울기 ≥ minTiltToPour → PouringSystem 자동 연결 → 액체 전달
    /// 5. 마우스 놓으면 원래 위치로 복귀 + 기울기 리셋
    /// 
    /// 사용법:
    /// 1. 병 오브젝트에 LiquidContainer + BottleInteraction 추가
    /// 2. 씬에 PouringSystem 오브젝트 필요
    /// 3. 타겟 용기(잔 등)는 자동 감지됨
    /// </summary>
    [RequireComponent(typeof(LiquidContainer))]
    public class BottleInteraction : MonoBehaviour
    {
        [Header("Grab")]
        [SerializeField] private KeyCode grabKey = KeyCode.Mouse0;
        [SerializeField] private float dragSmooth = 12f;
        [SerializeField] private float returnSpeed = 6f;

        [Header("Tilt")]
        [SerializeField] private float autoTiltSpeed = 70f;     // 자동 기울기 속도 (도/초)
        [SerializeField] private float manualTiltSpeed = 120f;  // 스크롤 휠 기울기 속도
        [SerializeField] private float maxTiltAngle = 80f;      // 최대 기울기
        [SerializeField] private float untiltSpeed = 100f;      // 복원 속도

        [Header("Target Detection")]
        [Tooltip("병의 하단에서 이 반경 내의 용기를 타겟으로 감지")]
        [SerializeField] private float detectRadius = 2.5f;
        [Tooltip("비어있으면 모든 LiquidContainer 대상. 설정하면 해당 태그만")]
        [SerializeField] private string targetTag = "";

        [Header("Visual")]
        [Tooltip("잡았을 때 살짝 위로 올라가는 높이")]
        [SerializeField] private float liftHeight = 0.5f;

        // 컴포넌트
        private LiquidContainer container;
        private Camera cam;

        // 상태
        private bool grabbed = false;
        private Vector3 originPos;
        private Quaternion originRot;
        private Vector3 grabOffset;

        // 기울기
        private float currentTilt = 0f;
        private int tiltDirection = 0;  // -1=CW(오른쪽), +1=CCW(왼쪽), 0=미정
        private float bottleHeight = 0f; // 병 높이 (피벗 계산용)
        private Vector3 dragTargetPos;   // 기울기 적용 전 드래그 목표 위치

        // 타겟
        private LiquidContainer currentTarget = null;
        private bool pourConnected = false;

        // 타겟 감지 캐시 (매 프레임 FindObjectsOfType 방지)
        private LiquidContainer[] allContainers;
        private float cacheTimer = 0f;
        private const float CACHE_INTERVAL = 0.5f;

        private void Awake()
        {
            container = GetComponent<LiquidContainer>();
            cam = Camera.main;
        }

        private void Start()
        {
            originPos = transform.position;
            originRot = transform.rotation;
            dragTargetPos = originPos;
            RefreshContainerCache();
        }

        /// <summary>
        /// ★ 병 높이 계산 (지연 초기화: LiquidRenderer 초기화 이후에 호출)
        /// </summary>
        private void EnsureBottleHeight()
        {
            if (bottleHeight > 0.1f) return;

            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                bottleHeight = sr.bounds.size.y;

            if (bottleHeight < 0.1f)
            {
                var ren = GetComponentInChildren<LiquidRenderer>();
                if (ren != null && ren.PPU > 0)
                    bottleHeight = (float)ren.GridHeight / ren.PPU;
            }
            if (bottleHeight < 0.1f) bottleHeight = 4f;
        }

        private void Update()
        {
            if (cam == null) return;

            // 주기적 캐시 갱신
            cacheTimer += Time.deltaTime;
            if (cacheTimer >= CACHE_INTERVAL)
            {
                cacheTimer = 0f;
                RefreshContainerCache();
            }

            HandleGrab();

            if (grabbed)
            {
                EnsureBottleHeight();
                HandleDrag();
                DetectTarget();
                HandleTilt();
            }
            else
            {
                HandleReturn();
            }
        }

        private void RefreshContainerCache()
        {
            allContainers = FindObjectsOfType<LiquidContainer>();
        }

        // ============================================================
        // 잡기 / 놓기
        // ============================================================

        private void HandleGrab()
        {
            if (Input.GetKeyDown(grabKey) && !grabbed)
            {
                Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                if (IsOverBottle(mouseWorld))
                {
                    grabbed = true;
                    grabOffset = transform.position - (Vector3)mouseWorld;
                    grabOffset.z = 0f;
                }
            }

            if (Input.GetKeyUp(grabKey) && grabbed)
            {
                grabbed = false;
                DisconnectPour();
            }
        }

        private bool IsOverBottle(Vector2 wp)
        {
            var col = GetComponent<Collider2D>();
            if (col != null) return col.OverlapPoint(wp);

            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) return sr.bounds.Contains((Vector3)wp);

            Vector3 p = transform.position;
            return Mathf.Abs(wp.x - p.x) < 1.5f && Mathf.Abs(wp.y - p.y) < 2.5f;
        }

        // ============================================================
        // 드래그
        // ============================================================

        private void HandleDrag()
        {
            Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = transform.position.z;

            Vector3 target = mouseWorld + grabOffset + Vector3.up * liftHeight;
            dragTargetPos = Vector3.Lerp(dragTargetPos, target, Time.deltaTime * dragSmooth);
            // ★ 위치는 HandleTilt에서 피벗 회전 후 적용
        }

        // ============================================================
        // 타겟 감지
        // ============================================================

        /// <summary>
        /// ★ 병 입구(상단) 근처에서 가장 가까운 LiquidContainer를 찾음
        /// </summary>
        private void DetectTarget()
        {
            if (allContainers == null) return;

            // ★ 검색 기준: 병의 입구(상단) = 피벗 위치
            Vector3 mouthPos = dragTargetPos + Vector3.up * bottleHeight;

            LiquidContainer closest = null;
            float closestDist = detectRadius;

            foreach (var c in allContainers)
            {
                if (c == null || c == container) continue;

                // 태그 필터
                if (!string.IsNullOrEmpty(targetTag) && !c.CompareTag(targetTag))
                    continue;

                // 입구보다 위에 있는 용기는 제외
                if (c.transform.position.y > mouthPos.y + 0.5f)
                    continue;

                // 수평 거리 체크
                float horizDist = Mathf.Abs(c.transform.position.x - mouthPos.x);
                if (horizDist > detectRadius * 1.5f)
                    continue;

                // 받기 위치와의 거리
                Vector3 receivePos = c.GetReceivePosition();
                float dist = Vector3.Distance(mouthPos, receivePos);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = c;
                }
            }

            // 타겟 변경 시 연결 갱신
            if (closest != currentTarget)
            {
                DisconnectPour();
                currentTarget = closest;

                if (currentTarget != null)
                {
                    // 타겟이 있는 쪽으로 기울기 방향 설정
                    float dx = currentTarget.transform.position.x - mouthPos.x;
                    tiltDirection = (dx < 0) ? 1 : -1;
                }
                else
                {
                    tiltDirection = 0;
                }
            }
        }

        // ============================================================
        // 기울기
        // ============================================================

        /// <summary>
        /// ★ 기울기 제어 + 피벗 회전
        /// </summary>
        private void HandleTilt()
        {
            // 스크롤 휠 수동 조절
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
                currentTilt = Mathf.Clamp(currentTilt + scroll * manualTiltSpeed, 0f, maxTiltAngle);

            if (currentTarget != null)
                currentTilt = Mathf.MoveTowards(currentTilt, maxTiltAngle, autoTiltSpeed * Time.deltaTime);
            else
                currentTilt = Mathf.MoveTowards(currentTilt, 0f, untiltSpeed * Time.deltaTime);

            currentTilt = Mathf.Clamp(currentTilt, 0f, maxTiltAngle);

            // ★ 항상 상단 피벗으로 위치/회전 적용
            float angle = (tiltDirection != 0) ? tiltDirection * currentTilt : 0f;
            ApplyPivotTransform(dragTargetPos, angle);

            UpdatePourConnection();
        }

        // ============================================================
        // ★ 상단 피벗 회전 (기울기/복귀 모두 동일)
        // ============================================================

        /// <summary>
        /// 병의 입구(상단)를 피벗으로 회전 적용
        /// basePos = 회전 전 병 하단 위치 (드래그 목표 또는 원래 위치)
        /// angle = Z축 회전 각도 (도)
        /// </summary>
        private void ApplyPivotTransform(Vector3 basePos, float angle)
        {
            // 피벗 = 병 상단 (입구)
            Vector3 pivotWorld = basePos + Vector3.up * bottleHeight;
            Quaternion rot = Quaternion.Euler(0, 0, angle);

            // 하단 오프셋을 회전시켜 새 위치 계산
            // angle=0이면 newPos = basePos (원래 위치 그대로)
            Vector3 bottomOffset = basePos - pivotWorld; // (0, -bottleHeight, 0)
            transform.position = pivotWorld + rot * bottomOffset;
            transform.rotation = rot;
        }

        // ============================================================
        // PouringSystem 연결
        // ============================================================

        private void UpdatePourConnection()
        {
            if (PouringSystem.Instance == null) return;

            bool shouldPour = currentTarget != null
                           && container.IsTiltedEnoughToPour();

            if (shouldPour && !pourConnected)
            {
                if (PouringSystem.Instance.IsPouring
                    && PouringSystem.Instance.Source != container)
                    PouringSystem.Instance.StopPouring();

                PouringSystem.Instance.StartPouring(container, currentTarget);
                pourConnected = true;
            }
            else if (!shouldPour && pourConnected)
            {
                DisconnectPour();
            }
        }

        private void DisconnectPour()
        {
            if (pourConnected && PouringSystem.Instance != null
                && PouringSystem.Instance.Source == container)
                PouringSystem.Instance.StopPouring();
            pourConnected = false;
        }

        // ============================================================
        // 복귀
        // ============================================================

        /// <summary>
        /// 놓으면 원래 위치 + 회전으로 부드럽게 복귀
        /// ★ 복귀 중에도 상단 피벗 사용 → 기울기→직립 전환이 자연스러움
        /// </summary>
        private void HandleReturn()
        {
            EnsureBottleHeight();

            // 드래그 목표를 원래 위치로 복귀
            dragTargetPos = Vector3.Lerp(dragTargetPos, originPos, Time.deltaTime * returnSpeed);

            // 기울기 복원
            currentTilt = Mathf.MoveTowards(currentTilt, 0f, untiltSpeed * Time.deltaTime);

            // ★ 항상 상단 피벗으로 적용 (기울기 0이면 그냥 직립)
            float angle = (tiltDirection != 0) ? tiltDirection * currentTilt : 0f;
            ApplyPivotTransform(dragTargetPos, angle);

            // 완전히 복귀하면 tiltDirection 리셋
            if (currentTilt < 0.3f)
                tiltDirection = 0;

            // 타겟 해제
            if (currentTarget != null)
            {
                DisconnectPour();
                currentTarget = null;
            }
        }

        // ============================================================
        // 공개
        // ============================================================

        public bool IsGrabbed => grabbed;
        public LiquidContainer CurrentTarget => currentTarget;
        public float CurrentTilt => currentTilt;
    }
}
