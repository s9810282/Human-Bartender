using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// ★ 원형 제스처 감지기
    /// 
    /// 6개 점이 원형으로 배치되고, 좌클릭 드래그로 시계방향 회전을 감지.
    /// 
    /// 감지 방식 (섹터 기반):
    /// - 원을 6개 섹터(60°)로 분할
    /// - 마우스가 새 섹터에 진입하면 방향 판별
    /// - 시계방향 순서로 6개 섹터를 모두 통과하면 → onStirEvent 발행
    /// - 도중에 반시계방향으로 돌면 → onPenaltyEvent 발행
    /// 
    /// 좌클릭 유지 상태에서 연속 회전 가능 (한 바퀴 → 이벤트 → 다음 바퀴)
    /// 
    /// 배치:
    ///       ● (0)  ← 12시
    ///   (5) ●    ● (1)  ← 2시
    ///   (4) ●    ● (2)  ← 4시
    ///       ● (3)  ← 6시
    /// </summary>
    public class CircularGestureDetector : MonoBehaviour
    {
        // ============================================================
        // 인스펙터 설정
        // ============================================================

        [Header("SO Events (에셋 연결)")]
        [Tooltip("시계방향 한 바퀴 완성 시 발행")]
        [SerializeField] private VoidEvent onStirEvent;
        [Tooltip("방향 전환(반시계) 감지 시 발행")]
        [SerializeField] private VoidEvent onPenaltyEvent;

        [Header("Circle Layout")]
        [Tooltip("점들이 배치되는 원의 반지름 (월드 단위)")]
        [SerializeField] private float circleRadius = 1.2f;
        [Tooltip("이 반지름 안쪽은 무시 (원 중심 데드존)")]
        [SerializeField] private float innerDeadzone = 0.3f;
        [Tooltip("점 개수")]
        [SerializeField] private int dotCount = 6;

        [Header("Detection")]
        [Tooltip("첫 섹터 진입 후 이 초 안에 한 바퀴 못 돌면 리셋")]
        [SerializeField] private float roundTimeout = 3f;
        [Tooltip("연속 페널티 방지: 페널티 후 이 초간 쿨다운")]
        [SerializeField] private float penaltyCooldown = 0.5f;

        // ============================================================
        // 상태
        // ============================================================

        // 드래그
        private bool isDragging = false;
        private Camera cam;

        // 섹터 추적
        private int currentSector = -1;     // 마우스가 있는 섹터 (-1=원 밖)
        private int prevSector = -1;        // 직전 섹터
        private bool[] sectorVisited;       // 이번 바퀴에서 방문한 섹터
        private int sectorsVisitedCount;    // 방문한 섹터 수
        private int startSector = -1;       // 이번 바퀴 시작 섹터
        private int expectedNext = -1;      // 다음에 방문해야 할 섹터

        // 방향
        private int lastDirection = 0;      // +1=시계, -1=반시계, 0=미정

        // 타이밍
        private float roundStartTime;
        private float lastPenaltyTime;

        // ============================================================
        // 초기화
        // ============================================================

        private void Start()
        {
            cam = Camera.main;
            sectorVisited = new bool[dotCount];
            FullReset();
        }

        // ============================================================
        // 업데이트
        // ============================================================

        private void Update()
        {
            if (cam == null) return;

            Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 center = (Vector2)transform.position;
            Vector2 offset = mouseWorld - center;
            float dist = offset.magnitude;

            // --- 드래그 시작/끝 ---
            if (Input.GetMouseButtonDown(0))
            {
                // 원 영역 부근에서 시작
                if (dist < circleRadius * 1.5f)
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

            // --- 타임아웃 ---
            if (sectorsVisitedCount > 0 && Time.time - roundStartTime > roundTimeout)
                ResetRound();

            // ★ 마지막 점(시작점) 통과 대기: 매 프레임 각도 체크
            // 시작점 각도를 시계방향으로 넘어가야 완성
            if (sectorsVisitedCount >= dotCount - 1 && startSector >= 0)
            {
                Vector2 mouseCheck = cam.ScreenToWorldPoint(Input.mousePosition);
                float mouseAngle = GetClockwiseAngle(mouseCheck);
                float startDotAngle = GetSectorAngle(startSector);

                // 시작점 각도 대비 얼마나 지났는지 (시계방향 기준)
                float overshoot = (mouseAngle - startDotAngle + 360f) % 360f;
                float sectorSize = 360f / dotCount;

                // overshoot이 작은 양수 = 점을 막 넘김
                // overshoot이 큰 값(300+) = 아직 도착 전 (반대편)
                // ★ 점을 넘어서 sectorSize * 0.15f (약 9°) 이상 지나야 완성
                if (overshoot > 0f && overshoot < sectorSize * 0.5f)
                {
                    if (onStirEvent != null) onStirEvent.Raise(new Void());
                    Debug.Log("[Gesture] Stir!");
                    ResetRound();
                    StartNewRound(currentSector >= 0 ? currentSector : startSector);
                    return;
                }
            }

            // --- 섹터 계산 ---
            if (dist < innerDeadzone) return; // 중심 데드존

            int sector = GetSector(offset);
            if (sector == currentSector) return; // 같은 섹터 안에서 이동 → 무시

            prevSector = currentSector;
            currentSector = sector;

            // --- 섹터 전환 처리 ---
            ProcessSectorTransition();
        }

        // ============================================================
        // ★ 핵심: 섹터 전환 처리
        // ============================================================

        private void ProcessSectorTransition()
        {
            if (prevSector < 0)
            {
                // 첫 진입: 아무 섹터나 시작점
                StartNewRound(currentSector);
                return;
            }

            // --- 방향 판별 ---
            int diff = currentSector - prevSector;
            // 순환 보정: 5→0 은 +1(시계), 0→5 는 -1(반시계)
            if (diff > dotCount / 2) diff -= dotCount;
            if (diff < -dotCount / 2) diff += dotCount;

            // 인접 섹터가 아닌 경우 (점프) → 무시
            if (Mathf.Abs(diff) != 1) return;

            int direction = (diff > 0) ? 1 : -1; // +1=시계, -1=반시계

            // --- 방향 전환 감지 (페널티) ---
            if (lastDirection != 0 && direction != lastDirection)
            {
                if (Time.time - lastPenaltyTime >= penaltyCooldown)
                {
                    if (onPenaltyEvent != null) onPenaltyEvent.Raise(new Void());
                    lastPenaltyTime = Time.time;
                    Debug.Log("[Gesture] Penalty! Direction reversed");
                }
                ResetRound();
                // 방향 전환 후 현재 위치에서 새 라운드 시작
                StartNewRound(currentSector);
                return;
            }

            lastDirection = direction;

            // --- 시계방향이 아니면 무시 (반시계 연속은 그냥 무시) ---
            // 이미 위에서 방향 전환은 잡았으므로 여기선 반시계 연속 = 그냥 유지
            // 정책: 시계방향만 카운트
            if (direction != 1) return;

            // --- 시계방향 섹터 방문 기록 ---
            if (expectedNext >= 0 && currentSector != expectedNext)
            {
                // 기대 섹터가 아님 → 건너뜀 (정상적이지 않은 경로)
                ResetRound();
                StartNewRound(currentSector);
                return;
            }

            // ★ 마지막 섹터(시작점 복귀)는 여기서 방문 처리하지 않음
            // Update의 거리 체크로만 완성됨
            if (currentSector == startSector && sectorsVisitedCount >= dotCount - 1)
                return;

            if (!sectorVisited[currentSector])
            {
                sectorVisited[currentSector] = true;
                sectorsVisitedCount++;
            }

            expectedNext = (currentSector + 1) % dotCount;
        }

        // ============================================================
        // 라운드 관리
        // ============================================================

        private void StartNewRound(int sector)
        {
            startSector = sector;
            // ★ 시작점은 방문 처리하지 않음
            // 한 바퀴 돌아서 다시 이 섹터에 도착해야 완성
            sectorsVisitedCount = 0;
            expectedNext = (sector + 1) % dotCount;
            roundStartTime = Time.time;
        }

        private void ResetRound()
        {
            for (int i = 0; i < dotCount; i++)
                sectorVisited[i] = false;
            sectorsVisitedCount = 0;
            startSector = -1;
            expectedNext = -1;
            lastDirection = 0;
            // ★ currentSector, prevSector는 리셋하지 않음
            // ProcessSectorTransition → StartNewRound(currentSector)에서
            // 현재 섹터를 재사용하므로 -1로 만들면 인덱스 에러
        }

        /// <summary>
        /// 드래그 종료 시에만 전체 리셋
        /// </summary>
        private void FullReset()
        {
            ResetRound();
            currentSector = -1;
            prevSector = -1;
        }

        // ============================================================
        // 섹터 계산
        // ============================================================

        /// <summary>
        /// offset 벡터로부터 섹터 인덱스 계산 (0~dotCount-1)
        /// 
        /// 12시=섹터0, 시계방향으로 증가
        /// 
        /// atan2 → 수학 각도 (반시계, 3시=0°)
        /// → 시계방향 각도로 변환 (12시=0°)
        /// → 섹터 분할
        /// </summary>
        private int GetSector(Vector2 offset)
        {
            float angleDeg = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            // 수학 각도(반시계, 3시=0) → 시계각도(시계, 12시=0)
            float clockwiseDeg = (90f - angleDeg + 360f) % 360f;
            float sectorSize = 360f / dotCount;
            int sector = Mathf.FloorToInt((clockwiseDeg + sectorSize * 0.5f) / sectorSize) % dotCount;
            return sector;
        }

        // ============================================================
        // 외부 접근
        // ============================================================

        /// <summary>
        /// 현재 마우스가 위치한 섹터 (-1이면 원 밖)
        /// </summary>
        public int CurrentSector => currentSector;

        /// <summary>
        /// 이번 바퀴에서 해당 섹터를 방문했는지
        /// </summary>
        public bool IsSectorVisited(int index)
        {
            if (index < 0 || index >= dotCount) return false;
            return sectorVisited[index];
        }

        /// <summary>
        /// 다음 방문해야 할 섹터
        /// </summary>
        public int ExpectedNext => expectedNext;

        /// <summary>
        /// 드래그 중인지
        /// </summary>
        public bool IsDragging => isDragging;

        /// <summary>
        /// 점의 월드 좌표 (시각용)
        /// </summary>
        public Vector3 GetDotWorldPosition(int index)
        {
            float sectorSize = 360f / dotCount;
            float clockwiseDeg = sectorSize * index;
            float mathDeg = 90f - clockwiseDeg;
            float rad = mathDeg * Mathf.Deg2Rad;
            Vector3 local = new Vector3(
                Mathf.Cos(rad) * circleRadius,
                Mathf.Sin(rad) * circleRadius,
                0f
            );
            return transform.TransformPoint(local);
        }

        public int DotCount => dotCount;
        public float CircleRadius => circleRadius;
        public int StartSector => startSector;
        public int SectorsVisitedCount => sectorsVisitedCount;

        /// <summary>
        /// 시계방향 각도(0~360, 12시=0)를 반환.
        /// View에서 마우스 위치 기반 부분 채우기에 사용.
        /// </summary>
        public float GetClockwiseAngle(Vector2 worldPos)
        {
            Vector2 center = (Vector2)transform.position;
            Vector2 offset = worldPos - center;
            float angleDeg = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            return (90f - angleDeg + 360f) % 360f;
        }

        /// <summary>
        /// 섹터 인덱스 → 시계방향 각도 (섹터 중심)
        /// </summary>
        public float GetSectorAngle(int sector)
        {
            return (360f / dotCount) * sector;
        }
    }
}
