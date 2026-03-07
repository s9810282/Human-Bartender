using UnityEngine;

namespace LiquidSimulation
{
    /// <summary>
    /// 얼음 형태 종류
    /// </summary>
    public enum IceShape
    {
        Cube,       // 정육면체 (큰 사각형)
        HalfMoon,   // 반달형
        Sphere,     // 둥근 형태
        Crushed,    // 부서진 얼음 (작고 불규칙)
        Long,       // 긴 막대형
    }

    /// <summary>
    /// 단일 얼음 조각 (강체)
    /// 
    /// 그리드 위에서 강체로 동작:
    /// - 위치/속도/회전을 가짐
    /// - 액체 위에 떠다님 (부력)
    /// - 외부 힘(교반/흔들기)에 반응
    /// - 벽/다른 얼음과 충돌
    /// - 시간에 따라 녹을 수 있음 (선택)
    /// </summary>
    public class IcePiece
    {
        // 물리
        public Vector2 position;       // 그리드 좌표 (중심)
        public Vector2 velocity;
        public float rotation;         // 라디안
        public float angularVelocity;

        // 형태
        public IceShape shapeType;
        public bool[,] shape;          // 픽셀 패턴 (로컬 좌표)
        public int shapeW, shapeH;

        // 속성
        public float density = 0.9f;   // 물(1.0)보다 약간 가벼움 → 뜸
        public float meltProgress;     // 0 = 꽁꽁, 1 = 완전히 녹음
        public float meltRate = 0f; // 기본=안 녹음, Create()에서 설정

        // ★ 점진적 녹기 추적
        private int totalPixels;        // 초기 픽셀 수
        private float waterProduced;    // 지금까지 생성된 물 셀 수 (소수점)

        // 시각
        public Color32 baseColor = new Color32(180, 220, 240, 200);
        public Color32 highlightColor = new Color32(220, 240, 255, 230);

        // 물리 상수
        private const float BUOYANCY = 0.6f;
        private const float DRAG = 0.92f;
        private const float ANGULAR_DRAG = 0.88f;
        private const float BOUNCE = 0.3f;

        /// <summary>
        /// 얼음 생성
        /// </summary>
        public static IcePiece Create(IceShape type, Vector2 gridPos)
        {
            var ice = new IcePiece();
            ice.shapeType = type;
            ice.position = gridPos;
            ice.velocity = Vector2.zero;
            ice.rotation = 0f;
            ice.angularVelocity = 0f;
            ice.meltProgress = 0f;

            switch (type)
            {
                case IceShape.Cube:
                    ice.MakeCube(9, 8);       // was 6x5
                    break;
                case IceShape.HalfMoon:
                    ice.MakeHalfMoon(11);     // was 7
                    break;
                case IceShape.Sphere:
                    ice.MakeSphere(6);        // was 4 (diameter 13 vs 9)
                    break;
                case IceShape.Crushed:
                    ice.MakeCrushed();        // 4~7 x 3~5 (was 3~5 x 2~4)
                    break;
                case IceShape.Long:
                    ice.MakeLong(12, 4);      // was 8x3
                    break;
            }

            // ★ 기본 녹는 속도 설정
            ice.meltRate = 0.0008f;  // ~50초에 완전히 녹음 (25틱/초 * 50초 * 0.0008 ≈ 1.0)

            // ★ 초기 픽셀 수 기록 (점진적 물 생성 계산용)
            ice.totalPixels = ice.CountShapePixels();
            ice.waterProduced = 0f;

            return ice;
        }

        // ============================================================
        // 형태 생성
        // ============================================================

        private void MakeCube(int w, int h)
        {
            shapeW = w; shapeH = h;
            shape = new bool[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    shape[x, y] = true;
            // 모서리 깎기
            shape[0, 0] = false;
            shape[w - 1, 0] = false;
            shape[0, h - 1] = false;
            shape[w - 1, h - 1] = false;
        }

        private void MakeHalfMoon(int size)
        {
            shapeW = size; shapeH = size / 2 + 1;
            shape = new bool[shapeW, shapeH];
            float r = size * 0.5f;
            float cx = size * 0.5f;
            for (int x = 0; x < shapeW; x++)
                for (int y = 0; y < shapeH; y++)
                {
                    float dx = x - cx;
                    float dy = y;
                    shape[x, y] = (dx * dx + dy * dy) <= r * r;
                }
        }

        private void MakeSphere(int radius)
        {
            int d = radius * 2 + 1;
            shapeW = d; shapeH = d;
            shape = new bool[d, d];
            for (int x = 0; x < d; x++)
                for (int y = 0; y < d; y++)
                {
                    float dx = x - radius;
                    float dy = y - radius;
                    shape[x, y] = (dx * dx + dy * dy) <= radius * radius;
                }
        }

        private void MakeCrushed()
        {
            shapeW = Random.Range(4, 7);    // was 3~5
            shapeH = Random.Range(3, 5);    // was 2~4
            shape = new bool[shapeW, shapeH];
            for (int x = 0; x < shapeW; x++)
                for (int y = 0; y < shapeH; y++)
                    shape[x, y] = Random.value > 0.2f;
            // 최소 1픽셀 보장
            shape[shapeW / 2, shapeH / 2] = true;
        }

        private void MakeLong(int w, int h)
        {
            shapeW = w; shapeH = h;
            shape = new bool[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    shape[x, y] = true;
            // 양 끝 둥글게
            shape[0, 0] = false;
            shape[0, h - 1] = false;
            shape[w - 1, 0] = false;
            shape[w - 1, h - 1] = false;
        }

        // ============================================================
        // 물리 업데이트
        // ============================================================

        /// <summary>
        /// 한 틱 물리 시뮬레이션
        /// </summary>
        public void PhysicsStep(LiquidGrid grid)
        {
            if (meltProgress >= 1f) return;

            // 1) 부력
            ApplyBuoyancy(grid);

            // ★ 속도 상한 (절대 튀어나가지 않도록)
            velocity.x = Mathf.Clamp(velocity.x, -2f, 2f);
            velocity.y = Mathf.Clamp(velocity.y, -2.5f, 2f);

            // 2) 속도 적용
            position += velocity;

            // 3) 회전
            angularVelocity = Mathf.Clamp(angularVelocity, -0.3f, 0.3f);
            rotation += angularVelocity;

            // 4) 벽 충돌
            ClampToContainer(grid);

            // 5) 감쇠
            velocity *= DRAG;
            angularVelocity *= ANGULAR_DRAG;

            // 6) 녹기
            if (meltRate > 0f)
            {
                meltProgress += meltRate;
                if (meltProgress >= 1f)
                    meltProgress = 1f;
            }
        }

        /// <summary>
        /// 부력: 얼음 픽셀이 액체와 겹치는 비율로 부력 계산
        /// 
        /// 실제 물리: 얼음 밀도 0.9 → 90%가 물속에 잠기고 10%가 수면 위
        /// 
        /// - 액체가 없으면: 그냥 중력으로 바닥에 앉음
        /// - 액체 안에 있으면: 부력으로 위로 떠오름
        /// - 수면 근처에서: 90% 잠긴 상태로 균형
        /// </summary>
        private void ApplyBuoyancy(LiquidGrid grid)
        {
            int cx = Mathf.RoundToInt(position.x);
            int cy = Mathf.RoundToInt(position.y);

            int totalPixels = 0;
            int submergedPixels = 0; // 액체와 겹치는 픽셀
            int belowSurface = 0;    // 아래쪽에 액체가 있는 횟수

            // 얼음의 각 픽셀이 액체와 겹치는지 확인
            for (int lx = 0; lx < shapeW; lx++)
                for (int ly = 0; ly < shapeH; ly++)
                {
                    if (!shape[lx, ly]) continue;
                    totalPixels++;

                    // 로컬 → 월드 좌표 (회전 포함)
                    float cos = Mathf.Cos(rotation);
                    float sin = Mathf.Sin(rotation);
                    float dx = lx - shapeW * 0.5f;
                    float dy = ly - shapeH * 0.5f;
                    int wx = Mathf.RoundToInt(position.x + dx * cos - dy * sin);
                    int wy = Mathf.RoundToInt(position.y + dx * sin + dy * cos);

                    if (wx < 0 || wx >= grid.Width || wy < 0 || wy >= grid.Height) continue;

                    // 이 위치에 액체가 있으면 잠긴 것
                    if (!grid.Cells[wx, wy].IsEmpty)
                        submergedPixels++;

                    // 아래에 액체가 있는지 (부력 지지)
                    if (wy > 0 && !grid.Cells[wx, wy - 1].IsEmpty)
                        belowSurface++;
                }

            if (totalPixels == 0) return;

            float submergedRatio = (float)submergedPixels / totalPixels;
            float supportRatio = (float)belowSurface / totalPixels;

            // 중력은 항상 적용
            velocity.y -= 0.3f;

            // ★ 부력: 잠긴 비율에 비례 (아르키메데스 원리)
            // 밀도 0.9이므로, 잠긴 비율이 0.9 이상이면 부력 > 중력
            float buoyancyForce = submergedRatio * (1.0f / density) * 0.4f;
            velocity.y += buoyancyForce;

            // ★ 아래에 지지하는 액체가 있으면 추가 부력
            if (supportRatio > 0.2f)
                velocity.y += supportRatio * 0.15f;

            // ★ 수면 근처에서 안정화: 잠긴 비율이 ~0.8~0.95이면 속도 감쇠
            if (submergedRatio > 0.7f && submergedRatio < 0.98f)
            {
                velocity.y *= 0.8f; // 수면 근처에서 진동 방지
            }
        }

        /// <summary>
        /// 컨테이너 벽 충돌 - 마스크 기반으로 얼음을 안에 가둠
        /// </summary>
        private void ClampToContainer(LiquidGrid grid)
        {
            float halfW = shapeW * 0.5f;
            float halfH = shapeH * 0.5f;

            // 그리드 경계 클램프
            position.x = Mathf.Clamp(position.x, halfW + 1, grid.Width - halfW - 2);
            position.y = Mathf.Clamp(position.y, halfH + 1, grid.Height - halfH - 2);

            // 중심이 마스크 밖이면 안쪽으로 밀기
            int cx = Mathf.Clamp(Mathf.RoundToInt(position.x), 0, grid.Width - 1);
            int cy = Mathf.Clamp(Mathf.RoundToInt(position.y), 0, grid.Height - 1);

            if (!grid.ContainerMask[cx, cy])
            {
                velocity *= -BOUNCE;

                // 마스크 안쪽의 가장 가까운 유효 셀 찾기
                float bestDist = float.MaxValue;
                int bestX = cx, bestY = cy;
                int searchR = Mathf.Max(shapeW, shapeH);
                for (int sx = cx - searchR; sx <= cx + searchR; sx++)
                    for (int sy = cy - searchR; sy <= cy + searchR; sy++)
                    {
                        if (sx < 0 || sx >= grid.Width || sy < 0 || sy >= grid.Height) continue;
                        if (!grid.ContainerMask[sx, sy]) continue;
                        float d = (sx - cx) * (sx - cx) + (sy - cy) * (sy - cy);
                        if (d < bestDist) { bestDist = d; bestX = sx; bestY = sy; }
                    }
                position.x = bestX;
                position.y = bestY;
            }

            // 좌우 마스크 경계 확인
            int leftX = Mathf.RoundToInt(position.x - halfW);
            int rightX = Mathf.RoundToInt(position.x + halfW);
            if (leftX >= 0 && leftX < grid.Width && !grid.ContainerMask[leftX, cy])
            {
                position.x += 1f;
                velocity.x = Mathf.Abs(velocity.x) * BOUNCE;
            }
            if (rightX >= 0 && rightX < grid.Width && !grid.ContainerMask[rightX, cy])
            {
                position.x -= 1f;
                velocity.x = -Mathf.Abs(velocity.x) * BOUNCE;
            }

            // 상하 마스크 경계 확인
            int bottomY = Mathf.RoundToInt(position.y - halfH);
            int topY = Mathf.RoundToInt(position.y + halfH);
            if (bottomY >= 0 && bottomY < grid.Height && !grid.ContainerMask[cx, bottomY])
            {
                position.y += 1f;
                velocity.y = Mathf.Abs(velocity.y) * BOUNCE;
            }
            if (topY >= 0 && topY < grid.Height && !grid.ContainerMask[cx, topY])
            {
                position.y -= 1f;
                velocity.y = -Mathf.Abs(velocity.y) * BOUNCE;
            }
        }

        // ============================================================
        // 외부 힘
        // ============================================================

        /// <summary>
        /// 힘 적용 (교반/흔들기에서 호출)
        /// </summary>
        public void ApplyForce(Vector2 force)
        {
            velocity += force;
        }

        /// <summary>
        /// 회전을 포함한 충격 (특정 지점에 힘)
        /// </summary>
        public void ApplyImpulse(Vector2 worldPoint, Vector2 force)
        {
            velocity += force;

            // 충격 지점과 중심의 수직 거리 → 토크
            Vector2 arm = worldPoint - position;
            float torque = arm.x * force.y - arm.y * force.x;
            angularVelocity += torque * 0.05f;
        }

        // ============================================================
        // 렌더링 헬퍼
        // ============================================================

        /// <summary>
        /// 그리드 좌표 (gx, gy)에 이 얼음의 픽셀이 있는지,
        /// 있다면 색상을 반환
        /// </summary>
        public bool GetPixel(int gx, int gy, out Color32 color)
        {
            color = baseColor;
            if (meltProgress >= 1f) return false;

            // 회전 적용한 로컬 좌표
            float cos = Mathf.Cos(-rotation);
            float sin = Mathf.Sin(-rotation);
            float dx = gx - position.x;
            float dy = gy - position.y;
            float lx = dx * cos - dy * sin + shapeW * 0.5f;
            float ly = dx * sin + dy * cos + shapeH * 0.5f;

            int ix = Mathf.RoundToInt(lx);
            int iy = Mathf.RoundToInt(ly);

            if (ix < 0 || ix >= shapeW || iy < 0 || iy >= shapeH) return false;
            if (!shape[ix, iy]) return false;

            // 색상: 가장자리는 밝게, 내부는 기본색
            bool isEdge = (ix == 0 || ix == shapeW - 1 || iy == 0 || iy == shapeH - 1);
            if (isEdge || !shape[Mathf.Max(0, ix - 1), iy]
                      || !shape[Mathf.Min(shapeW - 1, ix + 1), iy]
                      || !shape[ix, Mathf.Max(0, iy - 1)]
                      || !shape[ix, Mathf.Min(shapeH - 1, iy + 1)])
            {
                color = highlightColor;
            }
            else
            {
                color = baseColor;
            }

            // 녹아가면 투명해짐
            if (meltProgress > 0.5f)
            {
                float fade = 1f - (meltProgress - 0.5f) * 2f;
                color.a = (byte)(color.a * fade);
            }

            return true;
        }

        /// <summary>
        /// 얼음의 픽셀 수 (녹은 정도에 따라 감소)
        /// </summary>
        public int GetPixelCount()
        {
            int count = 0;
            for (int x = 0; x < shapeW; x++)
                for (int y = 0; y < shapeH; y++)
                    if (shape[x, y]) count++;
            return Mathf.RoundToInt(count * (1f - meltProgress));
        }

        public bool IsMelted => meltProgress >= 1f;

        /// <summary>
        /// shape에서 true인 픽셀 수 (순수 카운트, 녹음 무관)
        /// </summary>
        public int CountShapePixels()
        {
            int count = 0;
            for (int x = 0; x < shapeW; x++)
                for (int y = 0; y < shapeH; y++)
                    if (shape[x, y]) count++;
            return count;
        }

        /// <summary>
        /// ★ 이번 틱에 생성해야 할 물 셀 수 계산
        /// 
        /// meltProgress가 0→1로 진행되면서 totalPixels만큼의 물을 점진적으로 생성
        /// 예: 72픽셀 얼음, meltProgress=0.3 → 21.6개가 이미 녹았어야 함
        ///     이전에 20개 생성했다면 → 이번에 1개 생성
        /// </summary>
        public int GetWaterCellsThisTick()
        {
            float shouldHaveProduced = meltProgress * totalPixels;
            int toMake = Mathf.FloorToInt(shouldHaveProduced - waterProduced);
            if (toMake > 0)
                waterProduced += toMake;
            return Mathf.Max(0, toMake);
        }
    }
}
