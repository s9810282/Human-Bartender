using UnityEngine;
using System.Collections.Generic;

namespace LiquidSimulation
{
    /// <summary>
    /// 핵심 액체 시뮬레이션 그리드 (v7 - 부드러운 전환)
    /// 
    /// ★ 물리 모드와 정착 모드 사이의 뚝 끊김 해결:
    /// 
    /// [항상 물리 실행] 중력 + 속도 이동은 항상 동작
    /// [turbulence에 따라 물리 강도 조절]
    ///   - 높을 때: 약한 중력, 약한 감쇠 → 액체가 자유롭게 출렁임
    ///   - 낮을 때: 강한 중력, 강한 감쇠 → 빠르게 안정됨
    /// [행 기반 정착은 완전 안정 후에만]
    ///   - turbulence ≈ 0 이고 모든 속도 ≈ 0일 때만 실행
    ///   → 자연스럽게 가라앉은 후 마지막 정리용
    /// </summary>
    public class LiquidGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public LiquidCell[,] Cells { get; private set; }
        public bool[,] ContainerMask { get; private set; }

        private List<int>[] rowValidXs;
        private int totalMaskCells = 0;

        private float turbulence = 0f;

        /// <summary>
        /// 정착(밀도 재정렬) 쿨다운. 0 초과이면 SettleRowBased 비활성.
        /// ApplyStir 등에서 설정하여 stir 직후 재정렬 방지.
        /// </summary>
        private float settleCooldown = 0f;
        private bool sweepLTR = true;
        private List<LiquidCell> allLiquid = new List<LiquidCell>(2048);

        // 물리 파라미터 (turbulence에 따라 보간)
        private const float GRAVITY_CALM = 3.0f;    // 평온할 때 강한 중력 (빠르게 안정)
        private const float GRAVITY_TURB = 0.8f;    // 난류 시 약한 중력 (출렁거림 유지)
        private const float DAMPING_CALM = 0.75f;   // 평온할 때 강한 감쇠
        private const float DAMPING_TURB = 0.92f;   // 난류 시 약한 감쇠

        public float Turbulence => turbulence;

        /// <summary>
        /// 외부에서 난류도 추가 (휘젓기 등)
        /// </summary>
        public void AddTurbulence(float amount)
        {
            turbulence = Mathf.Clamp01(turbulence + amount);
        }

        /// <summary>
        /// 밀도 재정렬을 일정 시간 비활성화.
        /// 휘젓기 후 섞인 상태가 유지되도록 함.
        /// </summary>
        public void SuppressSettle(float seconds)
        {
            settleCooldown = Mathf.Max(settleCooldown, seconds);
        }

        // ★ 기울기 (따르기 시스템에서 설정)
        // 양수 = 오른쪽으로 기울임 (액체가 오른쪽으로 쏠림)
        // 음수 = 왼쪽으로 기울임 (액체가 왼쪽으로 쏠림)
        public float TiltAngle { get; set; } = 0f;

        // ★ 얼음 조각 관리
        public List<IcePiece> IcePieces { get; private set; } = new List<IcePiece>();

        public LiquidGrid(int width, int height)
        {
            Width = width;
            Height = height;
            Cells = new LiquidCell[width, height];
            ContainerMask = new bool[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                {
                    Cells[x, y] = LiquidCell.Empty;
                    ContainerMask[x, y] = true;
                }
            BuildRowCache();
        }

        public void SetMaskFromTexture(Texture2D maskTexture)
        {
            Color32[] pixels = maskTexture.GetPixels32();
            for (int x = 0; x < Width && x < maskTexture.width; x++)
                for (int y = 0; y < Height && y < maskTexture.height; y++)
                    ContainerMask[x, y] = pixels[y * maskTexture.width + x].a > 128;
            BuildRowCache();
        }

        public void BuildRowCache()
        {
            rowValidXs = new List<int>[Height];
            totalMaskCells = 0;
            for (int y = 0; y < Height; y++)
            {
                rowValidXs[y] = new List<int>();
                for (int x = 0; x < Width; x++)
                    if (ContainerMask[x, y])
                    {
                        rowValidXs[y].Add(x);
                        totalMaskCells++;
                    }
            }
        }

        // ============================================================
        // 시뮬레이션 메인 루프
        // ============================================================

        public void SimulationStep()
        {
            // turbulence 점진적 감소
            turbulence = Mathf.Max(0f, turbulence - 0.01f);
            // 정착 쿨다운 감소 (틱 기반, ~60틱/초 가정)
            settleCooldown = Mathf.Max(0f, settleCooldown - 0.016f);

            // ★ turbulence에 따라 물리 파라미터를 보간
            float t = Mathf.Clamp01(turbulence);
            float currentGravity = Mathf.Lerp(GRAVITY_CALM, GRAVITY_TURB, t);
            float currentDamping = Mathf.Lerp(DAMPING_CALM, DAMPING_TURB, t);

            // ★ 항상 물리 실행 (강도만 다름)
            ApplyGravityToVelocity(currentGravity);
            ApplyVelocity();
            ApplyDamping(currentDamping);

            // 중력 낙하 (빈 공간 채움) - 패스 수도 turbulence에 따라
            int fallPasses = Mathf.RoundToInt(Mathf.Lerp(6f, 2f, t));
            for (int pass = 0; pass < fallPasses; pass++)
                GravityFall();

            // 수평 확산 (수면 평탄화)
            int spreadPasses = Mathf.RoundToInt(Mathf.Lerp(4f, 1f, t));
            for (int pass = 0; pass < spreadPasses; pass++)
                HorizontalSpread();

            // 인접 혼합: 난류 시 더 빠르게 섞임
            float mixRate = Mathf.Lerp(0.003f, 0.015f, Mathf.Clamp01(turbulence));
            MixAdjacentCells(mixRate);

            // ★ 마스크 밖에 떠다니는 셀 제거
            CleanupStrayPixels();

            // ★ 정착: 기울어져 있으면 기울기 기반, 아니면 행 기반
            if (Mathf.Abs(TiltAngle) > 1f)
            {
                SettleTilted();
            }
            else if (settleCooldown <= 0f && turbulence < 0.05f && GetMaxVelocity() < 1.5f)
            {
                SettleRowBased();
            }

            sweepLTR = !sweepLTR;

            // ★ 얼음 물리 시뮬레이션
            UpdateIce();
        }

        // ============================================================
        // ★ 얼음 시스템
        // ============================================================

        /// <summary>
        /// 얼음 추가
        /// </summary>
        public void AddIce(IceShape shape, float x = -1, float y = -1)
        {
            if (x < 0)
            {
                // ★ 기존 얼음과 겹치지 않는 X 위치 찾기
                x = Width * 0.5f;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    bool overlaps = false;
                    foreach (var other in IcePieces)
                    {
                        if (Mathf.Abs(other.position.x - x) < 5f)
                        { overlaps = true; break; }
                    }
                    if (!overlaps) break;
                    // 좌우로 분산
                    x = Random.Range(Width * 0.15f, Width * 0.85f);
                }
            }

            if (y < 0)
            {
                int midX = Mathf.Clamp(Mathf.RoundToInt(x), 0, Width - 1);

                int surfY = -1;
                for (int sy = Height - 1; sy >= 0; sy--)
                {
                    if (midX >= 0 && midX < Width && !Cells[midX, sy].IsEmpty)
                    { surfY = sy; break; }
                }

                if (surfY < 0)
                {
                    // 액체 없음 → 바닥
                    for (int sy = 0; sy < Height; sy++)
                    {
                        if (ContainerMask[midX, sy])
                        { y = sy + 3; break; }
                    }
                    if (y < 0) y = 3;
                }
                else
                {
                    y = Mathf.Min(surfY + 3, Height - 5);
                }

                // ★ 기존 얼음과 Y도 겹치면 위로 올림
                foreach (var other in IcePieces)
                {
                    if (Mathf.Abs(other.position.x - x) < 6f
                        && Mathf.Abs(other.position.y - y) < 6f)
                    {
                        y = other.position.y + other.shapeH + 1;
                    }
                }
                y = Mathf.Min(y, Height - 5);
            }

            var ice = IcePiece.Create(shape, new Vector2(x, y));
            IcePieces.Add(ice);
        }

        /// <summary>
        /// 모든 얼음 제거
        /// </summary>
        public void ClearIce()
        {
            IcePieces.Clear();
        }

        /// <summary>
        /// ★ 특정 셀이 얼음에 의해 차지되고 있는지 확인
        /// </summary>
        public bool IsOccupiedByIce(int gx, int gy)
        {
            Color32 dummy;
            for (int i = 0; i < IcePieces.Count; i++)
            {
                if (IcePieces[i].IsMelted) continue;
                if (IcePieces[i].GetPixel(gx, gy, out dummy))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 얼음 물리 업데이트
        /// </summary>
        private void UpdateIce()
        {
            for (int i = IcePieces.Count - 1; i >= 0; i--)
            {
                var ice = IcePieces[i];
                ice.PhysicsStep(this);
                DisplaceLiquidAroundIce(ice);

                // ★ 점진적 녹기: 매 틱마다 녹은 양에 비례해 물 셀 생성
                if (ice.meltRate > 0f && !ice.IsMelted)
                {
                    int waterCells = ice.GetWaterCellsThisTick();
                    if (waterCells > 0)
                        SpawnMeltWater(ice, waterCells);
                }

                // 완전히 녹으면 제거
                if (ice.IsMelted)
                    IcePieces.RemoveAt(i);
            }

            // 얼음끼리 충돌 처리
            ResolveIceCollisions();
        }

        /// <summary>
        /// ★ 얼음 주변에 물 셀을 생성 (점진적 녹기)
        /// 
        /// 생성되는 물의 특성:
        /// - LiquidType.Water
        /// - 투명한 연한 파란색 (거의 무색)
        /// - density = 1.0 (표준)
        /// - mixRatio = 0.5 (쉽게 혼합됨)
        /// → MixAdjacentCells에 의해 주변 칵테일과 자연 혼합 → 희석
        /// </summary>
        private void SpawnMeltWater(IcePiece ice, int count)
        {
            int cx = Mathf.RoundToInt(ice.position.x);
            int cy = Mathf.RoundToInt(ice.position.y);

            // 얼음 바로 아래/주변에 물 생성
            int placed = 0;
            for (int attempt = 0; attempt < count * 6 && placed < count; attempt++)
            {
                // 얼음 주변 랜덤 위치 (아래쪽 선호)
                int px = cx + Random.Range(-ice.shapeW / 2 - 1, ice.shapeW / 2 + 2);
                int py = cy + Random.Range(-ice.shapeH / 2 - 2, ice.shapeH / 2 + 1);

                if (px < 0 || px >= Width || py < 0 || py >= Height) continue;
                if (!ContainerMask[px, py]) continue;
                if (!Cells[px, py].IsEmpty) continue;
                if (IsOccupiedByIce(px, py)) continue;

                Cells[px, py] = new LiquidCell
                {
                    liquidType = LiquidType.Water,
                    color = new Color32(210, 230, 245, 100),  // 거의 투명한 물
                    density = 1.0f,
                    mixRatio = 0.5f,   // 높은 mixRatio → 주변과 빠르게 혼합
                    velocityX = 0f,
                    velocityY = -0.3f  // 약간 아래로
                };
                placed++;
            }
        }

        /// <summary>
        /// ★ 얼음끼리 겹치면 밀어내기 + 속도 교환
        /// </summary>
        private void ResolveIceCollisions()
        {
            for (int i = 0; i < IcePieces.Count; i++)
            {
                for (int j = i + 1; j < IcePieces.Count; j++)
                {
                    var a = IcePieces[i];
                    var b = IcePieces[j];

                    // 바운딩 박스 겹침 확인 (빠른 사전 검사)
                    float aHW = a.shapeW * 0.5f;
                    float aHH = a.shapeH * 0.5f;
                    float bHW = b.shapeW * 0.5f;
                    float bHH = b.shapeH * 0.5f;

                    float dx = b.position.x - a.position.x;
                    float dy = b.position.y - a.position.y;
                    float overlapX = (aHW + bHW) - Mathf.Abs(dx);
                    float overlapY = (aHH + bHH) - Mathf.Abs(dy);

                    if (overlapX <= 0 || overlapY <= 0) continue;

                    // 겹침 있음 → 밀어내기
                    float pushX, pushY;
                    if (overlapX < overlapY)
                    {
                        // X축으로 분리 (더 적게 겹친 축)
                        pushX = (dx > 0 ? overlapX : -overlapX) * 0.5f;
                        pushY = 0f;
                    }
                    else
                    {
                        // Y축으로 분리
                        pushX = 0f;
                        pushY = (dy > 0 ? overlapY : -overlapY) * 0.5f;
                    }

                    // 위치 분리
                    a.position.x -= pushX;
                    a.position.y -= pushY;
                    b.position.x += pushX;
                    b.position.y += pushY;

                    // 속도 교환 (탄성 충돌)
                    float tempVx = a.velocity.x;
                    float tempVy = a.velocity.y;
                    a.velocity.x = b.velocity.x * 0.5f;
                    a.velocity.y = b.velocity.y * 0.5f;
                    b.velocity.x = tempVx * 0.5f;
                    b.velocity.y = tempVy * 0.5f;

                    // 약간의 반발 (겹침 방향으로 밀기)
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > 0.01f)
                    {
                        float nx = dx / dist;
                        float ny = dy / dist;
                        float repel = 0.3f;
                        a.velocity.x -= nx * repel;
                        a.velocity.y -= ny * repel;
                        b.velocity.x += nx * repel;
                        b.velocity.y += ny * repel;
                    }
                }
            }
        }

        /// <summary>
        /// 얼음 주변의 액체를 밀어냄 (얼음이 물리적 공간 차지)
        /// </summary>
        private void DisplaceLiquidAroundIce(IcePiece ice)
        {
            int cx = Mathf.RoundToInt(ice.position.x);
            int cy = Mathf.RoundToInt(ice.position.y);
            int hw = ice.shapeW / 2 + 1;
            int hh = ice.shapeH / 2 + 1;

            for (int x = cx - hw; x <= cx + hw; x++)
                for (int y = cy - hh; y <= cy + hh; y++)
                {
                    if (x < 0 || x >= Width || y < 0 || y >= Height) continue;
                    if (Cells[x, y].IsEmpty) continue;

                    Color32 col;
                    if (ice.GetPixel(x, y, out col))
                    {
                        // 이 위치에 얼음이 있음 → 액체를 위로 밀어냄
                        // 위쪽 빈 셀 찾기
                        for (int pushY = y + 1; pushY < Height; pushY++)
                        {
                            if (pushY < 0 || pushY >= Height) break;
                            if (!ContainerMask[x, pushY]) break;
                            if (Cells[x, pushY].IsEmpty)
                            {
                                Cells[x, pushY] = Cells[x, y];
                                Cells[x, y] = LiquidCell.Empty;
                                break;
                            }
                        }
                    }
                }
        }

        /// <summary>
        /// 현재 그리드 내 최대 속도 (안정 판단용)
        /// </summary>
        private float GetMaxVelocity()
        {
            float maxV = 0f;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    float v = Cells[x, y].velocityX * Cells[x, y].velocityX
                            + Cells[x, y].velocityY * Cells[x, y].velocityY;
                    if (v > maxV) maxV = v;
                }
            return Mathf.Sqrt(maxV);
        }

        // ============================================================
        // ★ 기울기 정착 시스템
        // ============================================================

        // 기울기 정착용 사전 정렬 캐시
        private int[] tiltSortedPositions;  // 유효 마스크 위치를 유효 높이 순으로 정렬
        private float lastSortedTiltAngle = float.MaxValue;

        /// <summary>
        /// ★ 기울기 기반 정착
        /// 
        /// 핵심 아이디어: 용기가 θ만큼 기울어지면, "아래"의 방향이 바뀜.
        /// 그리드 좌표 (x, y)의 "유효 높이" = x·sin(θ) + y·cos(θ)
        /// → 유효 높이가 낮은 위치부터 무거운 액체를 채움
        /// → 기울어진 쪽에 액체가 자연스럽게 쏠림
        /// 
        /// TiltAngle > 0 (CCW): 왼쪽이 낮음 → 왼쪽에 액체 쏠림
        /// TiltAngle < 0 (CW):  오른쪽이 낮음 → 오른쪽에 액체 쏠림
        /// </summary>
        private void SettleTilted()
        {
            // 기울기 각도가 크게 바뀌면 정렬 갱신 (2도 이상 차이)
            if (tiltSortedPositions == null
                || Mathf.Abs(TiltAngle - lastSortedTiltAngle) > 2f)
            {
                RebuildTiltPositions();
                lastSortedTiltAngle = TiltAngle;
            }

            // 1) 모든 액체 수집 + 그리드 비우기
            allLiquid.Clear();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (!Cells[x, y].IsEmpty)
                    {
                        allLiquid.Add(Cells[x, y]);
                        Cells[x, y] = LiquidCell.Empty;
                    }
                }
            if (allLiquid.Count == 0) return;

            // 2) 밀도순 정렬 (무거운 것이 앞)
            SortByDensity(allLiquid);

            // 3) 유효 높이가 낮은 위치부터 채움
            int idx = 0;
            for (int i = 0; i < tiltSortedPositions.Length && idx < allLiquid.Count; i++)
            {
                int pos = tiltSortedPositions[i];
                int px = pos % Width;
                int py = pos / Width;

                // 얼음이 차지한 위치 건너뜀
                if (IsOccupiedByIce(px, py)) continue;

                LiquidCell cell = allLiquid[idx];
                cell.velocityX = 0f;
                cell.velocityY = 0f;
                Cells[px, py] = cell;
                idx++;
            }
        }

        /// <summary>
        /// 현재 TiltAngle에 맞게 마스크 위치를 유효 높이순으로 정렬
        /// </summary>
        private void RebuildTiltPositions()
        {
            float rad = TiltAngle * Mathf.Deg2Rad;
            float sinA = Mathf.Sin(rad);
            float cosA = Mathf.Cos(rad);

            // 유효 마스크 셀 수
            int count = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (ContainerMask[x, y]) count++;

            tiltSortedPositions = new int[count];
            float[] heights = new float[count];

            int idx = 0;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (!ContainerMask[x, y]) continue;
                    tiltSortedPositions[idx] = x + y * Width;
                    // ★ 유효 높이: x·sin(θ) + y·cos(θ)
                    // 이 값이 작을수록 기울어진 방향에서 "아래"
                    heights[idx] = x * sinA + y * cosA;
                    idx++;
                }

            // 유효 높이 오름차순 정렬 (낮은 곳부터)
            System.Array.Sort(heights, tiltSortedPositions);
        }

        /// <summary>
        /// ★ 기울인 방향의 입구(rim)에서 액체 셀을 제거
        /// 
        /// 핵심: 실제로 액체가 입구 높이(상위 15%)까지 차 올랐을 때만 제거
        /// 기울기가 부족해서 액체가 입구에 안 닿으면 Empty 반환
        /// </summary>
        public LiquidCell RemoveLiquidFromRim(bool leftSide, out int rimX, out int rimY)
        {
            rimX = leftSide ? 0 : Width - 1;
            rimY = Height - 1;

            // ★ 입구 영역 = 상위 15% 행만 검색 (컵 높이의 85% 이상)
            int rimMinY = Mathf.RoundToInt(Height * 0.85f);
            int edgeRange = 4; // 가장자리에서 안쪽으로 4셀까지

            // 위에서부터 아래로 스캔
            for (int y = Height - 1; y >= rimMinY; y--)
            {
                var xs = rowValidXs[y];
                if (xs.Count == 0) continue;

                // 입구 쪽 가장자리 X
                int edgeX = leftSide ? xs[0] : xs[xs.Count - 1];

                // 가장자리에서 안쪽으로 검색
                for (int d = 0; d < edgeRange && d < xs.Count; d++)
                {
                    int cx = leftSide ? edgeX + d : edgeX - d;
                    if (cx < 0 || cx >= Width) continue;
                    if (Cells[cx, y].IsEmpty) continue;

                    rimX = edgeX;
                    rimY = y;
                    LiquidCell cell = Cells[cx, y];
                    Cells[cx, y] = LiquidCell.Empty;
                    return cell;
                }
            }
            return LiquidCell.Empty;
        }

        /// <summary>
        /// ★ 특정 위치 근처의 빈 셀에 액체 셀 배치 (따르기 도착점)
        /// </summary>
        public bool AddCellAtPosition(int nearX, LiquidCell cell)
        {
            // nearX 근처에서 위→아래로 빈 셀 찾기
            for (int r = 0; r < Width / 2; r++)
            {
                for (int dir = 0; dir <= 1; dir++)
                {
                    int x = (dir == 0) ? nearX + r : nearX - r;
                    if (x < 0 || x >= Width) continue;

                    for (int y = Height - 1; y >= 0; y--)
                    {
                        if (!ContainerMask[x, y]) continue;
                        if (Cells[x, y].IsEmpty)
                        {
                            Cells[x, y] = cell;
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 중력: 아래가 비어있는(떠 있는) 셀에만 적용
        /// ★ 안착한 셀에는 중력을 주지 않고 속도를 리셋 → 수면 떨림 방지
        /// </summary>
        private void ApplyGravityToVelocity(float gravityStrength)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    ref LiquidCell c = ref Cells[x, y];

                    // 아래가 막혀있는지 확인 (바닥 or 아래에 다른 셀 or 마스크 밖)
                    bool grounded = (y == 0)
                        || !ContainerMask[x, y - 1]
                        || !Cells[x, y - 1].IsEmpty;

                    if (grounded)
                    {
                        // ★ 안착한 셀: 미세한 속도를 제거하여 떨림 방지
                        if (Mathf.Abs(c.velocityX) < 1.0f) c.velocityX = 0f;
                        if (Mathf.Abs(c.velocityY) < 1.0f) c.velocityY = 0f;
                    }
                    else
                    {
                        // 떠 있는 셀에만 중력 적용
                        c.velocityY -= gravityStrength;
                    }
                }
        }

        /// <summary>
        /// 중력 낙하: 빈 셀 위의 액체를 한 칸 아래로
        /// </summary>
        private void GravityFall()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 1; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    if (!Cells[x, y - 1].IsEmpty) continue;
                    if (!ContainerMask[x, y - 1]) continue;
                    // ★ 얼음이 차지한 셀로는 낙하하지 않음
                    if (IsOccupiedByIce(x, y - 1)) continue;

                    Cells[x, y - 1] = Cells[x, y];
                    Cells[x, y] = LiquidCell.Empty;
                }
        }

        /// <summary>
        /// 수평 확산: 높은 컬럼에서 낮은 컬럼으로
        /// </summary>
        private void HorizontalSpread()
        {
            int sx = sweepLTR ? 0 : Width - 1;
            int ex = sweepLTR ? Width : -1;
            int dx = sweepLTR ? 1 : -1;

            for (int x = sx; x != ex; x += dx)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;

                    bool blocked = (y == 0)
                        || !Cells[x, y - 1].IsEmpty
                        || !ContainerMask[x, y - 1];
                    if (!blocked) continue;

                    // 양옆 + 대각선 아래
                    TrySpread(x, y, x - 1, y);
                    TrySpread(x, y, x + 1, y);
                    TrySpread(x, y, x - 1, y - 1);
                    TrySpread(x, y, x + 1, y - 1);
                }
        }

        private void TrySpread(int fx, int fy, int tx, int ty)
        {
            if (tx < 0 || tx >= Width || ty < 0 || ty >= Height) return;
            if (!ContainerMask[tx, ty] || !Cells[tx, ty].IsEmpty) return;
            // ★ 얼음이 차지한 셀로는 확산하지 않음
            if (IsOccupiedByIce(tx, ty)) return;
            if (Random.value > 0.5f) return;

            Cells[tx, ty] = Cells[fx, fy];
            Cells[fx, fy] = LiquidCell.Empty;
        }

        /// <summary>
        /// ★ 마스크 밖에 있는 셀을 제거 (떠다니는 픽셀 정리)
        /// </summary>
        private void CleanupStrayPixels()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    if (!ContainerMask[x, y])
                    {
                        // 마스크 밖의 셀 → 가장 가까운 마스크 내 빈 셀로 이동
                        bool relocated = false;
                        for (int r = 1; r <= 5 && !relocated; r++)
                        {
                            for (int dy = -r; dy <= r && !relocated; dy++)
                                for (int dx = -r; dx <= r && !relocated; dx++)
                                {
                                    int nx = x + dx, ny = y + dy;
                                    if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) continue;
                                    if (!ContainerMask[nx, ny] || !Cells[nx, ny].IsEmpty) continue;
                                    Cells[nx, ny] = Cells[x, y];
                                    relocated = true;
                                }
                        }
                        Cells[x, y] = LiquidCell.Empty; // 재배치 못 해도 제거
                    }
                }
        }

        // ============================================================
        // 속도 기반 이동
        // ============================================================

        private void ApplyVelocity()
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    ref LiquidCell cell = ref Cells[x, y];
                    if (cell.IsEmpty) continue;

                    float spd = Mathf.Sqrt(cell.velocityX * cell.velocityX
                        + cell.velocityY * cell.velocityY);
                    if (spd < 0.3f) continue;

                    int tx = Mathf.Clamp(x + Mathf.RoundToInt(cell.velocityX), 0, Width - 1);
                    int ty = Mathf.Clamp(y + Mathf.RoundToInt(cell.velocityY), 0, Height - 1);
                    if (tx == x && ty == y) continue;

                    if (!ContainerMask[tx, ty])
                    {
                        // 벽 반사
                        cell.velocityX *= -0.4f;
                        cell.velocityY *= -0.4f;
                        continue;
                    }

                    // ★ 얼음도 벽처럼 취급
                    if (IsOccupiedByIce(tx, ty))
                    {
                        cell.velocityX *= -0.3f;
                        cell.velocityY *= -0.3f;
                        continue;
                    }

                    if (Cells[tx, ty].IsEmpty)
                    {
                        Cells[tx, ty] = cell;
                        Cells[x, y] = LiquidCell.Empty;
                    }
                    else
                    {
                        float ms = Mathf.Clamp(spd * 0.08f, 0.02f, 0.15f);
                        MixTwo(ref Cells[x, y], ref Cells[tx, ty], ms);
                        LiquidCell tmp = Cells[tx, ty];
                        Cells[tx, ty] = cell;
                        Cells[x, y] = tmp;
                    }
                }
        }

        private void ApplyDamping(float dampingValue)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    ref LiquidCell c = ref Cells[x, y];
                    c.velocityX *= dampingValue;
                    c.velocityY *= dampingValue;
                }
        }

        // ============================================================
        // 행 기반 정착 (완전 안정 후 최종 정리)
        // ============================================================

        private void SettleRowBased()
        {
            allLiquid.Clear();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    if (!Cells[x, y].IsEmpty)
                    {
                        allLiquid.Add(Cells[x, y]);
                        Cells[x, y] = LiquidCell.Empty;
                    }
                }
            if (allLiquid.Count == 0) return;

            SortByDensity(allLiquid);

            // ★ 바닥부터 채우되, 얼음이 차지한 셀은 건너뜀
            int idx = 0;
            for (int y = 0; y < Height && idx < allLiquid.Count; y++)
            {
                var xs = rowValidXs[y];
                for (int i = 0; i < xs.Count && idx < allLiquid.Count; i++)
                {
                    int cellX = xs[i];
                    // ★ 얼음이 이 셀을 차지하고 있으면 건너뜀
                    if (IsOccupiedByIce(cellX, y)) continue;

                    LiquidCell cell = allLiquid[idx];
                    cell.velocityX = 0f;
                    cell.velocityY = 0f;
                    Cells[cellX, y] = cell;
                    idx++;
                }
            }
        }

        private void SortByDensity(List<LiquidCell> cells)
        {
            for (int i = 1; i < cells.Count; i++)
            {
                LiquidCell key = cells[i];
                int j = i - 1;
                while (j >= 0
                    && cells[j].mixRatio < 0.3f
                    && key.mixRatio < 0.3f
                    && cells[j].density < key.density)
                {
                    cells[j + 1] = cells[j];
                    j--;
                }
                cells[j + 1] = key;
            }
        }

        // ============================================================
        // 외부 인터페이스
        // ============================================================

        public void AddLiquid(int centerX, int amount, LiquidData data)
        {
            int added = 0;
            for (int y = Height - 1; y >= 0 && added < amount; y--)
            {
                var xs = rowValidXs[y];
                for (int i = 0; i < xs.Count && added < amount; i++)
                {
                    int lx = xs[i];
                    if (!Cells[lx, y].IsEmpty) continue;
                    Cells[lx, y] = new LiquidCell
                    {
                        liquidType = data.liquidType,
                        color = data.color,
                        density = data.density,
                        mixRatio = 0f,
                        velocityX = 0f,
                        velocityY = 0f
                    };
                    added++;
                }
            }
            SettleRowBased();
        }

        public LiquidCell RemoveLiquidFromTop(int centerX)
        {
            for (int y = Height - 1; y >= 0; y--)
            {
                var xs = rowValidXs[y];
                int bestIdx = -1, bestDist = int.MaxValue;
                for (int i = 0; i < xs.Count; i++)
                {
                    if (Cells[xs[i], y].IsEmpty) continue;
                    int d = Mathf.Abs(xs[i] - centerX);
                    if (d < bestDist) { bestDist = d; bestIdx = i; }
                }
                if (bestIdx >= 0)
                {
                    int rx = xs[bestIdx];
                    LiquidCell removed = Cells[rx, y];
                    Cells[rx, y] = LiquidCell.Empty;
                    return removed;
                }
            }
            return LiquidCell.Empty;
        }

        public void ApplyForce(Vector2 pos, Vector2 force, float radius)
        {
            int cx = Mathf.RoundToInt(pos.x), cy = Mathf.RoundToInt(pos.y);
            int r = Mathf.CeilToInt(radius);
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(Width - 1, cx + r); x++)
                for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(Height - 1, cy + r); y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    float d = Vector2.Distance(new Vector2(x, y), pos);
                    if (d > radius) continue;
                    float f = 1f - d / radius;
                    ref LiquidCell c = ref Cells[x, y];
                    c.velocityX += force.x * f;
                    c.velocityY += force.y * f;
                }
        }

        public void ApplyVortex(Vector2 center, float strength, float radius)
        {
            // ★ 휘젓기는 turbulence를 올리지 않음 → 정착 모드 유지
            // (액체가 잔 안에서 섞이되, 물리 모드로 전환되지 않음)

            int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
            int r = Mathf.CeilToInt(radius);
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(Width - 1, cx + r); x++)
                for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(Height - 1, cy + r); y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > radius || d < 0.1f) continue;
                    float f = 1f - d / radius;
                    Vector2 dir = (new Vector2(x, y) - center).normalized;
                    Vector2 tan = new Vector2(-dir.y, dir.x);

                    // ★ 수평 힘만 적용, 수직 힘 완전 제거
                    ref LiquidCell c = ref Cells[x, y];
                    c.velocityX += tan.x * strength * f;
                    // velocityY는 건드리지 않음 (위로 튀는 것 원천 차단)

                    // 수평 속도도 제한
                    c.velocityX = Mathf.Clamp(c.velocityX, -2f, 2f);
                }
            MixInRadius(center, radius, Mathf.Clamp(strength * 0.03f, 0.005f, 0.08f));

            // ★ 휘젓기에서는 평균색 수렴 제거 (잔에서는 섞이되 단색이 되면 안 됨)
            // ConvergeToAverageColor는 ShakeAll에서만 호출

            // ★ 얼음에 부드러운 회전 힘 (매우 약하게)
            foreach (var ice in IcePieces)
            {
                float d = Vector2.Distance(ice.position, center);
                if (d > radius * 1.5f) continue;
                float f = 1f - d / (radius * 1.5f);
                Vector2 dir = (ice.position - center).normalized;
                Vector2 tan = new Vector2(-dir.y, dir.x);
                // ★ 수평 힘만, 매우 약하게
                ice.velocity.x += tan.x * strength * f * 0.08f;
                ice.angularVelocity += tan.x * f * 0.02f;
            }
        }

        /// <summary>
        /// 전체 흔들기 (쉐이커)
        /// </summary>
        public void ShakeAll(float intensity)
        {
            if (intensity < 0.001f) return;

            turbulence = Mathf.Clamp01(turbulence + intensity * 3f);

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    ref LiquidCell c = ref Cells[x, y];
                    c.velocityX += Random.Range(-intensity * 2f, intensity * 2f);
                    c.velocityY += Random.Range(-intensity * 3f, intensity * 3f);
                }

            // 셀 교환 + 혼합
            int swaps = Mathf.Max(3, Mathf.RoundToInt(intensity * Width * Height * 0.06f));
            for (int i = 0; i < swaps; i++)
            {
                int x1 = Random.Range(0, Width), y1 = Random.Range(0, Height);
                int x2 = Mathf.Clamp(x1 + Random.Range(-4, 5), 0, Width - 1);
                int y2 = Mathf.Clamp(y1 + Random.Range(-5, 6), 0, Height - 1);
                if (!ContainerMask[x1, y1] || !ContainerMask[x2, y2]) continue;
                if (Cells[x1, y1].IsEmpty && Cells[x2, y2].IsEmpty) continue;

                if (!Cells[x1, y1].IsEmpty && !Cells[x2, y2].IsEmpty
                    && Cells[x1, y1].liquidType != Cells[x2, y2].liquidType)
                {
                    MixTwo(ref Cells[x1, y1], ref Cells[x2, y2],
                        Mathf.Clamp(intensity * 0.4f, 0.08f, 0.5f));
                }
                LiquidCell tmp = Cells[x1, y1];
                Cells[x1, y1] = Cells[x2, y2];
                Cells[x2, y2] = tmp;
            }

            MixAdjacentCells(Mathf.Clamp(intensity * 0.1f, 0.02f, 0.2f));

            // ★ 전체 평균색으로 수렴: 모든 셀이 하나의 혼합색을 향해 이동
            ConvergeToAverageColor(Mathf.Clamp(intensity * 0.15f, 0.02f, 0.25f));

            // ★ 얼음에도 랜덤 힘 적용 (약하게 - 용기 안에서만 움직임)
            foreach (var ice in IcePieces)
            {
                ice.velocity.x += Random.Range(-intensity * 0.5f, intensity * 0.5f);
                ice.velocity.y += Random.Range(-intensity * 0.8f, intensity * 0.8f);
                ice.angularVelocity += Random.Range(-intensity * 0.15f, intensity * 0.15f);
            }
        }

        /// <summary>
        /// ★ 모든 액체 셀의 평균색을 계산하고, 각 셀을 평균색 쪽으로 보간
        /// 흔들수록 빠르게 단일 혼합색으로 수렴
        /// </summary>
        private void ConvergeToAverageColor(float convergenceRate)
        {
            // 1) 평균색 + 평균 밀도 계산
            int count = 0;
            float sumR = 0f, sumG = 0f, sumB = 0f, sumA = 0f;
            float sumDensity = 0f;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    Color32 c = Cells[x, y].color;
                    sumR += c.r;
                    sumG += c.g;
                    sumB += c.b;
                    sumA += c.a;
                    sumDensity += Cells[x, y].density;
                    count++;
                }

            if (count == 0) return;

            Color32 avgColor = new Color32(
                (byte)(sumR / count),
                (byte)(sumG / count),
                (byte)(sumB / count),
                (byte)(sumA / count)
            );
            float avgDensity = sumDensity / count;

            // 2) 모든 셀을 평균색 쪽으로 보간
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    ref LiquidCell cell = ref Cells[x, y];

                    cell.color = Color32.Lerp(cell.color, avgColor, convergenceRate);
                    cell.density = Mathf.Lerp(cell.density, avgDensity, convergenceRate * 0.5f);
                    cell.mixRatio = Mathf.Clamp01(cell.mixRatio + convergenceRate);

                    if (cell.mixRatio >= 0.95f)
                    {
                        cell.liquidType = LiquidType.Mixed;
                        cell.mixRatio = 1f;
                    }
                }
        }

        /// <summary>
        /// 용기 이동에 의한 관성
        /// </summary>
        public void ApplyContainerMotion(Vector2 containerVelocity)
        {
            if (containerVelocity.magnitude < 0.3f) return;

            Vector2 inertia = -containerVelocity * 0.4f;
            turbulence = Mathf.Clamp01(turbulence + containerVelocity.magnitude * 0.06f);

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    ref LiquidCell c = ref Cells[x, y];
                    c.velocityX += inertia.x;
                    c.velocityY += inertia.y;
                }

            // ★ 얼음에도 관성 적용 (약하게)
            foreach (var ice in IcePieces)
            {
                ice.velocity.x += inertia.x * 0.3f;
                ice.velocity.y += inertia.y * 0.3f;
            }
        }

        // ============================================================
        // 혼합
        // ============================================================

        public void MixInRadius(Vector2 center, float radius, float mixAmount)
        {
            int cx = Mathf.RoundToInt(center.x), cy = Mathf.RoundToInt(center.y);
            int r = Mathf.CeilToInt(radius);
            for (int x = Mathf.Max(0, cx - r); x <= Mathf.Min(Width - 1, cx + r); x++)
                for (int y = Mathf.Max(0, cy - r); y <= Mathf.Min(Height - 1, cy + r); y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d > radius) continue;
                    float f = (1f - d / radius) * mixAmount;
                    if (x + 1 < Width && !Cells[x + 1, y].IsEmpty)
                        MixTwo(ref Cells[x, y], ref Cells[x + 1, y], f);
                    if (y + 1 < Height && !Cells[x, y + 1].IsEmpty)
                        MixTwo(ref Cells[x, y], ref Cells[x, y + 1], f);
                }
        }

        public void MixAdjacentCells(float mixAmount)
        {
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    if (Cells[x, y].IsEmpty) continue;
                    if (x + 1 < Width && !Cells[x + 1, y].IsEmpty)
                        MixTwo(ref Cells[x, y], ref Cells[x + 1, y], mixAmount);
                    if (y + 1 < Height && !Cells[x, y + 1].IsEmpty)
                        MixTwo(ref Cells[x, y], ref Cells[x, y + 1], mixAmount);
                }
        }

        private void MixTwo(ref LiquidCell a, ref LiquidCell b, float amount)
        {
            if (a.IsEmpty || b.IsEmpty) return;
            if (a.liquidType == b.liquidType && a.liquidType != LiquidType.Mixed) return;

            a.mixRatio = Mathf.Clamp01(a.mixRatio + amount);
            b.mixRatio = Mathf.Clamp01(b.mixRatio + amount);

            Color32 bl = Color32.Lerp(a.color, b.color, 0.5f);
            a.color = Color32.Lerp(a.color, bl, amount);
            b.color = Color32.Lerp(b.color, bl, amount);

            float avg = (a.density + b.density) * 0.5f;
            a.density = Mathf.Lerp(a.density, avg, amount * 0.5f);
            b.density = Mathf.Lerp(b.density, avg, amount * 0.5f);

            if (a.mixRatio >= 0.95f) { a.liquidType = LiquidType.Mixed; a.mixRatio = 1f; }
            if (b.mixRatio >= 0.95f) { b.liquidType = LiquidType.Mixed; b.mixRatio = 1f; }
        }

        // ============================================================
        // 유틸리티
        // ============================================================

        public int GetTotalLiquidCount()
        {
            int c = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (!Cells[x, y].IsEmpty) c++;
            return c;
        }

        public int GetSurfaceHeight(int x)
        {
            for (int y = Height - 1; y >= 0; y--)
                if (!Cells[x, y].IsEmpty) return y;
            return -1;
        }
    }
}
