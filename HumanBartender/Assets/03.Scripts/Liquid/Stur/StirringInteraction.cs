using UnityEngine;

namespace LiquidSimulation
{
    [RequireComponent(typeof(LiquidContainer))]
    public class StirringInteraction : MonoBehaviour
    {
        [SerializeField] private float stirStrength = 1.5f;
        [SerializeField] private float minDragSpeed = 0.5f;

        private LiquidContainer container;
        private Camera mainCamera;
        private bool isStirring = false;
        private Vector2 lastMousePos;

        private void Start()
        {
            container = GetComponent<LiquidContainer>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (mainCamera == null) return;

            Vector2 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                if (IsOverContainer(mouseWorld))
                {
                    isStirring = true;
                    lastMousePos = mouseWorld;
                }
            }

            if (Input.GetMouseButtonUp(0))
                isStirring = false;

            if (isStirring)
            {
                Vector2 delta = mouseWorld - lastMousePos;
                float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.001f);

                if (speed > minDragSpeed)
                {
                    float str = Mathf.Min(speed * stirStrength * 0.15f, 2f);
                    container.Stir(mouseWorld, str);
                }

                lastMousePos = mouseWorld;
            }
        }

        private bool IsOverContainer(Vector2 pos)
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) return col.OverlapPoint(pos);

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) return sr.bounds.Contains(pos);

            // 대략적
            Vector3 p = transform.position;
            return Mathf.Abs(pos.x - p.x) < 2f && Mathf.Abs(pos.y - p.y) < 2f;
        }

        /// <summary>
        /// ★ 외부에서 호출: 제스처 완료 시 액체를 amount만큼 섞음
        /// 
        /// amount 0.0 ~ 1.0:
        ///   0.05 = 살짝 (한 번 젓기)
        ///   0.2  = 보통
        ///   0.5  = 강하게
        ///   1.0  = 완전히 섞기
        /// 
        /// 동작: 막대기로 휘젓는 것처럼 수직 스왑 + 대각 교반
        /// 속도가 아닌 셀 자체를 물리적으로 이동시킴
        /// </summary>
        public void ApplyStir(float amount)
        {
            if (container == null) container = GetComponent<LiquidContainer>();
            if (container.Grid == null) return;

            var grid = container.Grid;
            int w = grid.Width;
            int h = grid.Height;
            float clamped = Mathf.Clamp01(amount);

            // ★ 난류 증가 → SimulationStep의 MixAdjacentCells가 빨라짐
            grid.AddTurbulence(Mathf.Lerp(0.3f, 0.8f, clamped));
            // ★ 정착(밀도 재정렬) 쿨다운 → 섞인 상태 유지
            grid.SuppressSettle(Mathf.Lerp(1.5f, 4f, clamped));

            // ★ 1단계: 전폭 수직 스쿱 (아래↔위 셀 교환)
            int swapsPerColumn = Mathf.Max(1, Mathf.RoundToInt(clamped * 3f));
            float mixBoost = Mathf.Lerp(0.1f, 0.35f, clamped);
            for (int x = 0; x < w; x++)
            {
                // 이 열에서 액체 범위 찾기
                int minY = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    if (!grid.Cells[x, y].IsEmpty && grid.ContainerMask[x, y])
                    {
                        if (minY < 0) minY = y;
                        maxY = y;
                    }
                }
                if (minY < 0 || maxY <= minY + 2) continue;

                int range = maxY - minY;
                for (int s = 0; s < swapsPerColumn; s++)
                {
                    int pickY = minY + Random.Range(0, range / 3 + 1);
                    int dropY = maxY - Random.Range(0, range / 3 + 1);
                    if (pickY >= dropY) continue;
                    if (grid.Cells[x, pickY].IsEmpty || grid.Cells[x, dropY].IsEmpty) continue;

                    var tmp = grid.Cells[x, pickY];
                    grid.Cells[x, pickY] = grid.Cells[x, dropY];
                    grid.Cells[x, dropY] = tmp;

                    // ★ 스왑된 셀의 mixRatio를 임계값(0.3) 이상으로
                    // → SettleRowBased에서 밀도 재정렬 제외
                    grid.Cells[x, pickY].mixRatio = Mathf.Max(grid.Cells[x, pickY].mixRatio, mixBoost);
                    grid.Cells[x, dropY].mixRatio = Mathf.Max(grid.Cells[x, dropY].mixRatio, mixBoost);
                }
            }

            // ★ 2단계: 대각선 스쿱
            int diagCount = Mathf.Max(w, Mathf.RoundToInt(clamped * w * 0.8f));
            for (int d = 0; d < diagCount; d++)
            {
                int sx = Random.Range(1, w - 1);
                int sy = Random.Range(1, h - 3);
                if (grid.Cells[sx, sy].IsEmpty || !grid.ContainerMask[sx, sy]) continue;

                int tx = Mathf.Clamp(sx + Random.Range(-1, 2), 0, w - 1);
                int ty = Mathf.Clamp(sy + Random.Range(1, 4), 0, h - 1);
                if (grid.Cells[tx, ty].IsEmpty || !grid.ContainerMask[tx, ty]) continue;
                if (grid.Cells[sx, sy].liquidType == grid.Cells[tx, ty].liquidType
                    && grid.Cells[sx, sy].liquidType != LiquidType.Mixed) continue;

                var tmp = grid.Cells[sx, sy];
                grid.Cells[sx, sy] = grid.Cells[tx, ty];
                grid.Cells[tx, ty] = tmp;

                grid.Cells[sx, sy].mixRatio = Mathf.Max(grid.Cells[sx, sy].mixRatio, mixBoost);
                grid.Cells[tx, ty].mixRatio = Mathf.Max(grid.Cells[tx, ty].mixRatio, mixBoost);
            }

            // ★ 3단계: 약간의 회전 속도
            float velStr = Mathf.Lerp(0.2f, 1.0f, clamped);
            Vector2 center = new Vector2(w * 0.5f, h * 0.5f);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    ref LiquidCell c = ref grid.Cells[x, y];
                    if (c.IsEmpty) continue;
                    float dx = x - center.x, dy = y - center.y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist < 1f) continue;
                    float f = 1f - dist / (Mathf.Min(w, h) * 0.5f);
                    if (f <= 0f) continue;
                    Vector2 tang = new Vector2(-dy, dx).normalized;
                    c.velocityX += tang.x * velStr * f;
                    c.velocityY += tang.y * velStr * f * 0.3f;
                    c.velocityX = Mathf.Clamp(c.velocityX, -1.5f, 1.5f);
                    c.velocityY = Mathf.Clamp(c.velocityY, -1f, 1f);
                }

            // ★ 4단계: 강한 색 혼합 (핵심)
            // 스왑으로 생긴 노이즈를 즉시 그라데이션으로 녹임
            int passes = Mathf.Max(8, Mathf.RoundToInt(clamped * 40f));
            float mixPerPass = Mathf.Lerp(0.02f, 0.05f, clamped);
            for (int i = 0; i < passes; i++)
                grid.MixAdjacentCells(mixPerPass);



        }

        /// <summary>
        /// 현재 용기의 액체가 완전히 섞였을 때의 색상을 반환.
        /// UI에서 목표색 표시, 완성도 판정 등에 사용.
        /// </summary>
        public Color32 GetFullyMixedColor()
        {
            if (container == null) container = GetComponent<LiquidContainer>();
            if (container.Grid == null) return new Color32(0, 0, 0, 0);
            return container.Grid.GetFullyMixedColor();
        }
    }
}
