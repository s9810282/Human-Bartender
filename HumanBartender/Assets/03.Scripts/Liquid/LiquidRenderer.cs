using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 그리드 → Texture2D 렌더링 (v10)
    /// 
    /// ★ 에디터 배치용: 인스펙터에서 크기를 설정하면 Start()에서 자동 초기화
    ///   또는 코드에서 Initialize()를 직접 호출해도 됨
    /// 
    /// 사용법:
    /// 1. 빈 오브젝트에 이 컴포넌트 추가 (SpriteRenderer 자동 추가됨)
    /// 2. 인스펙터에서 Grid Width, Grid Height, Pixels Per Unit 설정
    /// 3. 플레이 → 자동 초기화
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class LiquidRenderer : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int gridWidth = 32;
        [SerializeField] private int gridHeight = 48;
        [SerializeField] private int pixelsPerUnit = 16;

        [Header("Visual")]
        [SerializeField] private Color32 backgroundColor = new Color32(0, 0, 0, 0);
        [SerializeField] private bool surfaceHighlight = true;
        [SerializeField] private bool edgeHighlight = true;

        private int ppu;
        private Texture2D tex;
        private Color32[] buf;
        private SpriteRenderer sr;
        private bool[,] maskCache;
        private bool ready = false;

        public LiquidGrid Grid { get; private set; }
        public bool IsReady => ready;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int PPU => ppu;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            // ★ 아직 초기화 안 됐으면 인스펙터 설정값으로 자동 초기화
            if (!ready)
                Initialize(gridWidth, gridHeight, pixelsPerUnit);
        }

        /// <summary>
        /// 초기화 (코드에서 직접 호출도 가능)
        /// Start()보다 먼저 호출하면 Start()에서 중복 호출 안 됨
        /// </summary>
        public void Initialize(int width, int height, int pxPerUnit = 16)
        {
            if (ready) return; // 중복 방지

            gridWidth = width;
            gridHeight = height;
            ppu = pxPerUnit;

            tex = new Texture2D(gridWidth, gridHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            buf = new Color32[gridWidth * gridHeight];

            if (sr == null) sr = GetComponent<SpriteRenderer>();
            Rect rect = new Rect(0, 0, gridWidth, gridHeight);
            sr.sprite = Sprite.Create(tex, rect, new Vector2(0.5f, 0f), ppu);

            Grid = new LiquidGrid(gridWidth, gridHeight);
            ready = true;
        }

        /// <summary>
        /// 마스크 변경 후 캐시 갱신
        /// </summary>
        public void RefreshMaskCache()
        {
            if (Grid == null) return;
            maskCache = new bool[gridWidth, gridHeight];
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    maskCache[x, y] = Grid.ContainerMask[x, y];
        }

        private void LateUpdate()
        {
            if (!ready || Grid == null) return;
            Render();
        }

        private void Render()
        {
            var cells = Grid.Cells;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    int idx = y * gridWidth + x;

                    if (maskCache != null && !maskCache[x, y])
                    { buf[idx] = backgroundColor; continue; }

                    if (cells[x, y].IsEmpty)
                    { buf[idx] = backgroundColor; continue; }

                    Color32 c = cells[x, y].color;

                    if (surfaceHighlight && (y >= gridHeight - 1 || cells[x, y + 1].IsEmpty))
                        c = Brighten(c, 25);

                    if (edgeHighlight && IsEdge(x, y))
                        c = Brighten(c, 12);

                    buf[idx] = c;
                }
            }

            // ★ 얼음 렌더링
            if (Grid.IcePieces != null)
            {
                foreach (var ice in Grid.IcePieces)
                {
                    if (ice.IsMelted) continue;
                    int iceCX = Mathf.RoundToInt(ice.position.x);
                    int iceCY = Mathf.RoundToInt(ice.position.y);
                    int hw = ice.shapeW / 2 + 2;
                    int hh = ice.shapeH / 2 + 2;

                    for (int x = iceCX - hw; x <= iceCX + hw; x++)
                    {
                        for (int y = iceCY - hh; y <= iceCY + hh; y++)
                        {
                            if (x < 0 || x >= gridWidth || y < 0 || y >= gridHeight) continue;
                            Color32 iceColor;
                            if (ice.GetPixel(x, y, out iceColor))
                            {
                                int idx = y * gridWidth + x;
                                Color32 bg = buf[idx];
                                float a = iceColor.a / 255f;
                                buf[idx] = new Color32(
                                    (byte)(iceColor.r * a + bg.r * (1f - a)),
                                    (byte)(iceColor.g * a + bg.g * (1f - a)),
                                    (byte)(iceColor.b * a + bg.b * (1f - a)),
                                    (byte)Mathf.Min(255, bg.a + iceColor.a)
                                );
                            }
                        }
                    }
                }
            }

            tex.SetPixels32(buf);
            tex.Apply();
        }

        private bool IsEdge(int x, int y)
        {
            if (maskCache == null) return false;
            if (x <= 0 || !maskCache[x - 1, y]) return true;
            if (x >= gridWidth - 1 || !maskCache[x + 1, y]) return true;
            if (y <= 0 || !maskCache[x, y - 1]) return true;
            if (y >= gridHeight - 1 || !maskCache[x, y + 1]) return true;
            return false;
        }

        private Color32 Brighten(Color32 c, byte amt)
        {
            return new Color32(
                (byte)Mathf.Min(255, c.r + amt),
                (byte)Mathf.Min(255, c.g + amt),
                (byte)Mathf.Min(255, c.b + amt), c.a);
        }

        public Vector2Int WorldToGrid(Vector2 wp)
        {
            int gx = Mathf.RoundToInt((wp.x - transform.position.x) * ppu + gridWidth * 0.5f);
            int gy = Mathf.RoundToInt((wp.y - transform.position.y) * ppu);
            return new Vector2Int(
                Mathf.Clamp(gx, 0, gridWidth - 1),
                Mathf.Clamp(gy, 0, gridHeight - 1));
        }

        public Vector3 GridToWorld(int gx, int gy)
        {
            float lx = (gx - gridWidth * 0.5f) / (float)ppu;
            float ly = (float)gy / (float)ppu;
            return transform.TransformPoint(lx, ly, 0f);
        }

        private void OnDestroy()
        {
            if (tex != null) Destroy(tex);
        }
    }
}
