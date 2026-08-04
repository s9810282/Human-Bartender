using UnityEngine;

/// <summary>
/// 병의 외곽선을 메시로 그린다. PourManager가 액체를 가두는 내부 프로파일(몸통 반너비 → 어깨 → 병목)을
/// 그대로 넘겨받아 거기에 유리 두께만 더해 그리므로, "보이는 병 모양"과 "액체가 갇히는 형태"가
/// 어긋날 수 없다. 사각 스프라이트 두 장으로 몸통/목을 흉내내면 어깨의 완만한 경사를 표현하지 못해
/// 그 구간에서 액체가 병 밖으로 삐져나와 보였다.
/// </summary>
public class BottleSilhouette : MonoBehaviour
{
    [SerializeField] Color glassColor = new Color(0.75f, 0.8f, 0.85f, 0.4f);
    [Tooltip("내부 프로파일 바깥으로 그릴 유리벽 두께(병 로컬 좌표).")]
    [SerializeField] float wallThickness = 0.08f;
    [SerializeField] int sortingOrder = 10;

    MeshRenderer meshRenderer;
    Mesh mesh;
    Material material;

    /// <summary>
    /// 내부 프로파일을 받아 실루엣을 다시 만든다. shoulderY 아래는 몸통 반너비 그대로,
    /// 위로는 neckHalfWidth까지 좁아지는 6각형이다(볼록해서 삼각형 부채꼴로 바로 분할된다).
    /// </summary>
    public void Build(Vector2 interiorHalfExtents, float neckHalfWidth, float shoulderY)
    {
        EnsureCreated();

        float m = wallThickness;
        float bodyHalfWidth = interiorHalfExtents.x + m;
        float mouthHalfWidth = neckHalfWidth + m;
        float bottomY = -interiorHalfExtents.y - m;
        float topY = interiorHalfExtents.y + m;

        var vertices = new Vector3[]
        {
            new Vector3(-bodyHalfWidth, bottomY, 0f),
            new Vector3(bodyHalfWidth, bottomY, 0f),
            new Vector3(bodyHalfWidth, shoulderY, 0f),
            new Vector3(mouthHalfWidth, topY, 0f),
            new Vector3(-mouthHalfWidth, topY, 0f),
            new Vector3(-bodyHalfWidth, shoulderY, 0f),
        };

        var colors = new Color[vertices.Length];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = glassColor;

        var triangles = new int[(vertices.Length - 2) * 3];
        for (int i = 0; i < vertices.Length - 2; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
    }

    void EnsureCreated()
    {
        if (mesh != null) return;

        var go = new GameObject("Silhouette");
        go.transform.SetParent(transform, false);

        var meshFilter = go.AddComponent<MeshFilter>();
        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sortingOrder = sortingOrder;

        material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.material = material;

        mesh = new Mesh { name = "BottleSilhouetteMesh" };
        meshFilter.mesh = mesh;
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
        if (mesh != null) Destroy(mesh);
    }
}
