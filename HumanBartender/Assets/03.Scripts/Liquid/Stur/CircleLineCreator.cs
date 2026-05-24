using UnityEngine;
using UnityEngine.UI;

public class CircleLineCreator : MonoBehaviour
{
    [Header("Path Line")]
    [SerializeField] private Color lineColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] float radius = 2f;
    [SerializeField] int segments = 64;
    [SerializeField] float width = 0.05f;
    [SerializeField] Vector3 centerPos;
    [SerializeField] private int sortingOrder = 20;

    [SerializeField] LineRenderer lr;


    public void BuildCircle(float rad, Vector3 center)
    {
        centerPos = center;
        radius = rad;

        lr.transform.position = centerPos;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.sortingOrder = sortingOrder - 2;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lineColor;
        lr.endColor = lineColor;
        lr.startWidth = lr.endWidth = width;
        lr.positionCount = segments;
        
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

}
