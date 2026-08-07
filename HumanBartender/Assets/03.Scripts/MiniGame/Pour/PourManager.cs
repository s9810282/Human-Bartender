using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 따르기(Pour) 미니게임의 메인 매니저. IMiniGameController를 구현한다.
/// 병 내부에 SPH 파티클을 채워두고, 병이 기울어지면(BottleTiltController) 병과 함께 회전하는
/// 내부 형태의 입구가 중력 반대편을 향하게 되면서 파티클이 물리적으로 흘러나온다 —
/// 유량을 손으로 계산하지 않는다. 다만 "몇 도부터 나오는지"까지 물리에 맡기면 액체가 줄면서
/// 그 각도가 계속 올라가 조작감이 잡히지 않아서, 배출 시작만 각도로 여닫는다(아래 Pour Gate).
/// 목표/오차 판정은 잔 영역(AABB) 안에 들어온 파티클 개수로 한다.
/// Shake/Stur와 동일하게 actionFailCount/limitFailCount에 결과를 담아 CocktailCraftManager.Evaluate()의
/// 기존 백분율 판정 로직을 그대로 재사용한다.
/// </summary>
public class PourManager : MonoBehaviour, IMiniGameController
{
    [SerializeField] bool isTest = false;

    [Header("Data")]
    [SerializeField] CraftStationData data;
    [SerializeField] CategoryColorData colorData;
    [SerializeField] CocktailDataSO cocktailDataSO;

    [Header("Bottle Refs")]
    [SerializeField] BottleTiltController bottle;
    [Tooltip("병 외곽선을 그리는 컴포넌트. 아래 병 모양 값을 그대로 받아 그리므로 " +
             "보이는 모양과 액체가 갇히는 형태가 항상 일치한다.")]
    [SerializeField] BottleSilhouette bottleSilhouette;

    // ── 병 모양 ─────────────────────────────────────────────────────────
    // 아래 세 값이 액체가 갇히는 형태이자 그려지는 병 모양이다. 전부 병 로컬 좌표(스케일 적용 전)이며,
    // 스프라이트가 1유닛 정사각형이라 0.5가 최대치다. 실제 크기는 Bottle 오브젝트의 Scale로 조절한다.
    [Header("Bottle Shape")]
    [Tooltip("병 몸통 내부의 절반 크기. x=반너비, y=반높이.")]
    [SerializeField] Vector2 bottleInteriorHalfExtents = new Vector2(0.4f, 0.4f);
    [Range(2f, 5f)]
    [Tooltip("병목 통로의 폭을 파티클 몇 개로 잡을지. 어깨부터 입구까지 이 폭이 그대로 유지된다.\n" +
             "  2~3개: 한 줄기로 (권장)\n" +
             "  4개 이상: 여러 줄로 나란히 빠져나와 줄기가 갈라져 보인다\n" +
             "길이가 아니라 개수인 이유는 이 값이 프로파일의 spacing과 짝이기 때문이다 — 통로는 " +
             "좁은 구간이 목 길이만큼 이어지므로, 파티클 2개 폭이 안 되면 그 안에서 액체가 한 줄로 " +
             "늘어선다. 이웃이 위아래 둘뿐이면 PBF가 비압축성으로 벌어짐을 막지 못해 낙하 가속에 " +
             "그대로 벌어지고, 액체가 아니라 알갱이로 보인다.\n" +
             "월드 폭으로 적어두면 spacing이 다른 프로파일로 갈아끼울 때마다 이 관계가 소리 없이 " +
             "깨진다(같은 0.01이 Light에선 0.6개, Syrup에선 0.3개 폭이 된다).\n" +
             "줄기를 가늘게 '보이게' 하려면 이 값이 아니라 프로파일의 streamThinning을 올릴 것. " +
             "통로를 실제로 가늘게 하려면 spacing을 줄여야 하고, 그만큼 파티클이 늘어난다.")]
    [SerializeField] float neckWidthInParticles = 2.5f;
    [Range(0f, 1f)]
    [Tooltip("어깨가 시작되는 높이(몸통의 끝). 0=바닥, 1=입구. 이 아래는 몸통 너비 그대로다.\n" +
             "낮출수록 목이 길어져(=액체가 더 멀리 올라가야 해서) 나오기 시작하는 각도가 올라가는 " +
             "대신, 액체를 담을 몸통이 줄어든다.")]
    [SerializeField] float neckStartHeight01 = 0.6f;
    [Range(0f, 1f)]
    [Tooltip("어깨가 끝나고 통로가 시작되는 높이. neckStartHeight01 ~ 이 값 사이에서 몸통 너비가 " +
             "통로 너비까지 사선으로 좁아지고, 그 위로는 입구까지 같은 폭이 유지된다.\n" +
             "둘을 붙여두면 어깨가 90도 단차가 돼 액체가 그 밑에 갇히고, 벌려두면 완만한 깔때기가 돼 " +
             "액체가 통로로 모여든다. 이 값이 neckStartHeight01보다 작으면 어깨 없이 단차가 된다.")]
    [SerializeField] float neckChannelHeight01 = 0.75f;

    [Header("Glass")]
    [Tooltip("잔 스프라이트 자신의 Transform. InverseTransformPoint로 로컬 판정 영역을 계산한다.")]
    [SerializeField] Transform glassCenter;
    [Tooltip("잔에 담긴 것으로 칠 판정 영역(스케일 적용 전 잔 로컬 좌표). 스폰이 아니라 판정용이라 벽 여유는 필요 없다.")]
    [SerializeField] Vector2 glassInteriorHalfExtents = new Vector2(0.45f, 0.45f);

    [Header("Liquid (SPH)")]
    [SerializeField] SphLiquidRenderer liquidRenderer;
    [Tooltip("액체의 물성과 질감 묶음. 술마다 다른 프로파일을 꽂으면 물처럼 찰랑이는 것부터 " +
             "시럽처럼 늘어지는 것까지 다르게 표현된다.")]
    [SerializeField] LiquidProfile liquidProfile;
    [Range(0f, 1f)]
    [Tooltip("병 몸통을 얼마나 채울지(0=빔, 1=몸통 가득). 필요한 파티클 수는 몸통 격자 용량에서 자동으로 " +
             "계산하므로, 프로파일의 spacing을 바꿔도 보이는 액체량은 그대로 유지된다.")]
    [SerializeField] float initialFillRatio = 1f;
    [Tooltip("성능 상한. 위 비율로 계산한 수가 이걸 넘으면 여기까지만 만든다." +
             "렌더러는 파티클을 각자 크기만 한 사각형으로만 그리므로 수가 늘어도 거의 무겁지 않다 — " +
             "이제 한계는 렌더가 아니라 SPH 계산 쪽이다. 프레임이 떨어지면 이 값을 낮추거나 spacing을 키울 것.")]
    [SerializeField] int maxParticleCount = 1500;
    [Tooltip("SPH 공간분할그리드가 병/잔 위치 기준 사방으로 확보하는 여유 폭.\n" +
             "병은 바닥을 축으로 돌기 때문에 기울이면 몸체가 처음 위치에서 꽤 멀리 이동한다 — 그 범위와 " +
             "낙하 구간까지 덮을 만큼 잡아야 이웃 탐색이 느려지지 않는다.")]
    [SerializeField] float gridPadding = 5f;

    [Header("UI")]
    [SerializeField] Canvas buttonCanvas;
    [SerializeField] GradientRatioController gageBar;

    [Header("Pour Settings")]
    [Range(0f, 1f)]
    [Tooltip("isTest일 때 목표량. 병에 든 액체 중 몇 %를 잔에 따라야 하는지의 비율이다. " +
             "개수가 아니라 비율이라 spacing이나 병 크기를 바꿔도 난이도가 그대로 유지된다.\n" +
             "실제 모드에서는 recipe 기반 목표량과 연결 예정(오케스트레이터 작업 시).")]
    [SerializeField] float testTargetFillRatio = 0.45f;
    [Range(0f, 0.5f)]
    [Tooltip("허용 오차 비율. target+tolerance를 넘으면 즉시 오버플로우로 판정이 끝난다.")]
    [SerializeField] float testToleranceRatio = 0.15f;

    // ── 배출 게이트 ──────────────────────────────────────────────────────
    // 원래 액체가 나오기 시작하는 각도는 순전히 기하학이 정한다 — 수평인 수면이 병목 아랫입술보다
    // 높아지는 순간이다. 그런데 그 각도는 따르는 동안 액체가 줄면서 계속 올라가기 때문에
    // "60도부터 나온다" 같은 확정적인 조작감을 만들 수 없다. 그래서 입구를 각도로 직접 여닫는다.
    //
    // 게이트는 임계각을 '올리는' 방향으로만 작동한다. 기하학상 아직 수면이 입술에 못 닿는 각도라면
    // 게이트를 열어둬도 나올 것이 없다.
    [Header("Pour Gate")]
    [Tooltip("이 각도 미만에서는 입구를 아예 막아 한 방울도 나오지 않는다.")]
    [SerializeField] float pourStartAngle = 100f;
    [Tooltip("입구가 원래 폭(neckHalfWidth)까지 완전히 열리는 각도. pourStartAngle과 벌려둘수록 " +
             "졸졸 → 콸콸로 부드럽게 이어지고, 붙여두면 임계각에서 왈칵 쏟아진다.")]
    [SerializeField] float pourFullOpenAngle = 108f;

    [Header("Craft Event")]
    [SerializeField] VoidEvent craftServe;
    [SerializeField] VoidEvent craftRetry;

    int targetParticleCount;
    int toleranceCount;
    int glassParticleCount;
    bool isPlay;
    bool isFinished;

    SphSimulation simulation;
    float simulationAccumulator;
    readonly List<SphParticle> particles = new List<SphParticle>();

    // 게이트 상태는 물리 스텝마다 한 번만 계산해 들고 있는다. 벽 보정은 파티클 x 솔버 반복마다
    // 불리므로(프레임당 수만 번) 거기서 lossyScale 같은 Transform 조회를 하면 그것만으로 무거워진다.
    float gateOpen01;
    float valveLocalY;
    float shoulderLocalY;
    float channelLocalY;

    Matrix4x4 bottleWorldToLocal;
    Matrix4x4 bottleLocalToWorld;
    Matrix4x4 glassWorldToLocal;
    Matrix4x4 glassLocalToWorld;
    Vector2 glassWorldCenter;
    float glassGateRadiusSqr;

    /// <summary>병목 통로의 반너비(병 로컬). 프로파일의 spacing에서 유도되므로 Start에서 한 번만 잡는다.</summary>
    float neckHalfWidth;

    /// <summary>파티클 간격. 프로파일의 물성값을 짧게 쓰기 위한 별칭.</summary>
    float Spacing => liquidProfile.physics.spacing;

    UniTaskCompletionSource tcs;

    void Start()
    {
        if (isTest)
            data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];

        isFinished = false;
        glassParticleCount = 0;

        Color liquidColor = GetLiquidColor();
        liquidRenderer.Init(liquidProfile, liquidColor);

        // 통로 폭은 프로파일이 정한다. 병 모양보다 먼저 잡아야 실루엣이 같은 폭으로 그려진다.
        float scaleX = Mathf.Max(Mathf.Abs(bottle.BottleVisual.lossyScale.x), 0.0001f);
        neckHalfWidth = Spacing * neckWidthInParticles * 0.5f / scaleX;

        if (bottleSilhouette != null)
            bottleSilhouette.Build(bottleInteriorHalfExtents, neckHalfWidth, ShoulderLocalY(), ChannelLocalY());

        simulation = CreateSimulation();
        simulation.ConstrainToWalls = ConstrainParticle;

        // 스폰/밀도 보정도 벽 보정을 타므로 게이트 상태가 먼저 서 있어야 한다.
        RefreshBottleGate();

        SpawnBottleParticles();

        // 스폰 격자의 실제 밀도를 기준으로 삼아야 압력이 밀어내는 방향으로 작동한다(액체가 눌리지 않음).
        simulation.CalibrateRestDensity();

        // 목표는 실제로 만들어진 파티클 수에서 환산한다. 개수를 직접 적어두면 spacing이나 병 크기를
        // 바꿀 때마다 전체 파티클 수가 달라져 난이도가 멋대로 흔들린다.
        targetParticleCount = Mathf.Max(1, Mathf.RoundToInt(particles.Count * testTargetFillRatio));
        toleranceCount = Mathf.Max(1, Mathf.RoundToInt(particles.Count * testToleranceRatio));

        if (isTest)
            data.targetCraft_tolerance = toleranceCount;

        gageBar.UpdateValues(targetParticleCount + toleranceCount, targetParticleCount - toleranceCount, toleranceCount * 2f, 0f);

        isPlay = true;
    }

    /// <summary>
    /// 공간 격자의 칸 크기를 이웃 반경에 맞춰 잡는다. 칸이 반경보다 훨씬 크면 한 칸에 파티클이 잔뜩
    /// 들어가 이웃 탐색이 느려진다(파티클이 수백 개로 늘어난 뒤로는 이게 체감된다).
    /// 칸 수에 상한을 두는 건 격자 자체가 메모리를 먹기 때문이다.
    /// </summary>
    SphSimulation CreateSimulation()
    {
        Vector2 min = ComputeGridMin();
        Vector2 max = ComputeGridMax();
        Vector2 range = max - min;

        float cellSize = Mathf.Max(liquidProfile.physics.NeighborRadius, 0.001f);

        int gridX = Mathf.Clamp(Mathf.CeilToInt(range.x / cellSize), 16, 96);
        int gridY = Mathf.Clamp(Mathf.CeilToInt(range.y / cellSize), 16, 96);

        return new SphSimulation(liquidProfile.physics, min, max, gridX, gridY);
    }

    Vector2 ComputeGridMin()
    {
        Vector2 bottlePos = bottle.BottleVisual.position;
        Vector2 glassPos = glassCenter.position;
        return Vector2.Min(bottlePos, glassPos) - Vector2.one * gridPadding;
    }

    Vector2 ComputeGridMax()
    {
        Vector2 bottlePos = bottle.BottleVisual.position;
        Vector2 glassPos = glassCenter.position;
        return Vector2.Max(bottlePos, glassPos) + Vector2.one * gridPadding;
    }

    void SpawnBottleParticles()
    {
        var particlesRoot = new GameObject("Sph Particles").transform;
        particlesRoot.SetParent(transform, false);

        // SPH는 전부 월드 좌표로 계산하는데 병은 축마다 다른 스케일(예: 1.4 x 3.6)을 갖는다. 로컬 간격을
        // 그대로 쓰면 TransformPoint 후 세로 간격만 크게 늘어나 이웃 반경 밖으로 벗어나고, 위아래 파티클이
        // 서로 압력을 주고받지 못해 중력에 바닥으로 다 뭉쳐버린다(파티클을 늘려도 액체량이 그대로 보이던 원인).
        // 그래서 스케일로 나눠 "월드에서 spacing 간격"이 되도록 로컬 간격을 잡는다.
        Vector3 scale = bottle.BottleVisual.lossyScale;
        float localSpacingX = Spacing / Mathf.Max(Mathf.Abs(scale.x), 0.0001f);
        float localSpacingY = Spacing / Mathf.Max(Mathf.Abs(scale.y), 0.0001f);

        int columns = Mathf.Max(1, Mathf.FloorToInt(bottleInteriorHalfExtents.x * 2f / localSpacingX));

        // 액체는 어깨 아래(몸통)까지만 채운다 — 좁아지는 병목 안에 몸통 너비로 격자를 깔면 파티클이
        // 벽 밖에서 시작해버린다. 실제 술병도 목까지 채우지는 않으니 모양상으로도 맞다.
        float bodyHeight = ShoulderLocalY() + bottleInteriorHalfExtents.y;
        int rowCapacity = Mathf.Max(1, Mathf.FloorToInt(bodyHeight / localSpacingY));
        int gridCapacity = columns * rowCapacity;

        // 필요한 개수를 "몸통을 몇 % 채울지"에서 역산한다. 이렇게 해야 spacing을 바꿔도 -- 칸이 작아진 만큼
        // 개수가 자동으로 늘어서 -- 보이는 액체량이 유지된다(개수를 직접 적으면 spacing을 줄일 때마다
        // 액체가 줄어든 것처럼 보인다).
        int spawnCount = Mathf.RoundToInt(gridCapacity * Mathf.Clamp01(initialFillRatio));
        int limit = Mathf.Max(1, maxParticleCount);

        if (spawnCount > limit)
        {
            Logger.LogWarning(
                $"[Pour] 몸통을 {initialFillRatio:P0} 채우려면 {spawnCount}개가 필요하지만 상한({limit}개)에 걸려 " +
                "그만큼만 만듭니다. 액체가 덜 차 보이면 프로파일의 spacing을 키우거나 maxParticleCount를 올리세요.");
            spawnCount = limit;
        }

        int spawned = 0;
        for (int row = 0; spawned < spawnCount; row++)
        {
            for (int col = 0; col < columns && spawned < spawnCount; col++)
            {
                Vector2 localPos = new Vector2(
                    (col - (columns - 1) * 0.5f) * localSpacingX,
                    -bottleInteriorHalfExtents.y + localSpacingY * 0.5f + row * localSpacingY);

                Vector3 worldPos = bottle.BottleVisual.TransformPoint(localPos);

                SphParticle particle = CreateParticle(particlesRoot, worldPos);
                particles.Add(particle);
                simulation.Register(particle);

                spawned++;
            }
        }
    }

    SphParticle CreateParticle(Transform parent, Vector3 worldPos)
    {
        var go = new GameObject("Particle");
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;

        var particle = go.AddComponent<SphParticle>();
        particle.Init(liquidProfile.physics);
        particle.Activate(worldPos);

        return particle;
    }

    void Update()
    {
        // 물리는 판정이 끝난 뒤에도 계속 돌린다. 여기서 멈춰버리면 공중에 있던 액체가 그대로 얼어붙어
        // 물리 버그처럼 보인다 — 판정만 멈추고 액체는 잔으로 마저 떨어지게 둔다.
        StepSimulation();
        liquidRenderer.UpdateBlobs(simulation.ActiveParticles);

        if (!isPlay) return;

        glassParticleCount = CountParticlesInGlass();

        if (glassParticleCount > targetParticleCount + toleranceCount)
            FinishPour();
    }

    /// <summary>
    /// SPH를 고정 시간 간격으로 돌린다. 오일러 적분 + 강한 압력 조합은 dt에 민감해서, Time.deltaTime을
    /// 그대로 넣으면 프레임이 튈 때 힘이 폭발하거나 기기마다 액체 거동이 달라진다. 한 프레임에 도는
    /// 스텝 수를 제한해 저사양에서 물리가 프레임을 더 잡아먹는 악순환도 막는다.
    /// </summary>
    void StepSimulation()
    {
        const float fixedStep = 1f / 120f;
        const int maxStepsPerFrame = 4;

        simulationAccumulator += Time.deltaTime;

        int steps = 0;
        while (simulationAccumulator >= fixedStep && steps < maxStepsPerFrame)
        {
            RefreshBottleGate();

            // 벽 클램프는 Tick 안에서 솔버 반복마다 불린다(ConstrainToWalls로 연결해둠).
            // 반복 밖에서 한 번만 걸면 파티클이 벽을 뚫은 채로 밀도가 계산돼 액체가 새어나간다.
            simulation.Tick(fixedStep);

            simulationAccumulator -= fixedStep;
            steps++;
        }

        // 한 프레임에 다 따라잡지 못했으면 밀린 시간은 버린다(따라잡으려다 더 느려지는 걸 방지).
        if (steps >= maxStepsPerFrame)
            simulationAccumulator = 0f;
    }

    /// <summary>
    /// 파티클을 병/잔 안쪽으로 가두는 유일한 벽 처리다(Unity 물리 콜라이더는 쓰지 않는다 —
    /// 둘을 같이 쓰면 서로 다른 위치로 밀어대 파티클이 떨렸다).
    ///
    /// PBF 솔버가 반복마다 예측 위치를 옮기므로 이 함수도 매 반복 불린다. 그래서 확정 위치(pos)가 아니라
    /// 예측 위치(predictedPos)를 보정한다 — 속도는 따로 건드리지 않아도 스텝 끝에서
    /// (예측위치 - 현재위치)/dt로 되뽑히므로 벽에 부딪힌 만큼 알아서 깎인다.
    ///
    /// 병은 회전하기 때문에 "로컬 좌표로 근처인지"를 매번 다시 판정하면, 이미 입구를 빠져나가 자유낙하
    /// 중인 파티클도 회전된 좌표계에서 벽에 부딪힌 것으로 오판해 붙잡힐 수 있다(허공에 멈춘 것처럼 보였다).
    /// 그래서 입구를 한 번 넘으면 exitedBottle로 기록해 다시는 병 기준으로 붙잡지 않는다.
    /// 잔은 회전하지 않아 그런 문제가 없다.
    /// </summary>
    void ConstrainParticle(SphParticle particle)
    {
        if (!particle.gameObject.activeSelf) return;

        if (!particle.exitedBottle)
            ConstrainToBottle(particle);

        ConstrainToGlass(particle);
    }

    /// <summary>
    /// 병 내부에 가둔다. 폭이 높이에 따라 연속으로 변하므로 그 높이의 반너비로 X만 자르면 된다.
    ///
    /// 어깨가 사선이라는 게 여기서 중요하다. 어깨에서 몸통 폭이 통로 폭으로 뚝 끊기면 그 단차 때문에
    /// 어깨를 갓 넘어선 파티클이 몸통 벽에서 통로 한가운데로 순간이동해, 액체가 어깨를 타고 빨려
    /// 올라간다. 그걸 피하려면 사각형 여러 개의 합집합으로 두고 매번 최근접 투영을 해야 했는데,
    /// 사선이 그 불연속을 없애주므로 그럴 필요가 없다.
    /// </summary>
    void ConstrainToBottle(SphParticle particle)
    {
        Vector3 local = bottleWorldToLocal.MultiplyPoint3x4(particle.predictedPos);

        float halfY = bottleInteriorHalfExtents.y;

        // 게이트가 열린 상태에서 입구(로컬 +Y)를 넘었으면 이제부터 자유낙하 — 다시는 이 파티클을
        // 병 기준으로 재해석하지 않는다.
        if (gateOpen01 > 0f && local.y > halfY)
        {
            particle.exitedBottle = true;
            return;
        }

        // 완전히 닫혀 있으면 밸브 입구가 천장이다. 밸브 구간까지 들여보내고 막으면 폭이 0으로
        // 수렴하는 곳에 갇혀 한 줄로 짜여 나오므로, 아예 밸브 아래에서 멈춰 세운다.
        float ceilingY = gateOpen01 > 0f ? float.PositiveInfinity : valveLocalY;

        float clampedY = Mathf.Clamp(local.y, -halfY, ceilingY);
        float halfWidth = BottleHalfWidthAt(clampedY);
        float clampedX = Mathf.Clamp(local.x, -halfWidth, halfWidth);

        if (Mathf.Approximately(clampedX, local.x) && Mathf.Approximately(clampedY, local.y))
            return;

        particle.predictedPos = bottleLocalToWorld.MultiplyPoint3x4(new Vector3(clampedX, clampedY, local.z));
    }

    /// <summary>
    /// 해당 높이에서 병 내부가 허용하는 반너비. 아래에서부터 몸통 → 어깨(사선) → 통로 → 밸브다.
    /// 구간이 바뀌는 지점마다 값이 이어지므로 폭이 튀는 곳이 없다.
    /// </summary>
    float BottleHalfWidthAt(float localY)
    {
        float shoulderY = shoulderLocalY;
        float channelY = channelLocalY;
        float halfY = bottleInteriorHalfExtents.y;

        // 몸통 — 폭 그대로.
        if (localY <= shoulderY) return bottleInteriorHalfExtents.x;

        // 어깨 — 몸통 폭에서 통로 폭까지 사선으로 좁아진다.
        if (localY <= channelY)
            return Mathf.Lerp(bottleInteriorHalfExtents.x, neckHalfWidth,
                              Mathf.InverseLerp(shoulderY, channelY, localY));

        // 통로 — 입구까지 같은 폭.
        if (localY <= valveLocalY) return neckHalfWidth;

        // 밸브 — 게이트가 덜 열렸으면 입구로 갈수록 조여든다. 여기도 사선인 이유는 같다. 단차로
        // 두면 밸브를 갓 넘어선 파티클이 통로 벽에서 입구 한가운데로 순간이동한다.
        return Mathf.Lerp(neckHalfWidth, neckHalfWidth * gateOpen01,
                          Mathf.InverseLerp(valveLocalY, halfY, localY));
    }

    /// <summary>
    /// 이번 물리 스텝에서 쓸 게이트 상태를 계산한다.
    ///
    /// 게이트가 조이는 건 목 전체가 아니라 입구 쪽 짧은 구간(밸브)뿐이다. 통로 전체를 좁히면 파티클
    /// 한 개 폭도 안 되는 구간이 목 길이만큼 길게 이어져서, 그 안에서 액체가 한 줄로 늘어선 채
    /// 자유낙하로 가속돼 알갱이로 끊어진다 — PBF는 이웃이 있어야 비압축성으로 그 벌어짐을 막는데,
    /// 한 줄로 서면 이웃이 위아래 둘뿐이라 사실상 액체가 아니라 낱개 낙하가 된다.
    /// </summary>
    void RefreshBottleGate()
    {
        gateOpen01 = NeckOpen01();

        shoulderLocalY = ShoulderLocalY();
        channelLocalY = ChannelLocalY();

        float channelLength = bottleInteriorHalfExtents.y - channelLocalY;

        // 밸브는 파티클 한 개 높이는 돼야 실제로 막힌다(그보다 얇으면 한 스텝에 뛰어넘는다).
        float oneParticle = Spacing / Mathf.Max(Mathf.Abs(bottle.BottleVisual.lossyScale.y), 0.0001f);
        float valveHeight = Mathf.Min(Mathf.Max(channelLength * 0.3f, oneParticle), channelLength);

        valveLocalY = bottleInteriorHalfExtents.y - valveHeight;

        // 벽 보정은 (파티클 x 솔버 반복)만큼 불린다. 거기서 Transform을 직접 쓰면 InverseTransformPoint /
        // lossyScale 같은 네이티브 호출이 프레임당 수만 번 나가므로, 행렬만 여기서 한 번 받아둔다.
        // 병은 물리 스텝 도중에는 움직이지 않으므로 스텝당 한 번이면 충분하다.
        Transform bottleVisual = bottle.BottleVisual;
        bottleWorldToLocal = bottleVisual.worldToLocalMatrix;
        bottleLocalToWorld = bottleVisual.localToWorldMatrix;

        glassWorldToLocal = glassCenter.worldToLocalMatrix;
        glassLocalToWorld = glassCenter.localToWorldMatrix;
        glassWorldCenter = glassCenter.position;

        const float gateBuffer = 0.1f;
        Vector2 glassWorldHalfExtents = Vector2.Scale(glassInteriorHalfExtents, glassCenter.lossyScale);
        float gateRadius = glassWorldHalfExtents.magnitude + gateBuffer;
        glassGateRadiusSqr = gateRadius * gateRadius;
    }

    /// <summary>
    /// 지금 기울기에서 밸브가 얼마나 열려 있는지(0=완전히 막힘, 1=neckHalfWidth 그대로).
    /// 여닫는 건 뚜껑이 아니라 통과 폭이다 — 폭이 좁아지면 파티클이 물리적으로 못 빠져나가므로,
    /// 열리는 순간 왈칵 쏟아지지 않고 실제 액체처럼 졸졸에서 시작한다.
    /// </summary>
    float NeckOpen01()
    {
        float fullOpen = Mathf.Max(pourFullOpenAngle, pourStartAngle + 0.01f);
        return Mathf.Clamp01(Mathf.InverseLerp(pourStartAngle, fullOpen, bottle.CurrentAngle));
    }

    /// <summary>어깨가 시작되는 로컬 Y. 이 아래는 몸통이다.</summary>
    float ShoulderLocalY() =>
        Mathf.Lerp(-bottleInteriorHalfExtents.y, bottleInteriorHalfExtents.y, neckStartHeight01);

    /// <summary>어깨가 끝나고 통로가 시작되는 로컬 Y. 거꾸로 적힌 값도 단차(어깨 없음)로 받아준다.</summary>
    float ChannelLocalY() =>
        Mathf.Max(ShoulderLocalY(),
                  Mathf.Lerp(-bottleInteriorHalfExtents.y, bottleInteriorHalfExtents.y, neckChannelHeight01));

    void ConstrainToGlass(SphParticle particle)
    {
        Vector2 halfExtents = glassInteriorHalfExtents;

        // 잔은 회전하지 않으므로, 멀리 있으면(=아직 병 근처거나 낙하 중) 그냥 건드리지 않는다.
        if ((particle.predictedPos - glassWorldCenter).sqrMagnitude > glassGateRadiusSqr)
            return;

        Vector3 local = glassWorldToLocal.MultiplyPoint3x4(particle.predictedPos);

        // 입구보다 위에 있으면 아직 잔 안이 아니다. 이때 좌우로 끌어당기면 잔 옆으로 빗나가야 할
        // 액체까지 잔 위로 빨려들어간다 — 벽 사이 높이에 들어온 뒤에만 좌우를 막는다.
        if (local.y > halfExtents.y) return;

        // 속도는 건드리지 않는다 — PBF가 스텝 끝에서 위치 변화로부터 다시 계산하므로 벽 반응이 저절로 나온다.
        float clampedX = Mathf.Clamp(local.x, -halfExtents.x, halfExtents.x);
        float clampedY = Mathf.Max(local.y, -halfExtents.y);

        if (Mathf.Approximately(clampedX, local.x) && Mathf.Approximately(clampedY, local.y))
            return;

        particle.predictedPos = glassLocalToWorld.MultiplyPoint3x4(new Vector3(clampedX, clampedY, local.z));
    }

    int CountParticlesInGlass()
    {
        int count = 0;

        foreach (var p in particles)
        {
            if (!p.gameObject.activeSelf) continue;

            Vector3 local = glassCenter.InverseTransformPoint(p.pos);
            if (Mathf.Abs(local.x) <= glassInteriorHalfExtents.x && Mathf.Abs(local.y) <= glassInteriorHalfExtents.y)
                count++;
        }

        return count;
    }

    /// <summary>플레이어가 스스로 따르기를 멈추고 결과를 확정할 때(별도 UI 버튼 등에서) 호출.</summary>
    public void FinishPour()
    {
        if (!isPlay || isFinished) return;

        isFinished = true;
        isPlay = false;

        CompleteMade();

        // 실제 게임 흐름에서는 CocktailCraftManager가 완성/서빙 컷씬을 재생한 뒤 OnNextButton()을 부른다.
        // 독립 테스트 씬에는 그 흐름이 없어서, 끝났다는 신호가 전혀 없으면 그냥 멈춘 것처럼 보인다.
        if (isTest)
        {
            Logger.Log($"[Pour] 판정 종료. 잔에 담긴 파티클 {glassParticleCount} / 목표 {targetParticleCount}(±{toleranceCount})");
            OnNextButton();
        }
    }

    Color GetLiquidColor()
    {
        if (data.targetCocktailData.Keywords == null || data.targetCocktailData.Keywords.Length == 0)
            return Color.white;

        int n = colorData.categorys.FindIndex(a => a.Contains(data.targetCocktailData.Keywords[0]));
        return n >= 0 ? colorData.colors[n] : Color.white;
    }

    public void InitGame(UniTaskCompletionSource tcs)
    {
        this.tcs = tcs;
        data.craftingResult.actionFailCount = 0;
    }

    public void CompleteMade()
    {
        int deviation = Mathf.Abs(glassParticleCount - targetParticleCount);

        data.craftingResult.isResult = true;
        data.craftingResult.actionFailCount = deviation;
        data.craftingResult.limitFailCount = toleranceCount;

        if (tcs != null)
        {
            tcs.TrySetResult();
        }
    }

    public void OnNextButton()
    {
        buttonCanvas.gameObject.SetActive(true);
    }

    public void Serve()
    {
        craftServe?.Raise(new Void());
    }

    public void Retry()
    {
        craftRetry?.Raise(new Void());
    }
}
