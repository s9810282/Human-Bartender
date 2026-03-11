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

        /// <summary>
        /// ★ 외부에서 호출: 제스처 완료 시 쉐이커를 amount만큼 흔듦
        /// 
        /// amount 0.0 ~ 1.0:
        ///   0.05 = 살짝 (한 번 흔들기)
        ///   0.2  = 보통
        ///   0.5  = 강하게
        ///   1.0  = 완전히 섞기
        /// 
        /// Stir와 차이:
        /// - Stir: 수직 스쿱 + 대각 → 레이어 경계 교란
        /// - Shake: 전방향 랜덤 스왑 + 강한 속도 + 평균색 수렴
        ///   → 더 격렬하고 빠르게 단일색으로 수렴
        /// </summary>
        public void ApplyShake(float amount)
        {
            if (container == null) container = GetComponent<LiquidContainer>();
            if (container.Grid == null) return;

            var grid = container.Grid;
            int w = grid.Width;
            int h = grid.Height;
            float clamped = Mathf.Clamp01(amount);

            // ★ 난류 + 정착 쿨다운
            grid.AddTurbulence(Mathf.Lerp(0.8f, 1f, clamped));
            grid.SuppressSettle(Mathf.Lerp(3f, 6f, clamped));

            // ★ 1단계: 대규모 스플래시 — 액체를 컨테이너 전체로 흩뿌림
            // 액체 셀을 빈 공간으로 대량 이동 → 격렬한 비산 효과
            //
            // 1a: 먼저 모든 액체 셀과 빈 칸 목록 수집
            var liquidCells = new System.Collections.Generic.List<Vector2Int>();
            var emptyCells = new System.Collections.Generic.List<Vector2Int>();

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (!grid.ContainerMask[x, y]) continue;
                    if (grid.Cells[x, y].IsEmpty)
                        emptyCells.Add(new Vector2Int(x, y));
                    else
                        liquidCells.Add(new Vector2Int(x, y));
                }

            // 1b: 액체 셀의 일정 비율을 빈 칸으로 이동 (흩뿌림)
            // amount=0.1이면 40%, amount=1.0이면 80%의 액체가 흩어짐
            float scatterRatio = Mathf.Lerp(0.4f, 0.8f, clamped);
            int scatterCount = Mathf.Min(
                Mathf.RoundToInt(liquidCells.Count * scatterRatio),
                emptyCells.Count);

            // 빈 칸 목록 셔플 (위쪽 빈 칸 우선 X → 랜덤 분산)
            for (int i = emptyCells.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = emptyCells[i];
                emptyCells[i] = emptyCells[j];
                emptyCells[j] = tmp;
            }

            float mixBoost = Mathf.Lerp(0.15f, 0.4f, clamped);
            int scattered = 0;
            for (int i = 0; i < scatterCount && scattered < emptyCells.Count; i++)
            {
                // 랜덤 액체 셀 선택
                int srcIdx = Random.Range(0, liquidCells.Count);
                var src = liquidCells[srcIdx];
                if (grid.Cells[src.x, src.y].IsEmpty) continue;

                // 빈 칸으로 이동
                var dst = emptyCells[scattered];
                grid.Cells[dst.x, dst.y] = grid.Cells[src.x, src.y];
                grid.Cells[src.x, src.y] = LiquidCell.Empty;

                // 속도 부여 (전방향, 격렬하게)
                grid.Cells[dst.x, dst.y].velocityX = Random.Range(-3f, 3f) * clamped;
                grid.Cells[dst.x, dst.y].velocityY = Random.Range(-2f, 4f) * clamped;
                grid.Cells[dst.x, dst.y].mixRatio = Mathf.Max(
                    grid.Cells[dst.x, dst.y].mixRatio, mixBoost);

                scattered++;
            }

            // ★ 2단계: 전방향 랜덤 스왑 (격렬한 흔들기)
            // Stir의 수직 스쿱과 달리 상하좌우 전방향으로 셀 교환
            int swapCount = Mathf.Max(10, Mathf.RoundToInt(clamped * w * h * 0.08f));

            for (int i = 0; i < swapCount; i++)
            {
                int x1 = Random.Range(0, w);
                int y1 = Random.Range(0, h);
                if (!grid.ContainerMask[x1, y1] || grid.Cells[x1, y1].IsEmpty) continue;

                // 넓은 범위에서 상대 셀 선택 (Stir보다 넓음)
                int x2 = Mathf.Clamp(x1 + Random.Range(-3, 4), 0, w - 1);
                int y2 = Mathf.Clamp(y1 + Random.Range(-4, 5), 0, h - 1);
                if (!grid.ContainerMask[x2, y2] || grid.Cells[x2, y2].IsEmpty) continue;

                // 스왑
                var tmp = grid.Cells[x1, y1];
                grid.Cells[x1, y1] = grid.Cells[x2, y2];
                grid.Cells[x2, y2] = tmp;

                // mixRatio 올림 → 재정렬 방지
                grid.Cells[x1, y1].mixRatio = Mathf.Max(grid.Cells[x1, y1].mixRatio, mixBoost);
                grid.Cells[x2, y2].mixRatio = Mathf.Max(grid.Cells[x2, y2].mixRatio, mixBoost);
            }

            // ★ 3단계: 강한 랜덤 속도 (격렬한 출렁임)
            float velStr = Mathf.Lerp(1.5f, 4f, clamped);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    ref LiquidCell c = ref grid.Cells[x, y];
                    if (c.IsEmpty) continue;
                    c.velocityX += Random.Range(-velStr, velStr);
                    c.velocityY += Random.Range(-velStr, velStr);
                    c.velocityX = Mathf.Clamp(c.velocityX, -5f, 5f);
                    c.velocityY = Mathf.Clamp(c.velocityY, -5f, 5f);
                }

            // ★ 4단계: 강한 색 혼합 + 평균색 수렴
            // Stir보다 공격적 → 쉐이킹은 빠르게 단일색으로 수렴해야 함
            int passes = Mathf.Max(10, Mathf.RoundToInt(clamped * 50f));
            float mixPerPass = Mathf.Lerp(0.025f, 0.06f, clamped);
            for (int i = 0; i < passes; i++)
                grid.MixAdjacentCells(mixPerPass);

            // ★ 5단계: 평균색 수렴 (쉐이킹 특유)
            // Stir에는 없는 단계. 전체 셀을 하나의 혼합색으로 끌어당김
            float convergence = Mathf.Lerp(0.02f, 0.08f, clamped);
            ConvergeColors(grid, w, h, convergence);
        }

        /// <summary>
        /// 전체 액체의 평균색을 계산하고 모든 셀을 그 방향으로 보간.
        /// 쉐이킹은 잔 전체가 하나의 색으로 수렴하므로 Stir와 달리 이 단계가 필요.
        /// </summary>
        private void ConvergeColors(LiquidGrid grid, int w, int h, float rate)
        {
            // 평균색 계산
            long totalR = 0, totalG = 0, totalB = 0;
            int count = 0;
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    if (grid.Cells[x, y].IsEmpty) continue;
                    totalR += grid.Cells[x, y].color.r;
                    totalG += grid.Cells[x, y].color.g;
                    totalB += grid.Cells[x, y].color.b;
                    count++;
                }

            if (count == 0) return;

            Color32 avg = new Color32(
                (byte)(totalR / count),
                (byte)(totalG / count),
                (byte)(totalB / count),
                255);

            // 모든 셀을 평균색 방향으로 보간
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    ref LiquidCell c = ref grid.Cells[x, y];
                    if (c.IsEmpty) continue;
                    c.color = Color32.Lerp(c.color, avg, rate);

                    // mixRatio도 올림
                    c.mixRatio = Mathf.Min(1f, c.mixRatio + rate);
                    if (c.mixRatio >= 0.95f)
                    {
                        c.liquidType = LiquidType.Mixed;
                        c.mixRatio = 1f;
                    }
                }
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
