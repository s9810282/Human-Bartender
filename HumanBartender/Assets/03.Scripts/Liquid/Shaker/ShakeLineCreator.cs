using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class ShakeLineCreator : MonoBehaviour
{
    [Header("StrikeNode")]
    [SerializeField] ShakingStrikeNode strikeNode;

    [Header("Dot")]
    [SerializeField] Image dotMiddle;
    [SerializeField] Image dotTop;
    [SerializeField] Image dotBottom;


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
    private Camera cam;


    private void Start()
    {
        cam = Camera.main;

        CreateLine();
        BuildBgPath();

        dotTop.color = startColor;
        dotMiddle.color = Color.Lerp(startColor, endColor, 0.5f);
        dotBottom.color = endColor;
    }


    public Vector3 GetDotWorldPosition(int index)
    {
        Vector3 local;

        switch (index)
        {
            case 0: local = dotTop.rectTransform.position; break;
            case 1: local = dotMiddle.rectTransform.position; break;
            case 2: local = dotBottom.rectTransform.position; break;

            default: return (Vector2)transform.position;
        }


        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint
            (cam, local);
        screenPos.z = 10f;
        Vector3 worldPos = cam.ScreenToWorldPoint(screenPos);


        return worldPos;
    }


    private void BuildBgPath()
    {
        dotLine.SetPosition(0, GetDotWorldPosition(0));
        dotLine.SetPosition(1, GetDotWorldPosition(1));
        dotLine.SetPosition(2, GetDotWorldPosition(2));
    }
    private void CreateLine()
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

