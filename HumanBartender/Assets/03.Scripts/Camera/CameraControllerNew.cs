using Cysharp.Threading.Tasks;
using DG.Tweening;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;
using System.Threading;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;


/// <summary>슬롯 타입(Left/Right/Middle 등)별로 카메라가 이동할 목표 Transform을 매핑하는 데이터.</summary>
[System.Serializable]
public class CameraPostion
{
    public ESlotType slotType = ESlotType.Middle;
    public Transform pos;
}

/// <summary>카메라 줌(해상도) 프리셋 종류.</summary>
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
/// Cinemachine 기반 카메라 컨트롤러(CameraController의 후속 구현).
/// 카메라 자체를 옮기지 않고 vcam이 추적하는 cameraAnchor를 이동시키며, 해상도 전환도
/// Pixel Perfect Camera 대신 Cinemachine Lens의 OrthographicSize를 직접 보간한다.
/// </summary>
public class CameraControllerNew : MonoBehaviour, ICameraControlNew
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

    /// <summary>시작 시 Cinemachine Lens 크기를 현재 Pixel Perfect Camera 참조 해상도에 맞춰 동기화한다.</summary>
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



    #region TargetOffset
    /// <summary>cameraAnchor를 target의 자식으로 붙여 따라다니게 만든다.</summary>
    public void FollowTarget(Transform target, Vector3 localOffset = default)
    {
        if (cameraAnchor == null || target == null) return;

        cameraAnchor.SetParent(target, worldPositionStays: true);
        //cameraAnchor.localPosition = localOffset;
    }
    /// <summary>cameraAnchor의 부모를 해제해 추적을 중단한다.</summary>
    public void Unfollow()
    {
        if (cameraAnchor == null) return;
        cameraAnchor.SetParent(null, worldPositionStays: true);
    }
    /// <summary>추적 대상 기준 로컬 오프셋을 즉시 적용한다.</summary>
    public void SetFollowOffset(Vector3 localOffset)
    {
        CancelOffset();
        if (cameraAnchor != null)
            cameraAnchor.localPosition = localOffset;
    }
    /// <summary>현재 오프셋에서 targetOffset까지 duration초 동안 부드럽게 전환한다.</summary>
    public void TransitionFollowOffset(Vector3 targetOffset, float duration = 1f, AnimationCurve curve = null)
    {
        TransitionOffsetAsync(targetOffset, duration, SafeCurve(curve)).Forget();
    }
    private async UniTaskVoid TransitionOffsetAsync(Vector3 targetOffset, float duration, AnimationCurve curve)
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
                float k = curve.Evaluate(Mathf.Clamp01(t / duration));
                cameraAnchor.localPosition = Vector3.Lerp(from, targetOffset, k);
                await UniTask.Yield(token);
            }
            cameraAnchor.localPosition = targetOffset;
        }
        catch (System.OperationCanceledException) { }
    }
    #endregion


    #region CameraZoom
    /// <summary>해상도를 즉시 zoomType 프리셋으로 바꾼다.</summary>
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base)
    {
        ApplyResolutionImmediate(GetResolution(zoomType));
    }
    /// <summary>해상도를 즉시 zoomType으로 바꾼 뒤, tcs가 완료되면 원래 해상도로 되돌린다.</summary>
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
    /// <summary>Pixel Perfect Camera 참조 해상도를 즉시 바꾸고 Cinemachine Lens/Confiner 캐시를 갱신한다.</summary>
    public void ApplyResolutionImmediate(Vector2Int res)
    {
        CancelResolution();

        pixelPerfectCamera.refResolutionX = res.x;
        pixelPerfectCamera.refResolutionY = res.y;
        pixelPerfectCamera.enabled = true;

        SyncLensToPPC();
        InvalidateConfinerCache();
    }
    /// <summary>dur초 동안 zoomType 프리셋 해상도로 서서히 전환한다.</summary>
    public void TransitionCameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f, AnimationCurve curve = null)
    {
        Vector2Int target = GetResolution(zoomType);
        TransitionResolution(target, dur, SafeCurve(curve)).Forget();
    }
    /// <summary>
    /// Pixel Perfect Camera를 잠시 끄고 Cinemachine Lens의 OrthographicSize를 duration 동안 보간해
    /// 부드러운 줌 연출을 만든 뒤, 목표 크기로 고정한다. 전환이 끝나면 PPC의 참조 해상도를 to로 동기화하고
    /// 다시 켠다(Lens 크기는 재계산하지 않아 스냅 없이 자연스럽게 이어진다).
    /// </summary>
    private async UniTaskVoid TransitionResolution(Vector2Int to, float duration, AnimationCurve curve)
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
                float k = curve.Evaluate(Mathf.Clamp01(t / duration));

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

        // 보간으로 이미 도달한 OrthographicSize는 유지한 채 PPC 참조 해상도만 to로 동기화하고 다시 켠다
        // (SyncLensToPPC를 호출하면 Lens 크기가 재계산되어 화면이 튈 수 있으므로 호출하지 않음)
        pixelPerfectCamera.refResolutionX = to.x;
        pixelPerfectCamera.refResolutionY = to.y;
        pixelPerfectCamera.enabled = true;

        InvalidateConfinerCache();
    }
    #endregion



    #region Util
    private static readonly AnimationCurve LinearCurve = AnimationCurve.Linear(0, 0, 1, 1);
    private static AnimationCurve SafeCurve(AnimationCurve curve)
    {
        return curve ?? LinearCurve;
    }
    /// <summary>줌 타입에 대응하는 해상도 프리셋을 반환한다.</summary>
    private Vector2Int GetResolution(ECameraZoomType zoomType) => zoomType switch
    {
        ECameraZoomType.Base    => baseResolution,
        ECameraZoomType.Sub     => targetResolution,
        ECameraZoomType.OutSide => outSideResolution,
        _                       => baseResolution,
    };
    /// <summary>참조 해상도(res)를 픽셀당 유닛(PPU) 기준 직교 카메라 크기(orthographicSize)로 환산한다.</summary>
    private float ResToOrthoSize(Vector2Int res)
    {
        return res.y / (2f * pixelPerfectCamera.assetsPPU);
    }
    /// <summary>Cinemachine Lens의 OrthographicSize를 현재 Pixel Perfect Camera 참조 해상도와 일치시킨다.</summary>
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
    /// <summary>카메라 이동/해상도 변경 후 Confiner2D의 경계 캐시를 갱신되도록 무효화한다.</summary>
    private void InvalidateConfinerCache()
    {
        if (confiner != null) confiner.InvalidateBoundingShapeCache();
    }
    /// <summary>진행 중인 위치 전환(현재 미사용 - _positionCts를 세팅하는 코드가 없음)을 취소한다.</summary>
    private void CancelPosition()
    {
        if (_positionCts != null)
        {
            _positionCts.Cancel();
            _positionCts.Dispose();
            _positionCts = null;
        }
    }
    /// <summary>진행 중인 해상도(줌) 전환을 취소한다.</summary>
    private void CancelResolution()
    {
        if (_resolutionCts != null)
        {
            _resolutionCts.Cancel();
            _resolutionCts.Dispose();
            _resolutionCts = null;
        }
    }
    /// <summary>진행 중인 팔로우 오프셋 전환을 취소한다.</summary>
    private void CancelOffset()
    {
        if (_offsetCts != null)
        {
            _offsetCts.Cancel();
            _offsetCts.Dispose();
            _offsetCts = null;
        }
    }
    #endregion
}
