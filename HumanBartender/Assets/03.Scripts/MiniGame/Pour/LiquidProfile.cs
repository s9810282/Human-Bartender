using UnityEngine;

/// <summary>
/// 액체 한 종류의 물성과 질감을 한 묶음으로 담는다. 술마다 이 에셋만 갈아끼우면
/// 물처럼 찰랑이는 보드카부터 끈적하게 늘어지는 시럽까지 다르게 표현할 수 있다.
///
/// 물리(SphConfig)와 렌더(블롭 크기/threshold)를 굳이 한 곳에 둔 이유는 둘이 짝이기 때문이다 —
/// 파티클 간격을 바꾸면 블롭도 같이 맞춰야 액체로 보인다. 따로 두면 한쪽만 바꿨을 때
/// 액체가 낱알로 흩어지거나 뭉툭해진다.
///
/// 다만 그 짝(spacing / blobRadiusScale / threshold / edgeSmoothness)은 프로파일끼리 **같은 값으로
/// 맞춰 둔다**. 알갱이 굵기는 질감이 아니라 해상도 값이라서, 술마다 다르면 술을 바꿀 때마다 액체가
/// 얼마나 곱게 보이는지도 같이 흔들린다. spacing에는 병목 통로 폭(PourManager.neckWidthInParticles),
/// 파티클 수, 목표 개수가 전부 물려 있어서 성능과 난이도까지 술 종류를 따라 움직인다.
/// 술의 개성은 물성(viscosity/cohesion)과 흐를 때의 질감(maxStretch/streamThinning)으로만 낸다.
/// 기준값은 PourSceneSetup.SetGrain()에 있다.
/// </summary>
[CreateAssetMenu(fileName = "LiquidProfile", menuName = "Data/Liquid Profile")]
public class LiquidProfile : ScriptableObject
{
    [Header("물성")]
    public SphConfig physics = new SphConfig();

    [Header("질감 (렌더)")]
    [Tooltip("블롭 반지름을 파티클 간격의 몇 배로 잡을지. 키우면 매끈하고 두툼해지지만 뭉툭해진다.\n" +
             "threshold와 짝이다 — 인접 파티클 사이 필드값 2*(1 - 0.5/scale)^3 이 threshold를 넘어야 " +
             "액체 덩어리로 보이고, 못 넘으면 낱알로 흩어진다.")]
    public float blobRadiusScale = 1.7f;

    [Tooltip("필드가 이 값을 넘는 영역이 액체로 그려진다. 낮추면 액체가 부풀어 잘 뭉치고, " +
             "높이면 표면이 조여들어 가볍고 산뜻해 보인다.")]
    public float threshold = 0.35f;

    [Tooltip("가장자리 부드러움. 0에 가까우면 또렷한 실루엣, 크면 흐릿하게 번진다.")]
    public float edgeSmoothness = 0.12f;

    [Header("흐를 때")]
    [Tooltip("최고 속도에서 블롭을 진행 방향으로 몇 배 늘릴지. '알갱이가 뚝뚝 떨어지는' 느낌과 " +
             "'주르륵 흐르는' 느낌을 가르는 가장 중요한 값이다.\n" +
             "낙하하며 가속되면 파티클 간격도 속도에 비례해 벌어지므로, 늘임도 그 속도 배율만큼은 " +
             "따라가야 줄기가 이어져 보인다(최고 속도가 나가는 속도의 4배면 3~4 정도).\n" +
             "너무 키우면 액체가 실처럼 길게 번진다.")]
    public float maxStretch = 3.5f;

    [Range(0f, 0.95f)]
    [Tooltip("최고 속도에서 줄기를 몇 % 가늘게 만들지. 실제 액체도 가속될수록 단면이 좁아진다.\n" +
             "진행 방향 길이는 건드리지 않고 두께만 줄이므로, 올려도 줄기가 끊어지지 않는다 " +
             "(이어짐은 maxStretch가 담당한다). 흐를 때만 적용되고 고인 액체는 그대로다.")]
    public float streamThinning = 0.35f;

    [Header("색")]
    [Tooltip("켜면 아래 색을 쓰고, 끄면 칵테일 키워드에서 뽑은 색(CategoryColorData)을 쓴다.")]
    public bool overrideColor = false;
    public Color color = new Color(1f, 0.82f, 0.4f, 1f);
}
