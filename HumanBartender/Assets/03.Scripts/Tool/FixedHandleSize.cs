using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
[DefaultExecutionOrder(100)] // ScrollRect 이후에 실행
public class FixedHandleSize : MonoBehaviour
{
    [Range(0.01f, 1f)] public float handleSize = 0.1f;

    Scrollbar sb;
    void Awake() => sb = GetComponent<Scrollbar>();
    void LateUpdate()
    {
        if (!Mathf.Approximately(sb.size, handleSize))
            sb.size = handleSize;
    }
}