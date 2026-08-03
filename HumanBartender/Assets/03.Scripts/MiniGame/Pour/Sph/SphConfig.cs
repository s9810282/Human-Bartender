using System;
using UnityEngine;

/// <summary>
/// SPH(Smoothed Particle Hydrodynamics) 시뮬레이션 튜닝값.
/// AlexandreSajus/Unity-Fluid-Simulation의 Config.cs(정적 필드)를 인스펙터에서 조절 가능한
/// 값 묶음으로 옮긴 것 — 여러 PourManager 인스턴스가 각자 다른 값을 가질 수 있게 static을 없앴다.
/// </summary>
[Serializable]
public class SphConfig
{
    [Tooltip("중력 가속도.")]
    public float gravity = 9f;

    [Tooltip("파티클 사이 월드 간격. 넓힐수록 같은 파티클 수로 더 넓은 부피를 차지한다(=액체가 많아 보인다). " +
             "바꾸면 neighborRadius도 1.25배 관계를 유지하도록 같이 조정해야 한다.")]
    public float spacing = 0.16f;

    [Tooltip("압력 계수(K). 밀도가 restDensity를 넘으면 이 값에 비례해 밀어낸다. " +
             "gravity 대비 너무 작으면 액체가 중력에 짓눌려, 파티클을 늘려도 부피가 안 늘어난다(참고 레포 비율 gravity/K ≈ 62).")]
    public float pressureStiffness = 0.14f;

    [Tooltip("근접 압력 계수(K_NEAR). 파티클끼리 완전히 겹치는 것을 막는다. 관례상 K의 10배.")]
    public float nearPressureStiffness = 1.4f;

    [Tooltip("기준 밀도. 로컬 밀도가 이보다 높으면 압력이 파티클을 밀어낸다.")]
    public float restDensity = 3f;

    [Tooltip("이웃 반경(R). 이 거리 안의 파티클만 서로 영향을 준다. spacing의 약 1.25배를 유지할 것.")]
    public float neighborRadius = 0.2f;

    [Tooltip("점성 계수(SIGMA). 클수록 액체가 끈적하게 뭉쳐 움직인다.")]
    public float viscosity = 0.2f;

    [Tooltip("최대 속도. 불안정한 폭발적 힘을 방지하기 위한 클램프. 벽 터널링 방지를 위해 낮게 잡는다.")]
    public float maxVelocity = 4.5f;

    [Tooltip("벽 충돌 시 반사되는 속도 비율(감쇠).")]
    public float wallDamp = 0.35f;
}
