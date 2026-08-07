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

    [Tooltip("파티클 사이 월드 간격. 액체 알갱이의 굵기를 정하는 기준값 — 줄이면 더 곱고 촘촘한 액체가 되지만 " +
             "같은 부피를 채우는 데 파티클이 더 많이 들어간다(제곱으로 늘어나니 성능 주의). " +
             "이웃 반경과 렌더링 블롭 크기가 모두 이 값에서 유도되므로 이것만 조절하면 된다.")]
    public float spacing = 0.16f;

    [Tooltip("압력 계수(K). 액체가 중력에 눌리는 정도를 좌우한다 — 작으면 파티클을 아무리 늘려도 바닥에 " +
             "얇게 깔리고, 크면 부피를 유지한다. 참고 레포보다 훨씬 큰 값을 쓰는데, 저쪽은 타임스텝이 " +
             "우리(1/120초 고정)의 40배라 낮은 강성밖에 못 썼기 때문이다. 값을 키우면 액체가 덜 눌린다.")]
    public float pressureStiffness = 200f;

    [Tooltip("근접 압력 계수(K_NEAR). 파티클끼리 완전히 겹쳐 뭉치는 것을 막는다.")]
    public float nearPressureStiffness = 40f;

    [Tooltip("기준 밀도의 초기값. 실제로는 SphSimulation.CalibrateRestDensity()가 스폰 직후 실측값으로 " +
             "덮어쓰므로 보통 건드릴 필요 없다.")]
    public float restDensity = 3f;

    [Tooltip("이웃 반경을 spacing의 몇 배로 잡을지. 1.25면 격자에서 상하좌우 4개만 이웃이 되고, " +
             "1.5를 넘기면 대각선까지 들어와 액체가 더 끈끈해진다. 보통 건드릴 필요 없다.")]
    public float neighborRadiusScale = 1.25f;

    /// <summary>이웃 반경(R). 이 거리 안의 파티클만 서로 영향을 준다.
    /// spacing에서 유도해야 spacing을 바꿔도 이웃 관계가 깨지지 않는다(따로 두면 세로로 이웃이 끊겨
    /// 액체가 바닥으로 짓눌리는 문제가 재발한다).</summary>
    public float NeighborRadius => spacing * neighborRadiusScale;

    [Tooltip("점성 계수(SIGMA). 클수록 액체가 끈적하게 뭉쳐 움직인다.")]
    public float viscosity = 0.2f;

    [Range(0f, 1f)]
    [Tooltip("매 스텝 속도를 이웃 평균 쪽으로 당기는 비율. 정지 상태에서 액체가 부르르 떨면 올리고, " +
             "너무 끈적하게 굳어 보이면 낮춘다. 0이면 평활화를 끈다.")]
    public float velocitySmoothing = 0.25f;

    [Tooltip("최대 속도. 불안정한 폭발적 힘을 방지하기 위한 클램프. 벽 터널링 방지를 위해 낮게 잡는다.")]
    public float maxVelocity = 4.5f;

}
