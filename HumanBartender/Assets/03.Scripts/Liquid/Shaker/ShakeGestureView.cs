using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// ★ 쉐이킹 제스처 시각화
    /// 
    /// 3점 지그재그 경로를 배경선 + 진행선으로 표시.
    /// Detector에서 상태를 읽어 색/위치를 갱신.
    ///
    ///       ●[0]
    ///      /
    ///     / ← 이 선이 진행도에 따라 채워짐
    ///    ●[1]
    ///     \
    ///      \
    ///       ●[2]
    /// </summary>
    public class ShakeGestureView : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private ShakeGestureDetector detector;

        [Header("Path Line")]
        [SerializeField] private Color pathBgColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
        [SerializeField] private Color pathFillColor = new Color(1f, 0.6f, 0.2f, 1f);
        [SerializeField] private float pathBgWidth = 0.06f;
        [SerializeField] private float pathFillWidth = 0.09f;
        [Tooltip("선 구간당 중간 포인트 수 (부드러움)")]
        [SerializeField] private int pointsPerSegment = 8;

        [Header("Dots")]
        [SerializeField] private Color dotIdle = new Color(0.45f, 0.45f, 0.45f, 1f);
        [SerializeField] private Color dotActive = new Color(1f, 0.6f, 0.2f, 1f);
        [SerializeField] private float dotScale = 0.25f;

        [Header("Sorting")]
        [SerializeField] private int sortingOrder = 20;

        // 내부
        private SpriteRenderer[] dotRenderers;
        private LineRenderer bgLine;     // 배경 경로 (전체)
        private LineRenderer fillLine;   // 진행 경로 (부분)
        private Sprite circleSprite;
        private Camera cam;

        // 경로 포인트 캐시
        // 전체 경로: dot[1] → dot[0] → dot[1] → dot[2]
        // (위로 갔다 돌아오고 아래로 가는 전체 경로)
        // 배경은 1→0, 1→2 두 선만 표시
        private Vector3[] bgPoints;      // 배경 전체 포인트
        private int bgPointCount;

        private void Start()
        {
            cam = Camera.main;
            if (detector == null)
                detector = GetComponent<ShakeGestureDetector>();
            if (detector == null)
            {
                Debug.LogError("[ShakeView] ShakeGestureDetector not found!");
                return;
            }

            circleSprite = MakeCircleSprite(32);
            BuildBgPath();
            CreateBgLine();
            CreateFillLine();
            CreateDots();
        }

        private void LateUpdate()
        {
            if (detector == null) return;
            UpdateBgLine();
            UpdateFillLine();
            UpdateDots();
        }

        // ============================================================
        // 경로 생성
        // ============================================================

        /// <summary>
        /// 배경 경로: dot[0] ← dot[1] → dot[2] (V자 형태)
        /// 중간 포인트로 보간하여 부드럽게
        /// </summary>
        private void BuildBgPath()
        {
            // 구간 1: dot[1] → dot[0] (중앙→위)
            // 구간 2: dot[1] → dot[2] (중앙→아래)
            bgPointCount = pointsPerSegment * 2 + 1; // 양쪽 + 중앙
            bgPoints = new Vector3[bgPointCount];

            // 위쪽 구간 (역순: 0→1 순서로 저장하여 위에서 중앙까지)
            for (int i = 0; i <= pointsPerSegment; i++)
            {
                float t = (float)i / pointsPerSegment;
                bgPoints[i] = Vector3.Lerp(
                    detector.GetDotWorldPosition(0),
                    detector.GetDotWorldPosition(1), t);
            }

            // 아래쪽 구간 (중앙에서 아래까지, 중앙은 위 구간 마지막과 겹침)
            for (int i = 1; i <= pointsPerSegment; i++)
            {
                float t = (float)i / pointsPerSegment;
                bgPoints[pointsPerSegment + i] = Vector3.Lerp(
                    detector.GetDotWorldPosition(1),
                    detector.GetDotWorldPosition(2), t);
            }
        }

        private void CreateBgLine()
        {
            var obj = new GameObject("ShakeBg");
            obj.transform.parent = transform;
            bgLine = obj.AddComponent<LineRenderer>();
            bgLine.useWorldSpace = true;
            bgLine.sortingOrder = sortingOrder - 2;
            bgLine.material = new Material(Shader.Find("Sprites/Default"));
            bgLine.startColor = pathBgColor;
            bgLine.endColor = pathBgColor;
            bgLine.startWidth = pathBgWidth;
            bgLine.endWidth = pathBgWidth;
            bgLine.positionCount = bgPointCount;
        }

        private void CreateFillLine()
        {
            var obj = new GameObject("ShakeFill");
            obj.transform.parent = transform;
            fillLine = obj.AddComponent<LineRenderer>();
            fillLine.useWorldSpace = true;
            fillLine.sortingOrder = sortingOrder - 1;
            fillLine.material = new Material(Shader.Find("Sprites/Default"));
            fillLine.startColor = pathFillColor;
            fillLine.endColor = pathFillColor;
            fillLine.startWidth = pathFillWidth;
            fillLine.endWidth = pathFillWidth;
            fillLine.positionCount = 0;
        }

        private void CreateDots()
        {
            dotRenderers = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var obj = new GameObject($"ShakeDot_{i}");
                obj.transform.parent = transform;
                obj.transform.position = (Vector3)detector.GetDotWorldPosition(i);

                var sr = obj.AddComponent<SpriteRenderer>();
                sr.sprite = circleSprite;
                sr.color = dotIdle;
                sr.sortingOrder = sortingOrder;
                sr.transform.localScale = Vector3.one * dotScale;
                dotRenderers[i] = sr;
            }
        }

        // ============================================================
        // 업데이트
        // ============================================================

        private void UpdateBgLine()
        {
            // 부모 이동 대응: 매 프레임 배경 포인트 재계산
            for (int i = 0; i <= pointsPerSegment; i++)
            {
                float t = (float)i / pointsPerSegment;
                bgPoints[i] = Vector3.Lerp(
                    (Vector3)detector.GetDotWorldPosition(0),
                    (Vector3)detector.GetDotWorldPosition(1), t);
            }
            for (int i = 1; i <= pointsPerSegment; i++)
            {
                float t = (float)i / pointsPerSegment;
                bgPoints[pointsPerSegment + i] = Vector3.Lerp(
                    (Vector3)detector.GetDotWorldPosition(1),
                    (Vector3)detector.GetDotWorldPosition(2), t);
            }
            for (int i = 0; i < bgPointCount; i++)
                bgLine.SetPosition(i, bgPoints[i]);
        }

        /// <summary>
        /// ★ 핵심: 진행선을 현재 구간에서 마우스 위치까지 채움
        /// </summary>
        private void UpdateFillLine()
        {
            if (!detector.IsDragging || detector.LastReached < 0 || detector.CurrentTarget < 0)
            {
                fillLine.positionCount = 0;
                return;
            }

            int from = detector.LastReached;
            int to = detector.CurrentTarget;

            // 마우스 진행도
            Vector2 mouseWorld = cam != null
                ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition)
                : (Vector2)detector.GetDotWorldPosition(from);
            float progress = detector.GetProgress(mouseWorld);

            // from→to 구간의 포인트 수
            int fillCount = Mathf.Max(2, Mathf.RoundToInt(progress * pointsPerSegment) + 1);
            fillLine.positionCount = fillCount;

            Vector3 fromPos = (Vector3)detector.GetDotWorldPosition(from);
            Vector3 toPos = (Vector3)detector.GetDotWorldPosition(to);

            for (int i = 0; i < fillCount; i++)
            {
                float t = (float)i / (fillCount - 1) * progress;
                fillLine.SetPosition(i, Vector3.Lerp(fromPos, toPos, t));
            }
        }

        private void UpdateDots()
        {
            for (int i = 0; i < 3; i++)
            {
                if (dotRenderers[i] == null) continue;
                dotRenderers[i].transform.position = (Vector3)detector.GetDotWorldPosition(i);

                bool isTarget = (i == detector.CurrentTarget) && detector.IsDragging;
                bool isReached = (i == detector.LastReached);

                if (isReached)
                    dotRenderers[i].color = dotActive;
                else if (isTarget)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 5f);
                    dotRenderers[i].color = Color.Lerp(dotIdle, dotActive, pulse * 0.5f);
                }
                else
                    dotRenderers[i].color = dotIdle;
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
