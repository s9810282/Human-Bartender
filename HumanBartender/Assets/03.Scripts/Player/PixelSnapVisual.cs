using UnityEngine;

public class PixelSnapVisual : MonoBehaviour
{
    [SerializeField] private float ppu = 100f;

    private void LateUpdate()
    {
        Vector3 worldPos = transform.parent.position;
        Vector3 snapped = new Vector3(
            Mathf.Round(worldPos.x * ppu) / ppu,
            Mathf.Round(worldPos.y * ppu) / ppu,
            worldPos.z
        );
        // 부모 위치와의 차이를 localPosition으로 보정
        transform.localPosition = snapped - worldPos;
    }
}