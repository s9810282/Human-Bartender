using UnityEngine;


public class CameraFollow2D : MonoBehaviour
{
    [Header("타겟")]
    [SerializeField] private Transform target;

    [Header("오프셋")]
    [SerializeField] private Vector2 offset = new Vector2(0f, 1f);

    [Header("부드러움")]
    [Tooltip("값이 작을수록 빨리 따라옴 (0.1~0.3 권장)")]
    [SerializeField] private float smoothTime = 0.2f;

    [Header("Y축 처리")]
    [Tooltip("켜면 카메라 Y는 고정값을 유지 (점프 시 흔들림 방지)")]
    [SerializeField] private bool lockY = false;
    [SerializeField] private float fixedY = 0f;

    [Header("월드 경계 (Orthographic 카메라 전용)")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxBounds = new Vector2(50f, 50f);

    private Vector3 _velocity = Vector3.zero;
    private Camera _cam;


    private void Awake()
    {
        _cam = Camera.main;
    }

    public void Handle()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            lockY ? fixedY : target.position.y + offset.y,
            transform.position.z
        );

        Vector3 next = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _velocity,
            smoothTime
        );

        if (useBounds && _cam != null && _cam.orthographic)
        {
            float halfH = _cam.orthographicSize;
            float halfW = halfH * _cam.aspect;

            float minX = minBounds.x + halfW;
            float maxX = maxBounds.x - halfW;
            float minY = minBounds.y + halfH;
            float maxY = maxBounds.y - halfH;

            next.x = (minX <= maxX) ? Mathf.Clamp(next.x, minX, maxX)
                                    : (minBounds.x + maxBounds.x) * 0.5f;
            next.y = (minY <= maxY) ? Mathf.Clamp(next.y, minY, maxY)
                                    : (minBounds.y + maxBounds.y) * 0.5f;
        }

        transform.position = next;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _velocity = Vector3.zero;
    }


    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;
        Gizmos.color = Color.cyan;
        Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0f);
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) * 0.5f,
            (minBounds.y + maxBounds.y) * 0.5f,
            0f
        );
        Gizmos.DrawWireCube(center, size);
    }
}
