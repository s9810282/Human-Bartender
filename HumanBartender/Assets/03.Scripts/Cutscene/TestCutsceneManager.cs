using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;


public class TestCutSceneManager : MonoBehaviour, IEffectPlayer
{
    [Header("Canvas")]
    [SerializeField] Canvas        cutSceneCanvas;
    [SerializeField] RectTransform canvasRect;

    [Header("CutScene Root (이미지들의 부모 — 패닝 대상)")]
    [SerializeField] RectTransform cutSceneRoot;

    [Header("Overlay / Images")]
    [SerializeField] Image         effectOverlay;
    [SerializeField] Image         bgImage;
    [SerializeField] List<Image>   images = new();

    [Header("Padding")]
    [SerializeField] float padding_X = 0;
    [SerializeField] float padding_Y = 0;


    private EEffectType curEffect = EEffectType.None;
    private CancellationTokenSource _cts;

    public int  CurrentStepIndex { get; private set; } = -1;
    public bool IsPlaying        { get; private set; } = false;

    Queue<Image>              imagePool    = new();
    Dictionary<string, Image> activeImages = new();


    Dictionary<ECutSceneAction, Func<TestCutSceneStep, UniTask>> actionHandlers;


    static readonly Dictionary<AnchorType, Vector2> anchorPreset = new()
    {
        { AnchorType.Center,      new Vector2(0.5f, 0.5f) },
        { AnchorType.Left,        new Vector2(0.0f, 0.5f) },
        { AnchorType.Right,       new Vector2(1.0f, 0.5f) },
        { AnchorType.TopLeft,     new Vector2(0.0f, 1.0f) },
        { AnchorType.TopRight,    new Vector2(1.0f, 1.0f) },
        { AnchorType.BottomLeft,  new Vector2(0.0f, 0.0f) },
        { AnchorType.BottomRight, new Vector2(1.0f, 0.0f) },
    };



    void Awake()
    {
        imagePool = new Queue<Image>(images);
        ResetImages();

        actionHandlers = new Dictionary<ECutSceneAction, Func<TestCutSceneStep, UniTask>>
        {
            [ECutSceneAction.ShowImage]  = ExecuteShowImage,
            [ECutSceneAction.HideImage]  = ExecuteHideImage,
            [ECutSceneAction.HideAll]    = ExecuteHideAll,
            [ECutSceneAction.ShowLayout] = ExecuteShowLayout,
        };
    }


    
    public async UniTask PlayComicCutSceneAsync(
        TestCutScene cutScene,
        CancellationToken token = default,
        UniTaskCompletionSource tcs = null)
    {
        await PlayFromStepAsync(cutScene, 0, token);
        tcs?.TrySetResult();
    }

    public async UniTask PlayFromStepAsync(
        TestCutScene cutScene,
        int startIndex,
        CancellationToken token = default)
    {
        if (cutScene.steps == null || cutScene.steps.Count == 0) return;
        startIndex = Mathf.Clamp(startIndex, 0, cutScene.steps.Count - 1);

        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        var ct = _cts.Token;

        IsPlaying = true;
        cutSceneCanvas.worldCamera = Camera.main;
        SetupBackground(cutScene);

        if (cutScene.isCutSceneMove)
        {
            await UniTask.WhenAll(
                RunSequenceAsync(cutScene, startIndex, ct),
                RunCameraMoveAsync(cutScene, startIndex, ct)
            );
        }
        else
        {
            await RunSequenceAsync(cutScene, startIndex, ct);
        }

        IsPlaying = false;
        CurrentStepIndex = -1;
        ResetRootPosition();
    }

    public async UniTask PlaySingleStepAsync(
        TestCutSceneStep step,
        CancellationToken token = default)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(token);

        IsPlaying = true;
        cutSceneCanvas.worldCamera = Camera.main;

        if (actionHandlers.TryGetValue(step.action, out var handler))
            await handler(step);
        else
            Debug.LogWarning($"[TestCutSceneManager] 알 수 없는 action: {step.action}");

        IsPlaying = false;
    }

    public void StopPlayback()
    {
        _cts?.Cancel();
        _cts = null;
        IsPlaying = false;
        CurrentStepIndex = -1;
    }

    public void ClearCutScene()
    {
        StopPlayback();
        ResetImages();
        ResetRootPosition();
        ClearBackground();
        curEffect = EEffectType.None;
    }


    void SetupBackground(TestCutScene cutScene)
    {
        if (!cutScene.isSetBGSprite || string.IsNullOrEmpty(cutScene.bgPath))
        {
            ClearBackground();
            return;
        }

        Sprite bg = Resources.Load<Sprite>($"Cutscenes/{cutScene.bgPath}");
        if (bg != null && bgImage != null)
        {
            bgImage.sprite = bg;
            bgImage.gameObject.SetActive(true);
        }
    }

    void ClearBackground()
    {
        if (bgImage != null)
        {
            bgImage.sprite = null;
            bgImage.gameObject.SetActive(false);
        }
    }



    async UniTask RunCameraMoveAsync(
        TestCutScene cutScene,
        int startIndex,
        CancellationToken token)
    {
        if (cutSceneRoot == null) return;

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;
        float moveRatio = cutScene.cameraRatio;

        Vector2 fullDelta = cutScene.cameraMoveType switch
        {
            ECutSceneCameraMoveType.Right => new Vector2(-w * moveRatio, 0),
            ECutSceneCameraMoveType.Left  => new Vector2( w * moveRatio, 0),
            ECutSceneCameraMoveType.Up    => new Vector2(0, -h * moveRatio),
            ECutSceneCameraMoveType.Down  => new Vector2(0,  h * moveRatio),
            _                            => Vector2.zero,
        };

        if (fullDelta == Vector2.zero) return;

        int totalSteps = cutScene.steps.Count;
        float startRatio = totalSteps > 1 ? (float)startIndex / (totalSteps - 1) : 0f;

        Vector2 startPos = fullDelta * startRatio;
        Vector2 endPos   = fullDelta;

        cutSceneRoot.anchoredPosition = startPos;

        float remainDuration = cutScene.cameraDuration * (1f - startRatio);
        if (remainDuration <= 0) return;

        await cutSceneRoot
            .DOAnchorPos(endPos, remainDuration)
            .SetEase(cutScene.easeGraph)
            .ToUniTask(cancellationToken: token);
    }

    void ResetRootPosition()
    {
        if (cutSceneRoot != null)
        {
            cutSceneRoot.DOKill();
            cutSceneRoot.anchoredPosition = Vector2.zero;
        }
    }



    async UniTask RunSequenceAsync(
        TestCutScene cutScene,
        int startIndex,
        CancellationToken token)
    {
        float timeOffset = startIndex > 0
            ? cutScene.steps[startIndex].time
            : 0f;

        float startTime = Time.time - timeOffset;

        // 발사한 Step Task를 모아둔다
        List<UniTask> runningSteps = new();

        for (int i = startIndex; i < cutScene.steps.Count; i++)
        {
            token.ThrowIfCancellationRequested();

            var step = cutScene.steps[i];
            CurrentStepIndex = i;

            float targetTime = startTime + step.time;
            await UniTask.WaitUntil(() => Time.time >= targetTime,
                                    cancellationToken: token);

            if (actionHandlers.TryGetValue(step.action, out var handler))
                runningSteps.Add(handler(step));
            else
                Debug.LogWarning($"[TestCutSceneManager] 알 수 없는 action: {step.action}");
        }

        // 모든 Step의 enter → duration → exit가 끝날 때까지 대기
        await UniTask.WhenAll(runningSteps);
    }



    async UniTask ExecuteShowImage(TestCutSceneStep step)
    {
        Image img = GetPooledImage();
        if (img == null) return;

        Sprite sprite = Resources.Load<Sprite>($"Cutscenes/{step.path}");
        if (sprite != null) img.sprite = sprite;
        img.SetNativeSize();

        SetImagePositionPreset(img, step.positionPreset);
        activeImages[step.path] = img;

        await ApplyEnterAnimation(img, step);

        if (step.duration > 0)
            await UniTask.Delay(TimeSpan.FromSeconds(step.duration));

        if (step.exitType != EExitPreset.None)
        {
            await ApplyExitAnimation(img, step);
            ReturnToPool(step.path, img);
        }
    }

    async UniTask ExecuteHideImage(TestCutSceneStep step)
    {
        if (!activeImages.TryGetValue(step.path, out Image img)) return;

        await ApplyExitAnimation(img, step);
        ReturnToPool(step.path, img);
    }

    async UniTask ExecuteHideAll(TestCutSceneStep step)
    {
        if (activeImages.Count == 0) return;

        List<UniTask> exitTasks = new();
        List<KeyValuePair<string, Image>> snapshot = new(activeImages);

        foreach (var kvp in snapshot)
            exitTasks.Add(ApplyExitAnimation(kvp.Value, step));

        await UniTask.WhenAll(exitTasks);

        foreach (var kvp in snapshot)
            ReturnToPool(kvp.Key, kvp.Value);
    }

    async UniTask ExecuteShowLayout(TestCutSceneStep step)
    {
        await ExecuteShowImage(step);
    }



    float GetEnterSlideDistance(TestCutSceneStep step)
    {
        return step.enterSlideDistance > 0f ? step.enterSlideDistance : 1.0f;
    }
    float GetExitSlideDistance(TestCutSceneStep step)
    {
        return step.exitSlideDistance > 0f ? step.exitSlideDistance : 1.0f;
    }
    Ease GetEnterEase(TestCutSceneStep step, Ease fallback = Ease.OutCubic)
    {
        return step.enterEase == Ease.Unset ? fallback : step.enterEase;
    }
    Ease GetExitEase(TestCutSceneStep step, Ease fallback = Ease.InCubic)
    {
        return step.exitEase == Ease.Unset ? fallback : step.exitEase;
    }


    async UniTask ApplyEnterAnimation(Image img, TestCutSceneStep step)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = step.enterDuration;
        float dist     = GetEnterSlideDistance(step);
        Ease  ease     = GetEnterEase(step);

        switch (step.enterType)
        {
            case EEneterPreset.Cut:
                img.color = Color.white;
                img.gameObject.SetActive(true);
                break;

            case EEneterPreset.FadeIn:
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await img.DOFade(1f, duration)
                         .SetEase(GetEnterEase(step, Ease.Linear))
                         .ToUniTask();
                break;

            case EEneterPreset.SlideLeft:
            {
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest + new Vector2(canvasRect.rect.width * dist, 0);
                img.gameObject.SetActive(true);
                await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EEneterPreset.SlideRight:
            {
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest - new Vector2(canvasRect.rect.width * dist, 0);
                img.gameObject.SetActive(true);
                await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EEneterPreset.SlideUp:
            {
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest - new Vector2(0, canvasRect.rect.height * dist);
                img.gameObject.SetActive(true);
                await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EEneterPreset.SlideDown:
            {
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest + new Vector2(0, canvasRect.rect.height * dist);
                img.gameObject.SetActive(true);
                await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EEneterPreset.ScaleUp:
            {
                Ease scaleEase = GetEnterEase(step, Ease.OutBack);
                rect.localScale = Vector3.one * 0.5f;
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await UniTask.WhenAll(
                    rect.DOScale(1f, duration).SetEase(scaleEase).ToUniTask(),
                    img.DOFade(1f, duration).ToUniTask()
                );
                break;
            }

            default:
                img.color = Color.white;
                img.gameObject.SetActive(true);
                Debug.LogWarning($"[TestCutSceneManager] 알 수 없는 enter type: {step.enterType}");
                break;
        }
    }


 
    async UniTask ApplyExitAnimation(Image img, TestCutSceneStep step)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = step.exitDuration;
        float dist     = GetExitSlideDistance(step);
        Ease  ease     = GetExitEase(step);

        switch (step.exitType)
        {
            case EExitPreset.Cut:
                img.color = new Color(1, 1, 1, 0);
                break;

            case EExitPreset.FadeOut:
                await img.DOFade(0f, duration)
                         .SetEase(GetExitEase(step, Ease.Linear))
                         .ToUniTask();
                break;

            case EExitPreset.SlideLeft:
            {
                Vector2 target = rect.anchoredPosition - new Vector2(canvasRect.rect.width * dist, 0);
                await rect.DOAnchorPos(target, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EExitPreset.SlideRight:
            {
                Vector2 target = rect.anchoredPosition + new Vector2(canvasRect.rect.width * dist, 0);
                await rect.DOAnchorPos(target, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EExitPreset.SlideUp:
            {
                Vector2 target = rect.anchoredPosition + new Vector2(0, canvasRect.rect.height * dist);
                await rect.DOAnchorPos(target, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EExitPreset.SlideDown:
            {
                Vector2 target = rect.anchoredPosition - new Vector2(0, canvasRect.rect.height * dist);
                await rect.DOAnchorPos(target, duration).SetEase(ease).ToUniTask();
                break;
            }
            case EExitPreset.ScaleDown:
            {
                Ease scaleEase = GetExitEase(step, Ease.InBack);
                await UniTask.WhenAll(
                    rect.DOScale(0f, duration).SetEase(scaleEase).ToUniTask(),
                    img.DOFade(0f, duration).ToUniTask()
                );
                break;
            }

            default:
                await img.DOFade(0f, duration).ToUniTask();
                Debug.LogWarning($"[TestCutSceneManager] 알 수 없는 exit type: {step.exitType}, FadeOut 대체");
                break;
        }
    }

    public async UniTask PlayEffectAsync(EEffectType type, float duration, float intensity = 0f)
    {
        await ExecuteEffect(type, duration, intensity);
    }
    async UniTask ExecuteEffect(EEffectType type, float duration = 1f, float intensity = 0)
    {
        if (curEffect == type) return;
        curEffect = type;

        switch (type)
        {
            case EEffectType.FadeOut:
                effectOverlay.color = new Color(1, 1, 1, 1);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(0f, duration).ToUniTask();
                effectOverlay.gameObject.SetActive(false);
                break;

            case EEffectType.FadeIn:
                effectOverlay.color = new Color(1, 1, 1, 0);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(1f, duration).ToUniTask();
                break;

            case EEffectType.FlashWhite:
                effectOverlay.color = Color.white;
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(0f, duration).ToUniTask();
                effectOverlay.gameObject.SetActive(false);
                break;

            case EEffectType.ScreenShake:
                await canvasRect.DOShakeAnchorPos(duration, intensity, 20, 90, false, true)
                                .ToUniTask();
                break;

            case EEffectType.ZoomPulse:
                await canvasRect.DOScale(intensity, duration * 0.5f)
                                .SetEase(Ease.OutQuad).ToUniTask();
                await canvasRect.DOScale(1f, duration * 0.5f)
                                .SetEase(Ease.InQuad).ToUniTask();
                break;

            case EEffectType.Vignette:
                effectOverlay.gameObject.SetActive(true);
                effectOverlay.color = new Color(0, 0, 0, 0);
                await effectOverlay.DOFade(0.7f, duration).ToUniTask();
                break;

            case EEffectType.Dim:
                effectOverlay.color = new Color(0, 0, 0, 0);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(intensity, duration).ToUniTask();
                break;

            case EEffectType.Chromatic:
                Debug.Log("[TestCutSceneManager] chromatic — 오버레이 근사치 사용 중");
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
                break;

            default:
                Debug.LogWarning($"[TestCutSceneManager] 알 수 없는 effect: {type}");
                break;
        }
    }
    public EEffectType ConvertStringToEffect(string input)
    {
        return input.ToLower() switch
        {
            "fade_in"      => EEffectType.FadeIn,
            "fade_out"     => EEffectType.FadeOut,
            "flash_white"  => EEffectType.FlashWhite,
            "screen_shake" => EEffectType.ScreenShake,
            "zoom_pulse"   => EEffectType.ZoomPulse,
            "vignette"     => EEffectType.Vignette,
            "dim"          => EEffectType.Dim,
            "chromatic"    => EEffectType.Chromatic,
            _              => EEffectType.None
        };
    }

    public void SetImagePositionPreset(Image img, TestPositionPreset preset)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        Vector2 anchor = anchorPreset[preset.Anchor];

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = anchor;

        float w = canvasRect.rect.width  - padding_X;
        float h = canvasRect.rect.height - padding_Y;
        rect.anchoredPosition = new Vector2(w * preset.OffsetX, h * preset.OffsetY);
    }

    Image GetPooledImage()
    {
        if (imagePool.Count == 0)
        {
            Debug.LogWarning("[TestCutSceneManager] 이미지 풀 비어있음");
            return null;
        }
        return imagePool.Dequeue();
    }
    void ReturnToPool(string imageId, Image img)
    {
        img.gameObject.SetActive(false);
        img.color = Color.white;
        img.GetComponent<RectTransform>().localScale = Vector3.one;
        activeImages.Remove(imageId);
        imagePool.Enqueue(img);
    }
    public void ResetImages()
    {
        foreach (var kvp in activeImages)
        {
            kvp.Value.gameObject.SetActive(false);
            imagePool.Enqueue(kvp.Value);
        }

        effectOverlay.gameObject.SetActive(false);
        activeImages.Clear();
    }
}
