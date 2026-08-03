using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SPH 밀도/압력/점성 계산 + 공간 분할 그리드. AlexandreSajus/Unity-Fluid-Simulation의 Simulation.cs를
/// 최대한 그대로 이식했다(밀도·압력힘·점성힘을 "쌍을 이루는 두 파티클 모두에서" 누적하는 구조 포함 —
/// 이 이중 누적은 원본 K 계수 튜닝에 이미 녹아있는 특성이라 그대로 유지, 임의로 "정리"하면 검증된
/// 레퍼런스 거동과 달라질 위험이 있어 그대로 둔다).
///
/// PourManager가 파티클들의 Integrate()가 끝난 뒤 시점에 맞춰 매 프레임 Tick()을 명시적으로 호출한다.
/// </summary>
public class SphSimulation
{
    public SphConfig config;

    readonly List<SphParticle> activeParticles = new List<SphParticle>();
    public IReadOnlyList<SphParticle> ActiveParticles => activeParticles;

    readonly List<SphParticle>[,] grid;
    readonly int gridSizeX;
    readonly int gridSizeY;
    readonly Vector2 gridMin;
    readonly Vector2 gridMax;

    public SphSimulation(SphConfig config, Vector2 gridMin, Vector2 gridMax, int gridSizeX = 24, int gridSizeY = 24)
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

    public void Tick(float dt)
    {
        foreach (var p in activeParticles)
            p.ResetForFrame();

        AssignToGrid();
        CalculateDensity();

        foreach (var p in activeParticles)
            p.CalculatePressure();

        ApplyPressureForce();
        ApplyViscosity();

        foreach (var p in activeParticles)
            p.Integrate(dt);
    }

    void AssignToGrid()
    {
        for (int x = 0; x < gridSizeX; x++)
            for (int y = 0; y < gridSizeY; y++)
                grid[x, y].Clear();

        Vector2 size = gridMax - gridMin;

        foreach (var p in activeParticles)
        {
            p.gridX = Mathf.Clamp(Mathf.FloorToInt((p.pos.x - gridMin.x) / size.x * gridSizeX), 0, gridSizeX - 1);
            p.gridY = Mathf.Clamp(Mathf.FloorToInt((p.pos.y - gridMin.y) / size.y * gridSizeY), 0, gridSizeY - 1);

            grid[p.gridX, p.gridY].Add(p);
        }
    }

    void CalculateDensity()
    {
        float r = config.neighborRadius;

        foreach (var p in activeParticles)
        {
            for (int gx = p.gridX - 1; gx <= p.gridX + 1; gx++)
            {
                for (int gy = p.gridY - 1; gy <= p.gridY + 1; gy++)
                {
                    if (gx < 0 || gx >= gridSizeX || gy < 0 || gy >= gridSizeY) continue;

                    foreach (var n in grid[gx, gy])
                    {
                        float dist = Vector2.Distance(p.pos, n.pos);
                        if (dist >= r) continue;

                        float normalDistance = 1f - dist / r;
                        p.density += normalDistance * normalDistance;
                        p.densityNear += normalDistance * normalDistance * normalDistance;
                        n.density += normalDistance * normalDistance;
                        n.densityNear += normalDistance * normalDistance * normalDistance;

                        p.neighbours.Add(n);
                    }
                }
            }
        }
    }

    void ApplyPressureForce()
    {
        float r = config.neighborRadius;

        foreach (var p in activeParticles)
        {
            Vector2 pressureForce = Vector2.zero;

            foreach (var n in p.neighbours)
            {
                Vector2 toNeighbour = n.pos - p.pos;
                float distance = toNeighbour.magnitude;
                if (distance <= 0.0001f) continue;

                float normalDistance = 1f - distance / r;
                float totalPressure =
                    (p.pressure + n.pressure) * normalDistance * normalDistance +
                    (p.pressureNear + n.pressureNear) * normalDistance * normalDistance * normalDistance;

                Vector2 pressureVector = totalPressure * (toNeighbour / distance);
                n.force += pressureVector;
                pressureForce += pressureVector;
            }

            p.force -= pressureForce;
        }
    }

    void ApplyViscosity()
    {
        float r = config.neighborRadius;
        float sigma = config.viscosity;

        foreach (var p in activeParticles)
        {
            foreach (var n in p.neighbours)
            {
                Vector2 toNeighbour = n.pos - p.pos;
                float distance = toNeighbour.magnitude;
                if (distance <= 0.0001f) continue;

                Vector2 normalDir = toNeighbour / distance;
                float relativeDistance = distance / r;
                float velocityDifference = Vector2.Dot(p.velocity - n.velocity, normalDir);

                if (velocityDifference > 0f)
                {
                    Vector2 viscosityForce = (1f - relativeDistance) * velocityDifference * sigma * normalDir;
                    p.velocity -= viscosityForce * 0.5f;
                    n.velocity += viscosityForce * 0.5f;
                }
            }
        }
    }
}
