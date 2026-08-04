using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pour/MetaballStream 셰이더로 SPH 파티클 전체를 그린다. 곡선을 따라가는 절차적 블롭이 아니라,
/// 매 프레임 실제 살아있는 SphParticle들의 월드 위치를 그대로 블롭 좌표로 넘긴다 — 병 안에 있든,
/// 흘러나오는 중이든, 잔 안에 있든 전부 같은 하나의 액체 표현이다.
/// </summary>
public class SphLiquidRenderer : MonoBehaviour
{
    /// <summary>MetaballStream.shader의 MAX_BLOBS와 반드시 같아야 한다. 이 수를 넘는 파티클은 그려지지 않는다.</summary>
    public const int MaxBlobs = 192;

    [Tooltip("블롭 반지름을 파티클 간격(sphConfig.spacing)의 몇 배로 잡을지. 이 값이 작으면 이웃 파티클끼리 " +
             "필드가 겹치지 않아 threshold를 넘지 못하고, 액체가 아니라 점들이 흩뿌려진 것처럼 보인다. " +
             "2배 이상은 되어야 인접 파티클이 서로 뭉친다.")]
    [SerializeField] float blobRadiusScale = 1.375f;
    [SerializeField] int sortingOrder = 25;

    [Header("Motion Stretch")]
    [Tooltip("최고 속도(sphConfig.maxVelocity)에서 블롭을 진행 방향으로 몇 배 늘릴지. 떨어지는 파티클은 " +
             "가속되며 간격이 벌어지는데, 늘여주지 않으면 그 사이가 이어지지 않아 알갱이로 흩어져 보인다.")]
    [SerializeField] float maxStretch = 3f;
    [Range(0f, 0.95f)]
    [Tooltip("최고 속도(sphConfig.maxVelocity)에서 줄기를 몇 % 가늘게 만들지. 0.7이면 병 안에 고인 액체의 " +
             "30% 굵기까지 얇아진다. 실제 액체도 떨어지며 가속될수록 단면이 좁아지므로 낙하 구간만 " +
             "자연스럽게 가늘어진다. 0이면 고인 액체와 같은 굵기로 흘러 뭉툭해 보인다.")]
    [SerializeField] float streamThinning = 0.3f;

    [Header("Metaball Shader")]
    [Tooltip("필드가 이 값을 넘는 영역이 액체로 그려진다. 낮출수록 액체가 부풀어 잘 뭉치고, " +
             "혼자 떨어지는 물방울도 보이게 된다(1.0이면 고립된 파티클은 중심점만 찍혀 사실상 안 보인다).")]
    [SerializeField] float threshold = 1f;
    [SerializeField] float edgeSmoothness = 0.15f;
    [Tooltip("셰이더가 블롭 필드를 계산할 월드 스페이스 영역. 병+잔+그 사이 공간을 넉넉히 덮어야 한다.")]
    [SerializeField] Vector2 fieldQuadSize = new Vector2(14f, 10f);
    [SerializeField] Vector2 fieldQuadCenter = Vector2.zero;

    MeshRenderer meshRenderer;
    Material material;
    MaterialPropertyBlock propertyBlock;

    float blobRadius;
    float maxSpeed = 1f;
    Vector4[] blobData;
    Vector4[] blobStretch;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    static readonly int BlobCountId = Shader.PropertyToID("_BlobCount");
    static readonly int BlobPositionsId = Shader.PropertyToID("_BlobPositions");
    static readonly int BlobStretchId = Shader.PropertyToID("_BlobStretch");

    void Awake()
    {
        var go = new GameObject("Liquid Field");
        go.transform.SetParent(transform, false);

        var meshFilter = go.AddComponent<MeshFilter>();
        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;

        meshFilter.mesh = BuildQuadMesh(fieldQuadCenter, fieldQuadSize);

        material = new Material(Shader.Find("Pour/MetaballStream"));
        meshRenderer.material = material;

        propertyBlock = new MaterialPropertyBlock();
        blobData = new Vector4[MaxBlobs];
        blobStretch = new Vector4[MaxBlobs];
    }

    static Mesh BuildQuadMesh(Vector2 center, Vector2 size)
    {
        Vector2 half = size * 0.5f;

        var mesh = new Mesh { name = "MetaballFieldQuad" };
        mesh.vertices = new Vector3[]
        {
            new Vector3(center.x - half.x, center.y - half.y, 0f),
            new Vector3(center.x + half.x, center.y - half.y, 0f),
            new Vector3(center.x + half.x, center.y + half.y, 0f),
            new Vector3(center.x - half.x, center.y + half.y, 0f),
        };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// particleSpacing과 maxSpeed에는 sphConfig의 spacing / maxVelocity를 그대로 넘긴다.
    /// - 블롭 반지름을 파티클 간격에서 유도해야 spacing을 조정해도 액체가 계속 하나로 뭉쳐 보인다.
    /// - 늘임/가늘어짐을 최고 속도로 정규화해야 gravity나 maxVelocity를 바꿔도 줄기 굵기가 따라
    ///   흔들리지 않는다(속도 절대값에 걸어두면 물리를 느리게 튜닝하는 순간 줄기가 뭉툭해진다).
    /// </summary>
    public void Init(Color liquidColor, float particleSpacing, float maxSpeed)
    {
        blobRadius = particleSpacing * blobRadiusScale;
        this.maxSpeed = Mathf.Max(maxSpeed, 0.0001f);

        propertyBlock.SetColor(ColorId, liquidColor);
        propertyBlock.SetFloat(ThresholdId, threshold);
        propertyBlock.SetFloat(SmoothnessId, edgeSmoothness);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void UpdateBlobs(IReadOnlyList<SphParticle> particles)
    {
        int count = Mathf.Min(particles.Count, MaxBlobs);

        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = particles[i].velocity;
            float speed = velocity.magnitude;

            // 거의 멈춰 있으면 방향이 의미 없으므로 늘이지 않고 원 그대로 둔다.
            Vector2 dir = speed > 0.01f ? velocity / speed : Vector2.right;

            // 최고 속도 기준으로 정규화한다. 정지 상태(고인 액체)는 원래 굵기 그대로고,
            // 최고 속도에서 maxStretch만큼 늘어나며 streamThinning만큼 가늘어진다.
            float speed01 = Mathf.Clamp01(speed / maxSpeed);
            float stretch = Mathf.Lerp(1f, maxStretch, speed01);
            float radius = blobRadius * Mathf.Lerp(1f, 1f - streamThinning, speed01);

            Vector2 p = particles[i].pos;
            blobData[i] = new Vector4(p.x, p.y, radius, 0f);
            blobStretch[i] = new Vector4(dir.x, dir.y, stretch, 0f);
        }

        for (int i = count; i < MaxBlobs; i++)
        {
            blobData[i] = Vector4.zero;
            blobStretch[i] = new Vector4(1f, 0f, 1f, 0f);
        }

        propertyBlock.SetInt(BlobCountId, count);
        propertyBlock.SetVectorArray(BlobPositionsId, blobData);
        propertyBlock.SetVectorArray(BlobStretchId, blobStretch);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
