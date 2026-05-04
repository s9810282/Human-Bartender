using UnityEngine;

public enum EOutsideCameraMode
{
    Follow,
    Elevator
}



public class OutsideCamera : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] CameraFollow2D followCamera;
    [SerializeField] ElevatorCamera elevatorCamera;

    [SerializeField] EOutsideCameraMode curCameraMode = EOutsideCameraMode.Follow;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LateUpdate()
    {
        if(curCameraMode == EOutsideCameraMode.Follow )
            followCamera.Handle();
    }
}
