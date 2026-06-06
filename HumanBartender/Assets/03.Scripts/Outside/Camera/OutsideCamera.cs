using DG.Tweening;
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
    public AnimationCurve offsetCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public AnimationCurve resolutionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public Transform cameraTarget;
    public Transform cameraParent;
    public Vector3 cameraOffset;
    public bool isFirstChangeParent = false;

    public float offsetDuration = 1f;
    public float resolutionDuration = 1f;

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

    public void ChangeCameraMode(int n)
    {
        curCameraMode = (EOutsideCameraMode)n;

        ExcuteCameraOption(_cameraMap[curCameraMode]);
    }

    public void ExcuteCameraOption(OutsideCameraOption mode)
    {
        if(mode.isFirstChangeParent)
            cameraZoom.FollowTarget(mode.cameraParent);

        cameraZoom.TransitionFollowOffset(mode.cameraOffset, mode.offsetDuration, mode.offsetCurve);
        cameraZoom.TransitionCameraZoom(mode.targetResolution, mode.resolutionDuration, mode.resolutionCurve);

        if(!mode.isFirstChangeParent)
            cameraZoom.FollowTarget(mode.cameraParent);
    }
}
