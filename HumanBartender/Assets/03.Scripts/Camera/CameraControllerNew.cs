using Cysharp.Threading.Tasks;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;


public class CameraControllerNew : MonoBehaviour, ICameraControl
{
    [Header("References")]
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private CinemachineCamera vcam;
    [SerializeField] private CinemachineConfiner2D confiner;

    [Tooltip("vcam의 Tracking Target. 카메라 이동 시 이 Transform을 옮김")]
    [SerializeField] private Transform cameraAnchor;

    [Header("Resolutions")]
    [SerializeField] private Vector2Int baseResolution = new Vector2Int(1280, 720);
    [SerializeField] private Vector2Int targetResolution = new Vector2Int(960, 540);
    [SerializeField] private Vector2Int outSideResolution = new Vector2Int(480, 270);

    [Header("Transition")]
    [SerializeField] private float defaultTransitionDuration = 1f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private CancellationTokenSource _positionCts;
    private CancellationTokenSource _resolutionCts;
    private CancellationTokenSource _offsetCts;

    private void Awake()
    {
        SyncLensToPPC();
    }

    private void OnDestroy()
    {
        CancelPosition();
        CancelResolution();
        CancelOffset();
    }

    public void FollowTarget(Transform target, Vector3 localOffset = default)
    {
        if (cameraAnchor == null || target == null) return;

        cameraAnchor.SetParent(target, worldPositionStays: false);
        cameraAnchor.localPosition = localOffset;
    }

    public void Unfollow()
    {
        if (cameraAnchor == null) return;
        cameraAnchor.SetParent(null, worldPositionStays: true);
    }


    public void SetFollowOffset(Vector3 localOffset)
    {
        CancelOffset();
        if (cameraAnchor != null)
            cameraAnchor.localPosition = localOffset;
    }
    public void TransitionFollowOffset(Vector3 targetOffset, float duration = 1f)
    {
        TransitionOffsetAsync(targetOffset, duration).Forget();
    }

    private async UniTaskVoid TransitionOffsetAsync(Vector3 targetOffset, float duration)
    {
        if (cameraAnchor == null) return;

        CancelOffset();
        _offsetCts = new CancellationTokenSource();
        var token = _offsetCts.Token;

        Vector3 from = cameraAnchor.localPosition;
        float t = 0f;
        try
        {
            while (t < duration)
            {
                token.ThrowIfCancellationRequested();
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / duration));
                cameraAnchor.localPosition = Vector3.Lerp(from, targetOffset, k);
                await UniTask.Yield(token);
            }
            cameraAnchor.localPosition = targetOffset;
        }
        catch (System.OperationCanceledException) { }
    }


    public void CameraMove(ESlotType slot, float dur = 1f)
    {
        return;
    }
    public void CameraMove(Vector3 worldPos, float dur = 1f)
    {
        Unfollow();
        TransitionPosition(worldPos, dur).Forget();
    }

    public void ApplyPositionImmediate(Vector3 worldPos)
    {
        CancelPosition();
        Unfollow();

        if (cameraAnchor != null)
            cameraAnchor.position = new Vector3(worldPos.x, worldPos.y, cameraAnchor.position.z);
    }
    private async UniTaskVoid TransitionPosition(Vector3 targetPos, float duration)
    {
        if (cameraAnchor == null) return;

        CancelPosition();
        _positionCts = new CancellationTokenSource();
        var token = _positionCts.Token;

        Vector3 from = cameraAnchor.position;
        Vector3 to = new Vector3(targetPos.x, targetPos.y, from.z);

        float t = 0f;
        try
        {
            while (t < duration)
            {
                token.ThrowIfCancellationRequested();

                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / duration));
                cameraAnchor.position = Vector3.Lerp(from, to, k);

                await UniTask.Yield(token);
            }
            cameraAnchor.position = to;
        }
        catch (System.OperationCanceledException) { }
    }


    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base)
    {
        ApplyResolutionImmediate(GetResolution(zoomType));
    }

    public async void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null)
    {
        Vector2Int current = new Vector2Int(
            pixelPerfectCamera.refResolutionX,
            pixelPerfectCamera.refResolutionY
        );

        ApplyResolutionImmediate(GetResolution(zoomType));

        if (tcs != null) await tcs.Task;

        ApplyResolutionImmediate(current);
    }


    public void CameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f)
    {
        Vector2Int target = GetResolution(zoomType);
        TransitionResolution(target, dur).Forget();
    }

    public void ApplyResolutionImmediate(Vector2Int res)
    {
        CancelResolution();

        pixelPerfectCamera.refResolutionX = res.x;
        pixelPerfectCamera.refResolutionY = res.y;
        pixelPerfectCamera.enabled = true;

        SyncLensToPPC();
        InvalidateConfinerCache();
    }

    private async UniTaskVoid TransitionResolution(Vector2Int to, float duration)
    {
        CancelResolution();
        _resolutionCts = new CancellationTokenSource();
        var token = _resolutionCts.Token;

        float fromSize = vcam.Lens.OrthographicSize;
        float toSize = ResToOrthoSize(to);

        pixelPerfectCamera.enabled = false;

        float t = 0f;
        var lens = vcam.Lens;

        try
        {
            while (t < duration)
            {
                token.ThrowIfCancellationRequested();
                t += Time.deltaTime;
                float k = ease.Evaluate(Mathf.Clamp01(t / duration));
  
                lens.OrthographicSize = Mathf.Lerp(fromSize, toSize, k);
                vcam.Lens = lens;

                InvalidateConfinerCache();

                await UniTask.Yield(token);
            }
        }
        catch (System.OperationCanceledException)
        {
            return;
        }

        lens.OrthographicSize = toSize;
        vcam.Lens = lens;

        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token);

        ApplyResolutionImmediate(to);
    }



    private Vector2Int GetResolution(ECameraZoomType zoomType) => zoomType switch
    {
        ECameraZoomType.Base    => baseResolution,
        ECameraZoomType.Sub     => targetResolution,
        ECameraZoomType.OutSide => outSideResolution,
        _                       => baseResolution,
    };

    private float ResToOrthoSize(Vector2Int res)
    {
        return res.y / (2f * pixelPerfectCamera.assetsPPU);
    }

    private void SyncLensToPPC()
    {
        if (vcam == null || pixelPerfectCamera == null) return;

        var lens = vcam.Lens;
        lens.OrthographicSize = ResToOrthoSize(new Vector2Int(
            pixelPerfectCamera.refResolutionX,
            pixelPerfectCamera.refResolutionY
        ));
        vcam.Lens = lens;
    }

    private void InvalidateConfinerCache()
    {
        if (confiner != null) confiner.InvalidateBoundingShapeCache();
    }

    private void CancelPosition()
    {
        if (_positionCts != null)
        {
            _positionCts.Cancel();
            _positionCts.Dispose();
            _positionCts = null;
        }
    }

    private void CancelResolution()
    {
        if (_resolutionCts != null)
        {
            _resolutionCts.Cancel();
            _resolutionCts.Dispose();
            _resolutionCts = null;
        }
    }

    private void CancelOffset()
    {
        if (_offsetCts != null)
        {
            _offsetCts.Cancel();
            _offsetCts.Dispose();
            _offsetCts = null;
        }
    }
}
