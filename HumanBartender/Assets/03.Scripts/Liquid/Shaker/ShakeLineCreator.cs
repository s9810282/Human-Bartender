using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class ShakeLineCreator : MonoBehaviour
{
    [Header("Path Line")]
    [SerializeField] private Color startColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private Color endColor = new Color(1f, 0.6f, 0.2f, 1f);
    [SerializeField] private float pathWidth = 0.06f;

    [Tooltip("선 구간당 중간 포인트 수 (부드러움)")]
    [SerializeField] private int pointsPerSegment = 8;

    [Header("Sorting")]
    [SerializeField] private int sortingOrder = 20;
    [SerializeField] private Sprite circleSprite;

    private LineRenderer dotLine; 


    public void SetLinePosition(int i, Vector3 pos)
    {
        dotLine.SetPosition(i, pos);
    }

    public void CreateLine()
    {
        var obj = new GameObject("ShakeBg");
        obj.transform.parent = transform;
        dotLine = obj.AddComponent<LineRenderer>();
        dotLine.useWorldSpace = true;
        dotLine.sortingOrder = sortingOrder - 2;
        dotLine.material = new Material(Shader.Find("Sprites/Default"));
        dotLine.startColor = startColor;
        dotLine.endColor = endColor;
        dotLine.startWidth = pathWidth;
        dotLine.endWidth = pathWidth;
        dotLine.positionCount = pointsPerSegment;
    }
}

