using LiquidSimulation;
using UnityEngine;

public class SturTest : MonoBehaviour
{
    [SerializeField] LiquidContainer shakerCon;
    [SerializeField] LiquidContainer glassCon;
    [SerializeField] PouringSystem pour;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (pour.IsPouring)
            {
                pour.StopPouring();
                Debug.Log("[Pour] Disconnected - 기울기 복원은 ↑키");
            }
            else
            {
                pour.StartPouring(shakerCon, glassCon);
                Debug.Log("[Pour] Connected - ←→키로 쉐이커 기울이기");
            }
        }

        // ★ ←→: 쉐이커 수동 기울기
        // 에디터에서 직접 rotation을 돌려도 동일하게 동작
        float tiltInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) tiltInput = 60f;   // CCW
        if (Input.GetKey(KeyCode.RightArrow)) tiltInput = -60f;  // CW

        if (Mathf.Abs(tiltInput) > 0.1f)
        {
            Vector3 euler = shakerCon.transform.eulerAngles;
            euler.z += tiltInput * Time.deltaTime;
            // -90 ~ 90 범위 제한
            float z = euler.z;
            if (z > 180f) z -= 360f;
            z = Mathf.Clamp(z, -90f, 90f);
            shakerCon.transform.rotation = Quaternion.Euler(0, 0, z);
        }

        // ★ ↑: 기울기 리셋 (서서히 복원)
        if (Input.GetKey(KeyCode.UpArrow))
        {
            float z = shakerCon.transform.eulerAngles.z;
            if (z > 180f) z -= 360f;
            z = Mathf.MoveTowards(z, 0f, 80f * Time.deltaTime);
            shakerCon.transform.rotation = Quaternion.Euler(0, 0, z);
        }
    }
}
