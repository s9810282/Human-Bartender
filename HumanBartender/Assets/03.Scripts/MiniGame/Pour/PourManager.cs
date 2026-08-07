using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 따르기(Pour) 미니게임의 메인 매니저. IMiniGameController를 구현한다.
/// 병 내부에 SPH 파티클을 채워두고, 병이 기울어지면(BottleTiltController) 병과 함께 회전하는
/// U자 컨테이너 콜라이더의 열린 쪽이 중력 반대편을 향하게 되면서 파티클이 물리적으로 흘러나온다 —
/// 유량을 손으로 계산하지 않는다. 목표/오차 판정은 잔 영역(AABB) 안에 들어온 파티클 개수로 한다.
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

    [Header("Bottle")]
    [SerializeField] BottleTiltController bottle;
    [Tooltip("병 외곽선을 그리는 컴포넌트. 아래 병 모양 값을 그대로 받아 그리므로 " +
             "보이는 모양과 액체가 갇히는 형태가 항상 일치한다. 비워두면 외곽선을 그리지 않는다.")]
    [SerializeField] BottleSilhouette bottleSilhouette;
    [Tooltip("병 몸통 내부 영역(스케일 적용 전 병 로컬 좌표). 파티클을 처음 채우는 범위이자, 액체가 새지 않게 " +
             "가두는 벽 위치이기도 하다(ConstrainParticles가 이 경계로 클램프한다).")]
    [SerializeField] Vector2 bottleInteriorHalfExtents = new Vector2(0.4f, 0.4f);

    // ── 병목 ────────────────────────────────────────────────────────────
    // 아래에서부터 몸통 → 어깨(사선) → 통로다. 어깨에서 폭이 뚝 끊기지 않고 사선으로 이어지는 게
    // 중요하다 — 단차로 두면 어깨를 갓 넘어선 파티클이 몸통 벽에서 통로 한가운데로 순간이동해
    // 액체가 어깨를 타고 빨려 올라간다.
    [Range(2f, 5f)]
    [Tooltip("병목 통로의 폭을 파티클 몇 개로 잡을지. 어깨부터 입구까지 이 폭이 그대로 유지된다.\n" +
             "  2~3개: 한 줄기로 (권장)\n" +
             "  4개 이상: 여러 줄로 나란히 빠져나와 줄기가 갈라져 보인다\n" +
             "길이가 아니라 개수인 이유는 이 값이 프로파일의 spacing과 짝이기 때문이다 — 통로는 좁은 " +
             "구간이 목 길이만큼 이어지므로, 파티클 2개 폭이 안 되면 그 안에서 액체가 한 줄로 늘어서 " +
             "알갱이처럼 끊어진다.")]
    [SerializeField] float neckWidthInParticles = 2.5f;
    [Range(0f, 1f)]
    [Tooltip("어깨가 시작되는 높이(몸통의 끝). 0=바닥, 1=입구. 이 아래는 몸통 너비 그대로다.\n" +
             "낮출수록 목이 길어지는 대신 액체를 담을 몸통이 줄어든다.")]
    [SerializeField] float neckStartHeight01 = 0.6f;
    [Range(0f, 1f)]
    [Tooltip("어깨가 끝나고 통로가 시작되는 높이. neckStartHeight01 ~ 이 값 사이에서 몸통 너비가 " +
             "통로 너비까지 사선으로 좁아지고, 그 위로는 입구까지 같은 폭이 유지된다.\n" +
             "둘을 붙여두면 어깨가 90도 단차가 되고, 벌려두면 완만한 깔때기가 돼 액체가 통로로 모여든다.")]
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
    [Tooltip("병이 완전히 가득 찼을 때 기준 파티클 수(용량). 실제로 시작 시 채워지는 양은 이 값에 initialFillRatio를 곱한 만큼이다. " +
             "렌더 한계(SphLiquidRenderer.MaxBlobs)와 병 내부 격자 용량 중 작은 쪽으로 자동 제한되며, 잘릴 때는 콘솔에 경고가 뜬다.")]
    [SerializeField] int bottleParticleCount = 126;
    [Range(0f, 10f)]
    [Tooltip("시작할 때 bottleParticleCount 중 몇 %를 실제로 채울지. 바닥부터 격자로 쌓으므로 값을 올리면 액체 높이가 그만큼 올라간다.")]
    [SerializeField] float initialFillRatio = 0.5f;
    [Tooltip("SPH 공간분할그리드가 병/잔 위치 기준 사방으로 확보하는 여유 폭. 병과 잔을 멀리 떨어뜨려 배치해도 " +
             "그 사이 낙하 구간이 그리드 밖으로 벗어나 밀도 계산이 깨지지(=허공에 멈춰 보이지) 않도록 넉넉히 잡는다.")]
    [SerializeField] float gridPadding = 3f;

    [Header("UI")]
    [SerializeField] Canvas buttonCanvas;
    [SerializeField] GradientRatioController gageBar;

    [Header("Pour Settings")]
    [Tooltip("isTest일 때 목표 파티클 개수로 사용. 실제 모드에서도 recipe 기반 목표 연결 전까지는 임시로 이 값을 쓴다.\n" +
        "target+tolerance를 넘으면 즉시 오버플로우로 판정이 끝나므로, 시작 파티클 수" +
        "(bottleParticleCount * initialFillRatio)에 너무 가깝지도 너무 낮지도 않게 잡아야 한다.")]
    [SerializeField] int testTargetParticleCount = 30;
    [SerializeField] int testToleranceCount = 10;

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

    /// <summary>병목 통로의 반너비(병 로컬). spacing에서 유도되므로 Start에서 한 번만 잡는다.</summary>
    float neckHalfWidth;

    /// <summary>프로파일의 물성값을 짧게 쓰기 위한 별칭.</summary>
    SphConfig Physics => liquidProfile.physics;
    float Spacing => liquidProfile.physics.spacing;

    UniTaskCompletionSource tcs;

    void Start()
    {
        if (isTest)
        {
            data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];
            data.targetCraft_tolerance = testToleranceCount;
        }

        targetParticleCount = testTargetParticleCount; // TODO: recipe 기반 목표량과 연결 (오케스트레이터 작업 시)
        toleranceCount = data.targetCraft_tolerance > 0 ? data.targetCraft_tolerance : testToleranceCount;

        isFinished = false;
        glassParticleCount = 0;

        Color liquidColor = GetLiquidColor();
        liquidRenderer.Init(liquidProfile, liquidColor);

        // 통로 폭은 파티클 간격이 정한다. 병 모양보다 먼저 잡아야 실루엣이 같은 폭으로 그려진다.
        float scaleX = Mathf.Max(Mathf.Abs(bottle.BottleVisual.lossyScale.x), 0.0001f);
        neckHalfWidth = Spacing * neckWidthInParticles * 0.5f / scaleX;

        if (bottleSilhouette != null)
            bottleSilhouette.Build(bottleInteriorHalfExtents, neckHalfWidth, ShoulderLocalY(), ChannelLocalY());

        simulation = new SphSimulation(Physics, ComputeGridMin(), ComputeGridMax());
        SpawnBottleParticles();

        // 스폰 격자의 실제 밀도를 기준으로 삼아야 압력이 밀어내는 방향으로 작동한다(액체가 눌리지 않음).
        simulation.CalibrateRestDensity();

        gageBar.UpdateValues(targetParticleCount + toleranceCount, targetParticleCount - toleranceCount, toleranceCount * 2f, 0f);

        isPlay = true;
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

        // 바닥부터 격자로 채우므로, 목표 개수만큼만 스폰하면 자연스럽게 그만큼의 높이까지만 차 보인다.
        int spawnCount = Mathf.Clamp(Mathf.RoundToInt(bottleParticleCount * initialFillRatio), 0, bottleParticleCount);

        // 액체는 어깨 아래(몸통)까지만 채운다 — 좁아지는 병목 안에 몸통 너비로 격자를 깔면 파티클이
        // 벽 밖에서 시작해버린다. 실제 술병도 목까지 채우지는 않으니 모양상으로도 맞다.
        // 화면에 그릴 수 있는 수(MetaballStream 셰이더의 MAX_BLOBS)도 넘지 않게 같이 막는다.
        float bodyHeight = ShoulderLocalY() + bottleInteriorHalfExtents.y;
        int rowCapacity = Mathf.Max(1, Mathf.FloorToInt(bodyHeight / localSpacingY));
        int capacity = Mathf.Min(columns * rowCapacity, SphLiquidRenderer.MaxBlobs);

        if (spawnCount > capacity)
        {
            Logger.LogWarning($"[Pour] 파티클 {spawnCount}개는 병 용량/렌더 한계를 넘어 {capacity}개로 줄입니다. " +
                              "더 늘리려면 프로파일의 spacing을 줄이거나 병 크기를 키우세요.");
            spawnCount = capacity;
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
        particle.Init(Physics);
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
            simulation.Tick(fixedStep);
            ConstrainParticles();

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
    /// 병은 회전하기 때문에 "로컬 좌표로 근처인지 판정"을 매 프레임 다시 하면, 이미 입구를 빠져나가
    /// 자유낙하 중인 파티클도 회전된 로컬 좌표계에서 우연히 벽처럼 보이는 방향으로 떨어지는 걸 계속
    /// "벽에 부딪힘"으로 오판해 속도를 깎아버릴 수 있다(그래서 허공에 멈춘 것처럼 보였다).
    /// 그래서 병은 "입구를 한 번이라도 넘었는지"를 SphParticle.exitedBottle에 기록해두고, 넘은 뒤로는
    /// 다시는 병 기준으로 안 붙잡는다. 잔은 회전하지 않아 이런 문제가 없으므로 매 프레임 재판정해도 안전하다.
    /// </summary>
    void ConstrainParticles()
    {
        foreach (var p in particles)
        {
            if (!p.gameObject.activeSelf) continue;

            if (!p.exitedBottle)
                ConstrainToBottle(p);

            ConstrainToGlass(p);
        }
    }

    void ConstrainToBottle(SphParticle particle)
    {
        Transform container = bottle.BottleVisual;
        Vector2 halfExtents = bottleInteriorHalfExtents;

        Vector3 local = container.InverseTransformPoint(particle.pos);

        // 입구(로컬 +Y)를 넘었으면 이제부터 자유낙하 — 다시는 이 파티클을 병 기준으로 재해석하지 않는다.
        if (local.y > halfExtents.y)
        {
            particle.exitedBottle = true;
            return;
        }

        // 몸통이 아니라 그 높이의 병목 폭으로 가둔다. 폭이 높이에 따라 연속으로 변하므로
        // X만 잘라도 액체가 어깨를 타고 통로로 자연스럽게 모여든다.
        ApplyClamp(particle, container, local, BottleHalfWidthAt(local.y), halfExtents.y);
    }

    /// <summary>어깨가 시작되는 로컬 Y. 이 아래는 몸통이다.</summary>
    float ShoulderLocalY() =>
        Mathf.Lerp(-bottleInteriorHalfExtents.y, bottleInteriorHalfExtents.y, neckStartHeight01);

    /// <summary>어깨가 끝나고 통로가 시작되는 로컬 Y. 거꾸로 적힌 값도 단차(어깨 없음)로 받아준다.</summary>
    float ChannelLocalY() =>
        Mathf.Max(ShoulderLocalY(),
                  Mathf.Lerp(-bottleInteriorHalfExtents.y, bottleInteriorHalfExtents.y, neckChannelHeight01));

    /// <summary>
    /// 해당 높이에서 병 내부가 허용하는 반너비. 아래에서부터 몸통 → 어깨(사선) → 통로다.
    /// 구간이 바뀌는 지점마다 값이 이어지므로 폭이 튀는 곳이 없다.
    /// </summary>
    float BottleHalfWidthAt(float localY)
    {
        float shoulderY = ShoulderLocalY();
        float channelY = ChannelLocalY();

        // 몸통 — 폭 그대로.
        if (localY <= shoulderY) return bottleInteriorHalfExtents.x;

        // 어깨 — 몸통 폭에서 통로 폭까지 사선으로 좁아진다.
        if (localY <= channelY)
            return Mathf.Lerp(bottleInteriorHalfExtents.x, neckHalfWidth,
                              Mathf.InverseLerp(shoulderY, channelY, localY));

        // 통로 — 입구까지 같은 폭.
        return neckHalfWidth;
    }

    void ConstrainToGlass(SphParticle particle)
    {
        const float gateBuffer = 0.1f;

        Transform container = glassCenter;
        Vector2 halfExtents = glassInteriorHalfExtents;

        // 잔은 회전하지 않으므로, 멀리 있으면(=아직 병 근처거나 낙하 중) 그냥 건드리지 않는다.
        Vector2 worldHalfExtents = Vector2.Scale(halfExtents, container.lossyScale);
        float gateRadius = worldHalfExtents.magnitude + gateBuffer;

        if (Vector2.Distance(particle.pos, container.position) > gateRadius)
            return;

        Vector3 local = container.InverseTransformPoint(particle.pos);
        ApplyClamp(particle, container, local, halfExtents.x, halfExtents.y);
    }

    /// <summary>local 좌표를 컨테이너의 좌/우/바닥 안으로 클램프하고(위는 절대 안 막음), 벽을
    /// 뚫고 나가려던 속도 성분만 제거한다.
    /// halfWidth를 높이마다 다르게 넘기면 병목처럼 좁아지는 형태가 된다.</summary>
    static void ApplyClamp(SphParticle particle, Transform container, Vector3 local, float halfWidth, float halfHeight)
    {
        // 입구보다 위에 있으면 아직 컨테이너 안이 아니다. 이때 좌우로 끌어당기면 잔 옆으로 빗나가야 할
        // 액체까지 잔 위로 빨려들어간다 — 벽 사이 높이에 들어온 뒤에만 좌우를 막는다.
        if (local.y > halfHeight) return;

        float clampedX = Mathf.Clamp(local.x, -halfWidth, halfWidth);
        float clampedY = Mathf.Max(local.y, -halfHeight);

        bool hitSide = !Mathf.Approximately(clampedX, local.x);
        bool hitFloor = !Mathf.Approximately(clampedY, local.y);

        if (!hitSide && !hitFloor) return;

        Vector3 correctedWorld = container.TransformPoint(new Vector3(clampedX, clampedY, local.z));
        particle.pos = correctedWorld;
        particle.transform.position = correctedWorld;

        Vector2 localXAxis = container.right;
        Vector2 localYAxis = container.up;

        if (hitSide)
        {
            float velAlongX = Vector2.Dot(particle.velocity, localXAxis);
            particle.velocity -= velAlongX * localXAxis;
        }

        if (hitFloor)
        {
            float velAlongY = Vector2.Dot(particle.velocity, localYAxis);
            if (velAlongY < 0f)
                particle.velocity -= velAlongY * localYAxis;
        }
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
        if (liquidProfile.overrideColor)
            return liquidProfile.color;

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
