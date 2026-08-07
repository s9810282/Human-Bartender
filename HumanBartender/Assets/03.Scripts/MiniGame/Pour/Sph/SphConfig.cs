using System;
using UnityEngine;

/// <summary>
/// PBF(Position Based Fluids) 액체 설정.
///
/// 힘으로 밀도를 맞추던 이전 방식은 압력을 올리면 파티클이 서로 밀어내 줄기가 앙상해지고,
/// 낮추면 중력에 짓눌려 바닥에 깔리는 딜레마가 있었다. PBF는 밀도를 '제약'으로 직접 풀기 때문에
/// 중력 크기와 무관하게 부피가 유지되고, 뭉치는 성질(cohesion)은 별도 항이라 따로 조절할 수 있다.
///
/// 자주 만지는 값은 [액체 물성] 넷이고, [솔버]는 한 번 맞추면 보통 건드릴 일이 없다.
/// </summary>
[Serializable]
public class SphConfig
{
    [Header("액체 물성")]

    [Range(0f, 1f)]
    [Tooltip("점도. 이웃과 속도를 얼마나 맞출지(XSPH). 클수록 끈적하게 한 덩어리로 움직이고, " +
             "작으면 자유롭게 찰랑거린다.")]
    public float viscosity = 0.12f;

    [Range(0f, 1f)]
    [Tooltip("응집력. 파티클이 벌어졌을 때(밀도가 기준보다 낮을 때) 얼마나 다시 끌어당길지.\n" +
             "0이면 압축만 막고 당기지는 않아서 액체가 낱알로 흩어져 보인다. 올리면 한 덩어리로 뭉치고 " +
             "줄기도 이어진다. 너무 높이면 뭉쳐서 덩어리로 굳는다.")]
    public float cohesion = 0.25f;

    [Range(0f, 0.05f)]
    [Tooltip("뭉침 방지(PBF 인공 압력). 파티클 몇 개가 한 점에 붙어 굳는 현상을 막는 '반발' 힘이다.\n" +
             "이름과 달리 응집력이 아니다 — 올리면 파티클이 서로 밀어내 오히려 낱알로 흩어진다. " +
             "보통 0으로 두고, 파티클이 뭉쳐 굳는 게 보일 때만 아주 조금 올린다.")]
    public float antiClustering = 0f;

    [Tooltip("중력 가속도. PBF는 밀도를 제약으로 풀기 때문에 이 값을 키워도 액체가 짓눌리지 않는다.")]
    public float gravity = 9f;

    [Tooltip("파티클 사이 기준 간격. 액체 알갱이의 굵기를 정한다 — 줄이면 곱고 촘촘해지지만 " +
             "같은 부피에 파티클이 제곱으로 늘어난다.\n" +
             "이웃 반경과 렌더 블롭 크기가 이 값에서 유도되고 파티클 수도 '용량 x Fill Ratio'로 " +
             "역산되므로, 줄여도 액체 총량은 그대로다.")]
    public float spacing = 0.072f;

    [Header("솔버")]

    [Tooltip("밀도 제약을 몇 번 반복해 풀지. 많을수록 비압축성이 정확해져 액체가 덜 눌리지만 그만큼 무겁다. " +
             "3~5면 충분하다.")]
    [Range(1, 8)]
    public int solverIterations = 4;

    [Tooltip("제약 완화 계수(CFM). 이 값이 작으면 위치 보정량이 폭주해 파티클이 서로 튕겨나가고, " +
             "너무 크면 제약이 물러져 액체가 눌린다.\n" +
             "커널 스케일(이웃 반경)에 민감한 값이라 spacing을 크게 바꾸면 같이 조정해야 할 수 있다 — " +
             "다만 아래 maxCorrectionRatio가 상한을 걸어주므로 터지지는 않는다.")]
    public float relaxation = 0.1f;

    [Range(0.02f, 1f)]
    [Tooltip("한 번의 솔버 반복에서 파티클이 움직일 수 있는 최대 거리(spacing 대비 비율).\n" +
             "PBF의 위치 보정은 커널 스케일에 따라 값이 크게 달라져서, 설정이 어긋나면 한 번에 " +
             "간격의 몇 배씩 튕겨나간다. 이 상한이 그 폭주를 잘라내는 안전장치다.")]
    public float maxCorrectionRatio = 0.25f;

    [Tooltip("이웃 반경을 spacing의 몇 배로 잡을지. 이 반경 밖 파티클끼리는 아무 영향도 주고받지 못한다. " +
             "PBF는 이웃이 충분해야 밀도가 제대로 계산되므로 1.25보다는 넉넉히 잡는 편이 좋다.")]
    public float neighborRadiusScale = 2f;

    [Tooltip("한 스텝에 파티클이 움직일 수 있는 최대 속도. 터널링과 폭주를 막는 안전장치.")]
    public float maxVelocity = 6f;

    /// <summary>이웃 반경(H). spacing에서 유도해야 spacing을 바꿔도 이웃 관계가 깨지지 않는다.</summary>
    public float NeighborRadius => spacing * neighborRadiusScale;
}
