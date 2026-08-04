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
    [Tooltip("병목(입구)의 반너비. 좁힐수록 천천히, 가늘게 따라진다.\n" +
             "주의: 입구의 월드 폭(= 이 값 x 2 x Bottle의 Scale.x)이 sphConfig.spacing의 2배를 넘으면 " +
             "파티클이 두 줄로 나란히 빠져나와 액체 줄기가 여러 갈래로 갈라져 보인다. " +
             "한 갈래로 흐르게 하려면 월드 폭을 spacing 근처(1~1.3배)로 맞출 것.")]
    [SerializeField] float neckHalfWidth = 0.065f;
    [Range(0f, 1f)]
    [Tooltip("어깨(병목이 시작되는 높이). 0=바닥, 1=입구. 이 아래는 몸통 너비 그대로이고 위로 갈수록 " +
             "neckHalfWidth까지 좁아진다. 낮출수록 목이 길어지는 대신 액체를 담을 몸통이 줄어든다.")]
    [SerializeField] float neckStartHeight01 = 0.6f;

    [Header("Glass")]
    [Tooltip("잔 스프라이트 자신의 Transform. InverseTransformPoint로 로컬 판정 영역을 계산한다.")]
    [SerializeField] Transform glassCenter;
    [Tooltip("잔에 담긴 것으로 칠 판정 영역(스케일 적용 전 잔 로컬 좌표). 스폰이 아니라 판정용이라 벽 여유는 필요 없다.")]
    [SerializeField] Vector2 glassInteriorHalfExtents = new Vector2(0.45f, 0.45f);

    [Header("Liquid (SPH)")]
    [SerializeField] SphLiquidRenderer liquidRenderer;
    [SerializeField] SphConfig sphConfig = new SphConfig();
    [Range(0f, 1f)]
    [Tooltip("병 몸통을 얼마나 채울지(0=빔, 1=몸통 가득). 필요한 파티클 수는 몸통 격자 용량에서 자동으로 " +
             "계산하므로, sphConfig.spacing을 바꿔도 보이는 액체량은 그대로 유지된다.\n" +
             "참고: 압력 계수가 낮으면 액체가 촘촘하게 눌려 실제로 차오르는 높이는 이 값보다 낮다. " +
             "더 차게 하고 싶으면 압력을 올리지 말고(줄기가 앙상해진다) 이 값을 올릴 것.")]
    [SerializeField] float initialFillRatio = 1f;
    [Tooltip("성능/렌더 상한. 위 비율로 계산한 수가 이걸 넘으면 여기까지만 만든다. " +
             "SphLiquidRenderer.MaxBlobs를 넘는 파티클은 어차피 그려지지 않는다.")]
    [SerializeField] int maxParticleCount = SphLiquidRenderer.MaxBlobs;
    [Tooltip("SPH 공간분할그리드가 병/잔 위치 기준 사방으로 확보하는 여유 폭. 병과 잔을 멀리 떨어뜨려 배치해도 " +
             "그 사이 낙하 구간이 그리드 밖으로 벗어나 밀도 계산이 깨지지(=허공에 멈춰 보이지) 않도록 넉넉히 잡는다.")]
    [SerializeField] float gridPadding = 3f;

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

    UniTaskCompletionSource tcs;

    void Start()
    {
        if (isTest)
            data.targetCocktailData = cocktailDataSO.allCocktails[data.targetCocktailId];

        isFinished = false;
        glassParticleCount = 0;

        Color liquidColor = GetLiquidColor();
        liquidRenderer.Init(liquidColor, sphConfig.spacing, sphConfig.maxVelocity);

        if (bottleSilhouette != null)
            bottleSilhouette.Build(bottleInteriorHalfExtents, neckHalfWidth, ShoulderLocalY());

        WarnIfNeckSplitsStream();

        simulation = new SphSimulation(sphConfig, ComputeGridMin(), ComputeGridMax());
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
    /// 병목이 파티클 두 줄 이상 지날 만큼 넓으면 액체가 나란히 빠져나와 줄기가 여러 갈래로 갈라져 보인다.
    /// 블롭이 두꺼울 때는 뭉쳐 보여 눈치채기 어렵고, 줄기를 얇게 만든 뒤에야 드러나는 함정이라 미리 알린다.
    /// </summary>
    void WarnIfNeckSplitsStream()
    {
        float neckWorldWidth = neckHalfWidth * 2f * Mathf.Abs(bottle.BottleVisual.lossyScale.x);

        if (neckWorldWidth > sphConfig.spacing * 1.6f)
        {
            Logger.LogWarning(
                $"[Pour] 병목 월드 폭({neckWorldWidth:0.###})이 파티클 간격({sphConfig.spacing:0.###})에 비해 넓어 " +
                "액체가 여러 갈래로 갈라져 보일 수 있습니다. 한 갈래로 흐르게 하려면 neckHalfWidth를 " +
                $"{sphConfig.spacing * 1.3f / (2f * Mathf.Max(Mathf.Abs(bottle.BottleVisual.lossyScale.x), 0.0001f)):0.###} 근처로 줄이세요.");
        }
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
        float localSpacingX = sphConfig.spacing / Mathf.Max(Mathf.Abs(scale.x), 0.0001f);
        float localSpacingY = sphConfig.spacing / Mathf.Max(Mathf.Abs(scale.y), 0.0001f);

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
        int limit = Mathf.Min(maxParticleCount, SphLiquidRenderer.MaxBlobs);

        if (spawnCount > limit)
        {
            Logger.LogWarning(
                $"[Pour] 몸통을 {initialFillRatio:P0} 채우려면 {spawnCount}개가 필요하지만 상한({limit}개)에 걸려 " +
                "그만큼만 만듭니다. 액체가 덜 차 보이면 sphConfig.spacing을 키우거나 상한을 올리세요.");
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
        particle.Init(sphConfig);
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

        Vector3 local = container.InverseTransformPoint(particle.pos);

        // 입구(로컬 +Y)를 넘었으면 이제부터 자유낙하 — 다시는 이 파티클을 병 기준으로 재해석하지 않는다.
        if (local.y > bottleInteriorHalfExtents.y)
        {
            particle.exitedBottle = true;
            return;
        }

        ApplyClamp(particle, container, local, BottleHalfWidthAt(local.y), bottleInteriorHalfExtents.y);
    }

    /// <summary>어깨가 시작되는 로컬 Y. 이 아래는 몸통, 위는 병목 구간이다.</summary>
    float ShoulderLocalY() =>
        Mathf.Lerp(-bottleInteriorHalfExtents.y, bottleInteriorHalfExtents.y, neckStartHeight01);

    /// <summary>해당 높이에서 병 내부가 허용하는 반너비. 어깨 위로는 병목까지 선형으로 좁아진다.</summary>
    float BottleHalfWidthAt(float localY)
    {
        float shoulderY = ShoulderLocalY();
        if (localY <= shoulderY) return bottleInteriorHalfExtents.x;

        float t = Mathf.InverseLerp(shoulderY, bottleInteriorHalfExtents.y, localY);
        return Mathf.Lerp(bottleInteriorHalfExtents.x, neckHalfWidth, t);
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
    /// 뚫고 나가려던 속도 성분만 제거한다. halfWidth를 높이마다 다르게 넘기면 병목처럼 좁아지는 형태가 된다.</summary>
    static void ApplyClamp(SphParticle particle, Transform container, Vector3 local, float halfWidth, float halfHeight)
    {
        // 입구보다 위에 있으면 아직 컨테이너 안이 아니다. 이때 좌우로 끌어당기면 잔 옆으로 빗나가야 할
        // 액체까지 잔 위로 빨려들어간다 — 벽 사이 높이에 들어온 뒤에만 좌우를 막는다.
        if (local.y > halfHeight) return;

        float clampedX = Mathf.Clamp(local.x, -halfWidth, halfWidth);
        float clampedY = Mathf.Max(local.y, -halfHeight);

        if (Mathf.Approximately(clampedX, local.x) && Mathf.Approximately(clampedY, local.y))
            return;

        Vector2 correctedWorld = container.TransformPoint(new Vector3(clampedX, clampedY, local.z));
        Vector2 pushBack = correctedWorld - particle.pos;

        particle.pos = correctedWorld;
        particle.transform.position = correctedWorld;

        if (pushBack.sqrMagnitude <= 1e-10f) return;

        // 밀어낸 방향을 그대로 벽 법선으로 쓴다. 로컬 X축으로만 속도를 지우면 어깨처럼 비스듬한 벽에서
        // "벽을 파고드는" 성분이 남아 매 프레임 위치만 강제로 밀리다가 에너지가 쌓이고, 결국 입구에서
        // 사방으로 분사되듯 튄다. 보정 벡터를 쓰면 기울어진 벽이든 축마다 스케일이 다르든 항상 맞는다.
        Vector2 normal = pushBack.normalized;
        float intoWall = Vector2.Dot(particle.velocity, normal);

        if (intoWall < 0f)
            particle.velocity -= intoWall * normal;
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
