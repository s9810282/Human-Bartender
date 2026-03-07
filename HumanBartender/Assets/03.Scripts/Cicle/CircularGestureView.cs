using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// ★ 원형 제스처 시각화 (링 프로그레스 방식)
    /// 
    /// - 점 6개를 선으로 연결한 원형 경로
    /// - 진행도에 따라 체력바처럼 색이 채워짐
    /// - 배경 링(어두운) + 진행 링(밝은) 이중 구조
    /// - 마우스 위치에 따라 현재 구간도 부분 채우기
    /// </summary>
    public class CircularGestureView : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private CircularGestureDetector detector;

        [Header("Ring")]
        [SerializeField] private Color ringBgColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color ringFillColor = new Color(0.3f, 0.85f, 1f, 1f);
        [SerializeField] private float ringBgWidth = 0.06f;
        [SerializeField] private float ringFillWidth = 0.09f;
        [Tooltip("원 경로의 부드러움 (구간당 포인트 수)")]
        [SerializeField] private int pointsPerSegment = 8;

        [Header("Dots")]
        [SerializeField] private Color dotIdle = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color dotVisited = new Color(0.3f, 0.85f, 1f, 1f);
        [SerializeField] private float dotScale = 0.22f;

        [Header("Stick")]
        [SerializeField] private bool showStick = true;
        [SerializeField] private Color stickColor = new Color(0.5f, 0.4f, 0.3f, 1f);
        [SerializeField] private float stickWidth = 0.05f;

        [Header("Sorting")]
        [SerializeField] private int sortingOrder = 20;

        // 내부
        private int dotCount;
        private int totalRingPoints;   // 전체 링 포인트 수

        // 오브젝트
        private SpriteRenderer[] dotRenderers;
        private LineRenderer bgRing;     // 배경 링 (항상 전체)
        private LineRenderer fillRing;   // 진행 링 (부분)
        private LineRenderer stickLine;
        private Sprite circleSprite;
        private Camera cam;

        // 캐시: 전체 원형 경로 좌표 (로컬)
        private Vector3[] fullRingPoints;

        // ============================================================
        // 초기화
        // ============================================================

        private void Start()
        {
            cam = Camera.main;
            if (detector == null)
                detector = GetComponent<CircularGestureDetector>();
            if (detector == null)
            {
                Debug.LogError("[GestureView] CircularGestureDetector not found!");
                return;
            }

            dotCount = detector.DotCount;
            totalRingPoints = dotCount * pointsPerSegment;

            circleSprite = MakeCircleSprite(32);
            BuildRingPath();
            CreateBgRing();
            CreateFillRing();
            CreateDots();
            if (showStick) CreateStick();
        }

        /// <summary>
        /// ★ 전체 원형 경로 포인트 생성 (시계방향, 12시 시작)
        /// dotCount * pointsPerSegment 개의 포인트
        /// </summary>
        private void BuildRingPath()
        {
            fullRingPoints = new Vector3[totalRingPoints + 1]; // +1: 닫힘
            for (int i = 0; i <= totalRingPoints; i++)
            {
                // 시계방향 각도: 0 → 360
                float clockwiseDeg = (360f / totalRingPoints) * i;
                float mathDeg = 90f - clockwiseDeg;
                float rad = mathDeg * Mathf.Deg2Rad;
                fullRingPoints[i] = new Vector3(
                    Mathf.Cos(rad) * detector.CircleRadius,
                    Mathf.Sin(rad) * detector.CircleRadius,
                    0f
                );
            }
        }

        private void CreateBgRing()
        {
            var obj = new GameObject("RingBg");
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            bgRing = obj.AddComponent<LineRenderer>();
            bgRing.useWorldSpace = true;
            bgRing.loop = false;
            bgRing.sortingOrder = sortingOrder - 2;
            bgRing.material = new Material(Shader.Find("Sprites/Default"));
            bgRing.startColor = ringBgColor;
            bgRing.endColor = ringBgColor;
            bgRing.startWidth = ringBgWidth;
            bgRing.endWidth = ringBgWidth;

            // 전체 원 그리기
            bgRing.positionCount = fullRingPoints.Length;
            for (int i = 0; i < fullRingPoints.Length; i++)
                bgRing.SetPosition(i, detector.transform.TransformPoint(fullRingPoints[i]));
        }

        private void CreateFillRing()
        {
            var obj = new GameObject("RingFill");
            obj.transform.parent = transform;
            obj.transform.localPosition = Vector3.zero;

            fillRing = obj.AddComponent<LineRenderer>();
            fillRing.useWorldSpace = true;
            fillRing.loop = false;
            fillRing.sortingOrder = sortingOrder - 1;
            fillRing.material = new Material(Shader.Find("Sprites/Default"));
            fillRing.startColor = ringFillColor;
            fillRing.endColor = ringFillColor;
            fillRing.startWidth = ringFillWidth;
            fillRing.endWidth = ringFillWidth;
            fillRing.positionCount = 0; // 초기에는 비어있음
        }

        private void CreateDots()
        {
            dotRenderers = new SpriteRenderer[dotCount];
            for (int i = 0; i < dotCount; i++)
            {
                var obj = new GameObject($"GestureDot_{i}");
                obj.transform.parent = transform;
                obj.transform.position = detector.GetDotWorldPosition(i);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = circleSprite;
                sr.color = dotIdle;
                sr.sortingOrder = sortingOrder;
                sr.transform.localScale = Vector3.one * dotScale;
                dotRenderers[i] = sr;
            }
        }

        private void CreateStick()
        {
            var obj = new GameObject("GestureStick");
            obj.transform.parent = transform;

            stickLine = obj.AddComponent<LineRenderer>();
            stickLine.positionCount = 2;
            stickLine.useWorldSpace = true;
            stickLine.sortingOrder = sortingOrder + 1;
            stickLine.material = new Material(Shader.Find("Sprites/Default"));
            stickLine.startColor = stickColor;
            stickLine.endColor = stickColor;
            stickLine.startWidth = stickWidth;
            stickLine.endWidth = stickWidth * 0.7f;
        }

        // ============================================================
        // 업데이트
        // ============================================================

        private void LateUpdate()
        {
            if (detector == null) return;

            UpdateBgRing();
            UpdateFillRing();
            UpdateDots();
            if (showStick) UpdateStick();
        }

        /// <summary>
        /// 배경 링 위치 갱신 (부모 이동 대응)
        /// </summary>
        private void UpdateBgRing()
        {
            for (int i = 0; i < fullRingPoints.Length; i++)
                bgRing.SetPosition(i, detector.transform.TransformPoint(fullRingPoints[i]));
        }

        /// <summary>
        /// ★ 핵심: 진행 링을 체력바처럼 채우기
        /// 
        /// 계산:
        /// 1. 시작 섹터 → 시작 각도 (링에서 어디부터 채울지)
        /// 2. 방문한 섹터 수 → 완료된 구간
        /// 3. 마우스 각도 → 현재 구간 부분 채우기
        /// 4. 해당 범위의 포인트만 fillRing에 설정
        /// </summary>
        private void UpdateFillRing()
        {
            if (!detector.IsDragging || detector.SectorsVisitedCount == 0)
            {
                fillRing.positionCount = 0;
                return;
            }

            int start = detector.StartSector;
            if (start < 0) { fillRing.positionCount = 0; return; }

            // --- 시작 포인트 인덱스 (링 배열에서) ---
            int startPointIndex = start * pointsPerSegment;

            // --- 끝 포인트 계산 ---
            // 방문한 섹터 수 = 이동한 구간 수 (시작점 미포함)
            int completedPoints = detector.SectorsVisitedCount * pointsPerSegment;

            // 마우스의 현재 각도로 추가 포인트 (현재 구간 부분 채우기)
            int extraPoints = 0;
            if (cam != null && detector.ExpectedNext >= 0)
            {
                Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                float mouseAngle = detector.GetClockwiseAngle(mouseWorld);

                // 현재 구간: expectedNext-1(방문 완료) ~ expectedNext(미방문)
                float sectorStartAngle = detector.GetSectorAngle(detector.ExpectedNext);
                // 이전 섹터(마지막 방문)의 각도
                int lastVisited = (detector.ExpectedNext - 1 + dotCount) % dotCount;
                float lastVisitedAngle = detector.GetSectorAngle(lastVisited);

                // 마우스가 현재 구간 안에 있으면 부분 채우기
                float sectorSize = 360f / dotCount;
                float progressInSector = (mouseAngle - lastVisitedAngle + 360f) % 360f;

                if (progressInSector < sectorSize)
                {
                    float t = progressInSector / sectorSize;
                    extraPoints = Mathf.RoundToInt(t * pointsPerSegment);
                }
            }

            int totalFillPoints = completedPoints + extraPoints;
            // 최대값 제한
            totalFillPoints = Mathf.Min(totalFillPoints, totalRingPoints);

            if (totalFillPoints < 2) { fillRing.positionCount = 0; return; }

            // --- 포인트 설정 ---
            fillRing.positionCount = totalFillPoints;
            for (int i = 0; i < totalFillPoints; i++)
            {
                int ringIdx = (startPointIndex + i) % (totalRingPoints + 1);
                // 마지막 인덱스(닫힘 포인트) 보정
                if (ringIdx >= fullRingPoints.Length) ringIdx = 0;
                fillRing.SetPosition(i, detector.transform.TransformPoint(fullRingPoints[ringIdx]));
            }
        }

        private void UpdateDots()
        {
            for (int i = 0; i < dotCount; i++)
            {
                if (dotRenderers[i] == null) continue;
                dotRenderers[i].transform.position = detector.GetDotWorldPosition(i);

                if (detector.IsSectorVisited(i))
                    dotRenderers[i].color = dotVisited;
                else if (i == detector.ExpectedNext && detector.IsDragging)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                    dotRenderers[i].color = Color.Lerp(dotIdle, dotVisited, pulse * 0.4f);
                }
                else
                    dotRenderers[i].color = dotIdle;
            }
        }

        private void UpdateStick()
        {
            if (stickLine == null || cam == null) return;
            Vector3 center = detector.transform.position;

            if (detector.IsDragging)
            {
                Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorld.z = center.z;
                Vector2 dir = (Vector2)(mouseWorld - center);
                float len = Mathf.Min(dir.magnitude, detector.CircleRadius * 0.85f);
                Vector3 tip = center + (Vector3)(dir.normalized * len);
                stickLine.SetPosition(0, tip);
                stickLine.SetPosition(1, center);
                stickLine.enabled = true;
            }
            else
            {
                stickLine.SetPosition(0, center);
                stickLine.SetPosition(1, center + Vector3.down * detector.CircleRadius * 0.4f);
            }
        }

        // ============================================================
        // 유틸
        // ============================================================

        private Sprite MakeCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

            float c = size * 0.5f, r = size * 0.4f;
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    if (d <= r) px[y * size + x] = new Color32(255, 255, 255, 255);
                    else if (d <= r + 1.5f)
                        px[y * size + x] = new Color32(255, 255, 255,
                            (byte)(255 * Mathf.Clamp01(1f - (d - r) / 1.5f)));
                    else px[y * size + x] = new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
