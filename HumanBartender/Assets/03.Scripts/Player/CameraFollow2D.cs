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

    private Vector3 _velocity = Vector3.zero;

    
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(
            target.position.x + offset.x,
            lockY ? fixedY : target.position.y + offset.y,
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desired,
            ref _velocity,
            smoothTime
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        _velocity = Vector3.zero;
    }
}
