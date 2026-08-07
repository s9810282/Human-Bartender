using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PBF(Position Based Fluids, Macklin &amp; Müller 2013) 솔버.
///
/// 이전의 힘 기반 SPH는 압력을 힘으로 가했기 때문에 중력에 밀렸다 — 정지 간격에서 압력힘이 중력의
/// 1/200이라 액체가 1/3로 짓눌린 뒤에야 균형을 잡았고(병을 채워도 바닥에 깔림), 그렇다고 압력을 올리면
/// 파티클이 서로 밀어내 줄기가 갈래갈래 흩어졌다.
///
/// PBF는 밀도를 '제약'으로 보고 위치를 직접 반복 보정한다. 제약은 반복해서 풀면 반드시 지켜지므로
/// 중력 크기와 무관하게 부피가 유지되고, 파티클 간격도 거의 균일해진다(렌더링 값을 한 번만 맞추면
/// 병 안이든 공중이든 똑같이 동작한다). 뭉치는 성질은 인공 압력(cohesion) 항으로 따로 조절한다.
///
/// 한 스텝의 흐름:
///   1. 중력 적용 → 예측 위치
///   2. 이웃 탐색
///   3. (밀도 제약 lambda 계산 → 위치 보정 → 벽 클램프) x solverIterations
///   4. 보정된 위치를 확정하고 거기서 속도를 되뽑음
///   5. XSPH 점성으로 속도 평활화
/// </summary>
public class SphSimulation
{
    public SphConfig config;

    /// <summary>기준 밀도. CalibrateRestDensity()가 스폰 격자에서 실측해 채운다.</summary>
    float restDensity = 1f;

    readonly List<SphParticle> activeParticles = new List<SphParticle>();
    public IReadOnlyList<SphParticle> ActiveParticles => activeParticles;

    readonly List<SphParticle>[,] grid;
    readonly int gridSizeX;
    readonly int gridSizeY;
    readonly Vector2 gridMin;
    readonly Vector2 gridMax;

    /// <summary>솔버 반복마다 위치를 바꾸므로, 벽 밖으로 밀려난 파티클을 매 반복 되돌리기 위한 콜백.
    /// 반복 밖에서 한 번만 걸면 파티클이 벽을 뚫은 채로 밀도가 계산돼 액체가 새어나간다.</summary>
    public Action<SphParticle> ConstrainToWalls;

    public SphSimulation(SphConfig config, Vector2 gridMin, Vector2 gridMax, int gridSizeX = 32, int gridSizeY = 32)
    {
        this.config = config;
        this.gridMin = gridMin;
        this.gridMax = gridMax;
        this.gridSizeX = gridSizeX;
        this.gridSizeY = gridSizeY;

        grid = new List<SphParticle>[gridSizeX, gridSizeY];
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                grid[x, y] = new List<SphParticle>();
    }

    public void Register(SphParticle particle)
    {
        if (!activeParticles.Contains(particle))
            activeParticles.Add(particle);
    }

    public void Unregister(SphParticle particle)
    {
        activeParticles.Remove(particle);
    }

    /// <summary>
    /// 스폰 격자 상태의 실제 밀도를 재서 기준 밀도로 삼는다. 절대값을 적어두면 spacing을 조금만
    /// 바꿔도 기준이 어긋나 액체가 부풀거나 눌리는데, 실측해두면 어떤 설정에서도 스폰 간격이 곧
    /// 평형 상태가 된다.
    /// </summary>
    public void CalibrateRestDensity()
    {
        if (activeParticles.Count == 0) return;

        RefreshKernelCoefficients();

        foreach (var p in activeParticles)
            p.predictedPos = p.pos;

        BuildNeighbours();

        float maxDensity = 0f;
        foreach (var p in activeParticles)
        {
            p.density = ComputeDensity(p);
            maxDensity = Mathf.Max(maxDensity, p.density);
        }

        if (maxDensity > 0f)
            restDensity = maxDensity;
    }

    public void Tick(float dt)
    {
        if (activeParticles.Count == 0) return;

        RefreshKernelCoefficients();

        foreach (var p in activeParticles)
            p.PredictPosition(dt);

        BuildNeighbours();

        for (int iteration = 0; iteration < config.solverIterations; iteration++)
        {
            ComputeLambdas();
            ApplyPositionCorrections();

            if (ConstrainToWalls != null)
            {
                foreach (var p in activeParticles)
                    ConstrainToWalls(p);
            }
        }

        foreach (var p in activeParticles)
            p.ApplyPrediction(dt);

        ApplyViscosity();
    }

    // ── 커널 ────────────────────────────────────────────────────────────
    // 2D Poly6(밀도)와 Spiky(기울기). PBF 논문의 3D 계수를 2D로 바꾼 것.
    //
    // 계수는 이웃 반경에서만 나오는 상수인데, 커널은 (파티클 x 이웃 x 솔버 반복 x 서브스텝)만큼
    // 불린다 — 파티클 748개면 프레임당 수십만 번이다. 계수를 그때마다 Mathf.Pow로 구하면
    // (배정밀도 pow라 한 번에 수십 ns) 그것만으로 프레임을 다 써버린다. 그래서 반경이 바뀔 때만 다시 잡는다.

    float kernelRadius;
    float kernelRadiusSqr;
    float poly6Coefficient;
    float spikyCoefficient;

    /// <summary>인공 압력의 기준 거리(이웃 반경의 30% 지점에서의 커널값). 논문의 관례값.</summary>
    float antiClusterReference;

    void RefreshKernelCoefficients()
    {
        float h = config.NeighborRadius;
        if (h == kernelRadius) return;

        kernelRadius = h;
        kernelRadiusSqr = h * h;
        poly6Coefficient = 4f / (Mathf.PI * Mathf.Pow(h, 8));
        spikyCoefficient = -30f / (Mathf.PI * Mathf.Pow(h, 5));

        float reference = 0.3f * h;
        antiClusterReference = Poly6(reference * reference);
    }

    float Poly6(float distanceSqr)
    {
        if (distanceSqr >= kernelRadiusSqr) return 0f;

        float diff = kernelRadiusSqr - distanceSqr;
        return poly6Coefficient * diff * diff * diff;
    }

    Vector2 SpikyGradient(Vector2 delta)
    {
        float distance = delta.magnitude;
        if (distance <= 1e-6f || distance >= kernelRadius) return Vector2.zero;

        float diff = kernelRadius - distance;
        float scale = spikyCoefficient * diff * diff;

        return delta * (scale / distance);
    }

    float ComputeDensity(SphParticle p)
    {
        float density = Poly6(0f); // 자기 자신

        foreach (var n in p.neighbours)
            density += Poly6((p.predictedPos - n.predictedPos).sqrMagnitude);

        return density;
    }

    // ── PBF 단계 ────────────────────────────────────────────────────────

    /// <summary>
    /// 각 파티클의 밀도 제약 C = density/restDensity - 1 을 만족시키는 라그랑주 승수를 구한다.
    /// 분모에 relaxation(CFM)을 더해 이웃이 적은 표면 파티클에서 값이 폭주하는 것을 막는다.
    /// </summary>
    void ComputeLambdas()
    {
        foreach (var p in activeParticles)
        {
            p.density = ComputeDensity(p);

            float constraint = p.density / restDensity - 1f;

            // 밀도가 기준보다 낮을 때(= 파티클이 벌어졌을 때)도 제약을 걸면 서로 끌어당겨 뭉친다.
            // 원래 PBF는 이 방향을 버려서(한쪽만 제약) 압축만 막는데, 그러면 액체가 낱알로 흩어진다.
            // cohesion으로 이 방향의 세기만 따로 조절한다 — 0이면 원본처럼 압축만 막는다.
            if (constraint < 0f)
                constraint *= Mathf.Clamp01(config.cohesion);

            if (Mathf.Approximately(constraint, 0f))
            {
                p.lambda = 0f;
                continue;
            }

            Vector2 gradientSum = Vector2.zero;
            float squaredGradientSum = 0f;

            foreach (var n in p.neighbours)
            {
                Vector2 gradient = SpikyGradient(p.predictedPos - n.predictedPos) / restDensity;

                gradientSum += gradient;
                squaredGradientSum += gradient.sqrMagnitude;
            }

            squaredGradientSum += gradientSum.sqrMagnitude;

            p.lambda = -constraint / (squaredGradientSum + config.relaxation);
        }
    }

    /// <summary>
    /// lambda로부터 위치 보정량을 구해 예측 위치를 옮긴다.
    /// antiClustering은 PBF 논문의 인공 압력(s_corr) 항으로, 파티클 몇 개가 한 점에 뭉쳐 굳어버리는
    /// 현상을 막는 '반발' 힘이다(응집력이 아니다 — 올리면 오히려 액체가 낱알로 흩어진다).
    /// </summary>
    void ApplyPositionCorrections()
    {
        bool useAntiClustering = config.antiClustering > 0f && antiClusterReference > 0f;

        foreach (var p in activeParticles)
            p.deltaPos = Vector2.zero;

        foreach (var p in activeParticles)
        {
            foreach (var n in p.neighbours)
            {
                Vector2 delta = p.predictedPos - n.predictedPos;

                float antiClusterTerm = 0f;
                if (useAntiClustering)
                {
                    float ratio = Poly6(delta.sqrMagnitude) / antiClusterReference;
                    antiClusterTerm = -config.antiClustering * ratio * ratio * ratio * ratio;
                }

                p.deltaPos += (p.lambda + n.lambda + antiClusterTerm) * SpikyGradient(delta) / restDensity;
            }
        }

        // 위치 보정량은 커널 스케일에 따라 값이 크게 달라져, 설정이 어긋나면 한 번에 간격의 몇 배씩
        // 튕겨나가며 폭발한다. 상한을 걸어 그런 폭주만 잘라낸다(정상 범위에서는 걸리지 않는다).
        float maxCorrection = config.spacing * config.maxCorrectionRatio;
        float maxCorrectionSqr = maxCorrection * maxCorrection;

        foreach (var p in activeParticles)
        {
            if (p.deltaPos.sqrMagnitude > maxCorrectionSqr)
                p.deltaPos = p.deltaPos.normalized * maxCorrection;

            p.predictedPos += p.deltaPos;
        }
    }

    /// <summary>XSPH 점성. 속도를 이웃 평균 쪽으로 당겨 액체가 한 덩어리로 움직이게 한다.</summary>
    void ApplyViscosity()
    {
        float strength = Mathf.Clamp01(config.viscosity);
        if (strength <= 0f) return;

        foreach (var p in activeParticles)
        {
            Vector2 weightedSum = Vector2.zero;
            float weightTotal = 0f;

            foreach (var n in p.neighbours)
            {
                float weight = Poly6((p.pos - n.pos).sqrMagnitude);

                weightedSum += n.velocity * weight;
                weightTotal += weight;
            }

            p.smoothedVelocity = weightTotal > 0f
                ? Vector2.Lerp(p.velocity, weightedSum / weightTotal, strength)
                : p.velocity;
        }

        foreach (var p in activeParticles)
            p.velocity = p.smoothedVelocity;
    }

    // ── 이웃 탐색 ───────────────────────────────────────────────────────

    void BuildNeighbours()
    {
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                grid[x, y].Clear();

        Vector2 size = gridMax - gridMin;

        foreach (var p in activeParticles)
        {
            p.gridX = Mathf.Clamp(Mathf.FloorToInt((p.predictedPos.x - gridMin.x) / size.x * gridSizeX), 0, gridSizeX - 1);
            p.gridY = Mathf.Clamp(Mathf.FloorToInt((p.predictedPos.y - gridMin.y) / size.y * gridSizeY), 0, gridSizeY - 1);

            grid[p.gridX, p.gridY].Add(p);
        }

        float radiusSqr = config.NeighborRadius * config.NeighborRadius;

        foreach (var p in activeParticles)
        {
            p.neighbours.Clear();

            for (int gx = p.gridX - 1; gx <= p.gridX + 1; gx++)
            {
                for (int gy = p.gridY - 1; gy <= p.gridY + 1; gy++)
                {
                    if (gx < 0 || gx >= gridSizeX || gy < 0 || gy >= gridSizeY) continue;

                    foreach (var n in grid[gx, gy])
                    {
                        if (n == p) continue;
                        if ((p.predictedPos - n.predictedPos).sqrMagnitude >= radiusSqr) continue;

                        p.neighbours.Add(n);
                    }
                }
            }
        }
    }
}
