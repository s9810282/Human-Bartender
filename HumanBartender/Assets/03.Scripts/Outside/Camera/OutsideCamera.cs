using System.Collections.Generic;
using UnityEngine;
using VContainer;

public enum EOutsideCameraMode
{
    Follow = 0,
    Elevator = 1
}

[System.Serializable]
public class OutsideCameraOption
{
    public EOutsideCameraMode cameraMode;

    public Transform cameraTarget;
    public Vector3 cameraOffset;

    public bool isChangeResolution;
    public ECameraZoomType targetResolution;
}

public class OutsideCamera : MonoBehaviour
{
    [Header("CameraOption")]
    [SerializeField] OutsideCameraOption[] cameraOptions;
    
    [SerializeField] EOutsideCameraMode curCameraMode = EOutsideCameraMode.Follow;

    [SerializeField] CameraControllerNew cameraZoom;


    private Dictionary<EOutsideCameraMode, OutsideCameraOption> _cameraMap;
    

    void Start()
    {
        _cameraMap = new Dictionary<EOutsideCameraMode, OutsideCameraOption>(cameraOptions.Length);
        foreach (var slot in cameraOptions)
        {
            _cameraMap[slot.cameraMode] = slot;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeCameraMode(int n)
    {
        curCameraMode = (EOutsideCameraMode)n;

        ExcuteCameraOption(_cameraMap[curCameraMode]);
    }

    public void ExcuteCameraOption(OutsideCameraOption mode)
    {
        cameraZoom.TransitionFollowOffset(mode.cameraOffset, 1f);
        cameraZoom.CameraZoom(mode.targetResolution, 1.2f);
    }
}
