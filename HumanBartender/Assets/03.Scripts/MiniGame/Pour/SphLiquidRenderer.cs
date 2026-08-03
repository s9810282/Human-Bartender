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
    public const int MaxBlobs = 96;

    [SerializeField] float blobRadius = 0.22f;
    [SerializeField] int sortingOrder = 25;

    [Header("Metaball Shader")]
    [SerializeField] float threshold = 1f;
    [SerializeField] float edgeSmoothness = 0.15f;
    [Tooltip("셰이더가 블롭 필드를 계산할 월드 스페이스 영역. 병+잔+그 사이 공간을 넉넉히 덮어야 한다.")]
    [SerializeField] Vector2 fieldQuadSize = new Vector2(14f, 10f);
    [SerializeField] Vector2 fieldQuadCenter = Vector2.zero;

    MeshRenderer meshRenderer;
    Material material;
    MaterialPropertyBlock propertyBlock;

    Vector4[] blobData;

    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int ThresholdId = Shader.PropertyToID("_Threshold");
    static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    static readonly int BlobCountId = Shader.PropertyToID("_BlobCount");
    static readonly int BlobPositionsId = Shader.PropertyToID("_BlobPositions");

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

    public void Init(Color liquidColor)
    {
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
            Vector2 p = particles[i].pos;
            blobData[i] = new Vector4(p.x, p.y, blobRadius, 0f);
        }

        for (int i = count; i < MaxBlobs; i++)
            blobData[i] = Vector4.zero;

        propertyBlock.SetInt(BlobCountId, count);
        propertyBlock.SetVectorArray(BlobPositionsId, blobData);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
