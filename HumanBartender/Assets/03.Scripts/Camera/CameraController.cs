using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering.Universal;


[System.Serializable]
public class CameraPostion
{
    public SlotType slotType = SlotType.Middle;
    public Transform pos;
}

public enum CameraZoomType
{
    Base = 0,
    Sub,
    OutSide,
}


public class CameraController : MonoBehaviour, ICameraZoom, ICameraMove
{
    [Header("References")] 
    [SerializeField] PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] Camera mainCamera;

    [Header("Resolutions")]
    [SerializeField] Vector2Int baseResolution = new Vector2Int(1280, 720);
    [SerializeField] Vector2Int targetResolution = new Vector2Int(960, 540);
    [SerializeField] Vector2Int outSideResolution = new Vector2Int(480, 270);

    [Header("Move Position")]
    [SerializeField] CameraPostion[] movePositions;

    private Dictionary<SlotType, CameraPostion> _slotMap;

    [Header("Transition")]
    [SerializeField] float transitionDuration = 1f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isAtTarget = false;

    void Start()
    {

        _slotMap = new Dictionary<SlotType, CameraPostion>(movePositions.Length);

        foreach (var slot in movePositions)
        {
            _slotMap[slot.slotType] = slot;            
        }

        ApplyResolutionImmediate(baseResolution);
    }

    public async void ActionZoomAndBack(CameraZoomType zoomType = CameraZoomType.Base,  UniTaskCompletionSource tcs = null)
    {
        Vector2Int curResolution = new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        Vector2Int target = zoomType == CameraZoomType.Base ? baseResolution : 
            zoomType == CameraZoomType.Sub ? targetResolution : outSideResolution;

        ApplyResolutionImmediate(target);

        await tcs.Task;

        ApplyResolutionImmediate(curResolution);

        return;
    }

    public void ActionZoom(CameraZoomType zoomType = CameraZoomType.Base)
    {
        Vector2Int curResolution = new Vector2Int(pixelPerfectCamera.refResolutionX, pixelPerfectCamera.refResolutionY);
        Vector2Int target = zoomType == CameraZoomType.Base ? baseResolution :
             zoomType == CameraZoomType.Sub ? targetResolution : outSideResolution;

        ApplyResolutionImmediate(target);
    }



    public void ZoomIn(float dur = 1f)
    {
        Vector2Int target = targetResolution;
        transitionDuration = dur;
        TransitionResolution(target).Forget();
    }

    public void ZoomOut(float dur = 1f)
    {
        Vector2Int target = baseResolution;
        transitionDuration = dur;
        TransitionResolution(target).Forget();
    }

    public void CameraMove(SlotType slot, float dur = 1f)
    {
        Vector3 targetPos = _slotMap[slot].pos.transform.position;
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
