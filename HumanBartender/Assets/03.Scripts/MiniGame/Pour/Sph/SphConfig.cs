using System;
using UnityEngine;

/// <summary>
/// SPH(Smoothed Particle Hydrodynamics) 액체의 물성 설정.
/// AlexandreSajus/Unity-Fluid-Simulation의 Config.cs(정적 필드)를 인스펙터에서 조절 가능한
/// 값 묶음으로 옮긴 것 — 술 종류마다 다른 물성을 주려면 이 묶음만 갈아끼우면 된다.
///
/// 자주 만지는 값은 [액체 물성] 쪽 넷(점도, 밀도, 압력, 흔들림 억제)이고,
/// [시뮬레이션] 쪽은 한 번 맞춰두면 보통 건드릴 일이 없다.
/// </summary>
[Serializable]
public class SphConfig
{
    [Header("액체 물성")]

    [Tooltip("점도. 클수록 끈적하게 뭉쳐 움직이고(리큐어/시럽), 작을수록 찰랑거린다(물/보드카).")]
    public float viscosity = 0.2f;

    [Range(0.5f, 2f)]
    [Tooltip("밀도(응집력). 스폰 격자에서 실측한 밀도에 이 비율을 곱한 값을 기준 밀도로 삼는다.\n" +
             "1보다 크면 실제 밀도가 기준에 못 미쳐 압력이 '끌어당기는' 방향이 된다 — 표면장력처럼 " +
             "작용해서 줄기가 하나로 뭉쳐 흐른다(액체다운 모양의 핵심).\n" +
             "1보다 작으면 밀어내는 방향이라 파티클이 서로 거리를 두고, 줄기가 앙상하게 갈라져 보인다.")]
    public float restDensityScale = 1.3f;

    [Tooltip("중거리 압력 계수. restDensityScale이 1보다 크면 이 계수만큼 '끌어당기는' 힘이 되어 " +
             "줄기를 하나로 뭉치게 한다(표면장력 역할). 키우면 더 끈끈하게 뭉치고, 0에 가까우면 " +
             "각자 흩어져 앙상해진다.\n" +
             "액체가 바닥으로 눌려 부피가 안 나오는 건 이 값이 아니라 nearPressureStiffness로 잡는다.")]
    public float pressureStiffness = 0.14f;

    [Range(0f, 1f)]
    [Tooltip("흔들림 억제. 매 스텝 속도를 이웃 평균 쪽으로 당기는 비율. 정지 상태에서 액체가 부르르 떨면 " +
             "올리고, 너무 끈적하게 굳어 보이면 낮춘다. 0이면 끈다.\n" +
             "주의: 이웃이 있어야만 작동한다. 흩어져 날아가는 파티클은 서로 neighborRadius 밖이라 " +
             "이 값을 아무리 올려도 효과가 없다.")]
    public float velocitySmoothing = 0.1f;

    [Header("시뮬레이션")]

    [Tooltip("중력 가속도. 키우면 빠르게 쏟아진다.")]
    public float gravity = 9f;

    [Tooltip("파티클 사이 월드 간격. 액체 알갱이의 굵기를 정하는 기준값 — 줄이면 더 곱고 촘촘한 액체가 되지만 " +
             "같은 부피를 채우는 데 파티클이 더 많이 들어간다(제곱으로 늘어나니 성능 주의). " +
             "이웃 반경과 렌더링 블롭 크기가 모두 이 값에서 유도되므로 이것만 조절하면 된다.")]
    public float spacing = 0.16f;

    [Tooltip("근거리 반발 계수. 파티클이 서로 겹칠 만큼 가까워질 때만 세게 밀어낸다 — 액체가 중력에 " +
             "짓눌려 바닥에 납작하게 깔리는 걸 막는 건 바로 이 값이다(중거리 압력이 아니다).\n" +
             "병을 가득 채웠는데 바닥에 얇게 깔린다면 이 값을 올릴 것. 다만 너무 올리면 병목처럼 좁은 " +
             "곳에서 힘이 튀므로 maxAcceleration과 같이 봐야 한다.")]
    public float nearPressureStiffness = 1.4f;

    [Tooltip("이웃 반경을 spacing의 몇 배로 잡을지. 이 반경 밖으로 벌어진 파티클은 서로에게 아무 영향도 " +
             "주지 못한다(점도/응집/흔들림 억제 전부 무효). 올리면 벌어져도 서로 붙잡지만 이웃 수가 늘어 " +
             "무거워지고, pressureStiffness도 같이 낮춰야 액체가 부풀지 않는다.")]
    public float neighborRadiusScale = 1.25f;

    [Tooltip("최대 속도. 불안정한 폭발적 힘을 방지하기 위한 클램프.")]
    public float maxVelocity = 4.5f;

    [Tooltip("파티클 하나가 한 스텝에 받을 수 있는 최대 가속도(중력 대비 몇 배까지 허용할지의 절대값).\n" +
             "이 압력 모델은 조금만 눌려도 힘이 급격히 커져서, 병목처럼 좁아지는 곳에 고압이 모였다가 " +
             "구속이 풀리는 순간 액체가 사방으로 분사되듯 터진다. 상한을 걸면 줄기가 뭉쳐서 나온다.\n" +
             "낮출수록 얌전하지만 gravity보다는 확실히 커야 액체가 제 부피를 버틴다.\n" +
             "압력 계수가 낮으면 애초에 힘이 폭발하지 않으므로 사실상 안전장치로만 남는다.")]
    public float maxAcceleration = 100f;

    /// <summary>이웃 반경(R). 이 거리 안의 파티클만 서로 영향을 준다.
    /// spacing에서 유도해야 spacing을 바꿔도 이웃 관계가 깨지지 않는다(따로 두면 세로로 이웃이 끊겨
    /// 액체가 바닥으로 짓눌리는 문제가 재발한다).</summary>
    public float NeighborRadius => spacing * neighborRadiusScale;
}
