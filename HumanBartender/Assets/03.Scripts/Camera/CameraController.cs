using Cysharp.Threading.Tasks;
using System.Drawing;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour, ICameraZoom, ICameraMove
{
    [Header("References")] 
    [SerializeField] PixelPerfectCamera pixelPerfectCamera;
    [SerializeField] Camera mainCamera;

    [Header("Resolutions")]
    [SerializeField] Vector2Int baseResolution = new Vector2Int(1280, 720);
    [SerializeField] Vector2Int targetResolution = new Vector2Int(960, 540);

    [Header("Move Position")]
    [SerializeField] Transform leftPosition;
    [SerializeField] Transform RightPosition;


    [Header("Transition")]
    [SerializeField] float transitionDuration = 1f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isAtTarget = false;

    void Start()
    {
        ApplyResolutionImmediate(baseResolution);
    }

    
    public void ZoomIn()
    {
        Vector2Int target = targetResolution;
        TransitionResolution(target).Forget();
    }

    public void ZoomOut()
    {
        Vector2Int target = baseResolution;
        TransitionResolution(target).Forget();
    }

    public void CameraMove(string target)
    {
        Vector3 targetPos = target == "left" ? 
            leftPosition.transform.position : RightPosition.transform.position;
        TransitionPosition(targetPos).Forget();
    }

    private void ApplyResolutionImmediate(Vector2Int res)
    {
        pixelPerfectCamera.refResolutionX = res.x;
        pixelPerfectCamera.refResolutionY = res.y;
        pixelPerfectCamera.enabled = true;
    }
    private void ApplyPositionImmediate(Vector3 pos)
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
