using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class CameraPostion
{
    public ESlotType slotType = ESlotType.Middle;
    public Transform pos;
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ECameraZoomType
{
    None = 0,
    [EnumMember(Value = "base")]
    Base,
    [EnumMember(Value = "sub")]
    Sub,
    [EnumMember(Value = "outside")]
    OutSide,
}


/// <summary>
/// 추후 공용으로 사용할 항목과 실내 전용 구분하기
/// </summary>

public class CameraController : MonoBehaviour, ICameraControl
{
    [Header("References")] 
    [SerializeField] PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] CinemachineCamera vcam;
    [SerializeField] CinemachineConfiner2D confiner;
    [SerializeField] Camera mainCamera;

    [Header("Resolutions")]
    [SerializeField] Vector2Int baseResolution = new Vector2Int(1280, 720);
    [SerializeField] Vector2Int targetResolution = new Vector2Int(960, 540);
    [SerializeField] Vector2Int outSideResolution = new Vector2Int(480, 270);

    [Header("Move Position")]
    [SerializeField] CameraPostion[] movePositions;

    private Dictionary<ESlotType, CameraPostion> _slotMap;

    [Header("Transition")]
    [SerializeField] float transitionDuration = 1f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    

    private bool isAtTarget = false;

    void Start()
    {
        _slotMap = new Dictionary<ESlotType, CameraPostion>(movePositions.Length);
        foreach (var slot in movePositions)
        {
            _slotMap[slot.slotType] = slot;            
        }
    }

    public async void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base,  UniTaskCompletionSource tcs = null)
    {
        Vector2Int curResolution = new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        Vector2Int target = zoomType == ECameraZoomType.Base ? baseResolution : 
            zoomType == ECameraZoomType.Sub ? targetResolution : outSideResolution;

        ApplyResolutionImmediate(target);

        await tcs.Task;

        ApplyResolutionImmediate(curResolution);

        return;
    }

    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base)
    {
        Vector2Int curResolution = new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        Vector2Int target = zoomType == ECameraZoomType.Base ? baseResolution :
             zoomType == ECameraZoomType.Sub ? targetResolution : outSideResolution;

        ApplyResolutionImmediate(target);
    }

    public void CameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f)
    {
        Vector2Int target = zoomType == ECameraZoomType.Base ? baseResolution :
                 zoomType == ECameraZoomType.Sub ? targetResolution : outSideResolution;

        transitionDuration = dur;
        TransitionResolution(target).Forget();
    }

    public void CameraMove(ESlotType slot, float dur = 1f)
    {
        Vector3 targetPos = _slotMap[slot].pos.transform.position;
        transitionDuration = dur;
        TransitionPosition(targetPos).Forget();
    }

    public void CameraMove(Vector3 pos, float dur = 1)
    {
        Vector3 targetPos = pos;
        transitionDuration = dur;
        TransitionPosition(targetPos).Forget();
    }

    public void ApplyResolutionImmediate(Vector2Int res)
    {
        pixelPerfectCamera.refResolutionX = res.x;
        pixelPerfectCamera.refResolutionY = res.y;
        pixelPerfectCamera.enabled = true;

    }
    public void ApplyPositionImmediate(Vector3 pos)
    {
        mainCamera.transform.position = pos;
    }

    public async UniTaskVoid TransitionPosition(Vector3 targetPos)
    {
        Vector3 fromPosition = mainCamera.transform.position;
        Vector3 targetPosition = targetPos;
        
        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / transitionDuration));
            mainCamera.transform.position = Vector3.Lerp(fromPosition, targetPosition, k);

            await UniTask.Yield();
        }

        ApplyPositionImmediate(targetPosition);

        return;
    }
    public async UniTaskVoid TransitionResolution(Vector2Int to)
    {
        float fromSize = mainCamera.orthographicSize;
        float toSize = ResToOrthoSize(to);

        pixelPerfectCamera.enabled = false;

        float t = 0f;
        while (t < transitionDuration)
        {
            t += Time.deltaTime;
            float k = ease.Evaluate(Mathf.Clamp01(t / transitionDuration));
            mainCamera.orthographicSize = Mathf.Lerp(fromSize, toSize, k);
            await UniTask.Yield();
        }

        ApplyResolutionImmediate(to);

        return;
    }

    private float ResToOrthoSize(Vector2Int res)
    {
        return res.y / (2f * pixelPerfectCamera.assetsPPU);
    }
}
