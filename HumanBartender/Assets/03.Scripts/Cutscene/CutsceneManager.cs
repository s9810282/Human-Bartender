using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using static UnityEditor.Progress;
using static UnityEditor.SceneView;



public class CutSceneManager : MonoBehaviour, IEffectPlayer, ICutScenePlayer
{
    [SerializeField] CutSceneDataSO data;

    [SerializeField] SpriteAnimationManager spriteAnimationManager;
    [SerializeField] SpineAnimationManager spineAnimationManager;

    private ICameraZoom cameraZoom;
    private ICameraMove cameraMove;
    


    private const string ANIM_SLOT = "SpriteAnim";


    [SerializeField] Canvas cutSceneCanvas;
    [SerializeField] RectTransform canvasRect;
    [SerializeField] Image effectOverlay;           // 화면 전체 페이드/플래시용 단일 오버레이
    [SerializeField] List<Image> images = new();

    [SerializeField] float padding_X = 0;
    [SerializeField] float padding_Y = 0;


    private CancellationTokenSource _cts;


    // ── Anchor 프리셋 ─────────────────────────────────────────────────
    readonly Dictionary<AnchorType, Vector2> anchorPreset = new()
    {
        { AnchorType.Center,       new Vector2(0.5f, 0.5f) },
        { AnchorType.Left,         new Vector2(0.0f, 0.5f) },
        { AnchorType.Right,        new Vector2(1.0f, 0.5f) },
        { AnchorType.TopLeft,     new Vector2(0.0f, 1.0f) },
        { AnchorType.TopRight,    new Vector2(1.0f, 1.0f) },
        { AnchorType.BottomLeft,  new Vector2(0.0f, 0.0f) },
        { AnchorType.BottomRight, new Vector2(1.0f, 0.0f) },
    };

    Queue<Image> imagePool = new();
    Dictionary<string, Image> activeImages = new();   // imageId → Image


    void Awake()
    {
        imagePool = new Queue<Image>(images);
        ResetImages();


        spriteAnimationManager.Initialize();
    }




    public void SetSceneDependencies(ICameraZoom zoom, ICameraMove move)
    {
        cameraZoom = zoom;
        cameraMove = move;
    }
    public void ClearSceneDependencies()
    {
        cameraZoom = null;
        cameraMove = null;
    }



    public void ClearCutScene()
    {
        ResetImages();
        spriteAnimationManager.ActiveSelf(false);
        spriteAnimationManager.SetInactive();


    }

    public async UniTask PlayCutScene(
        string id,
        UniTaskCompletionSource tcs = null)
    {
        Logger.Log($"Play CutScene : {id}");

        if (!data.spriteCutsceneCachedById.TryGetValue(id, out SpriteCutscene cutScene))
        {
            Debug.LogWarning($"[CutsceneManager] 컷씬 ID를 찾을 수 없음: {id}");
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var token = CancellationTokenSource
            .CreateLinkedTokenSource(_cts.Token, this.GetCancellationTokenOnDestroy())
            .Token;

        cutSceneCanvas.worldCamera = Camera.main;


        //Enter, Exit 구조 변경.

        switch (cutScene.Type)
        {
            case CutsceneType.Sprite:
                await PlaySpriteAnimationCutScene(cutScene, token, tcs);
                break;

            case CutsceneType.Spine:
                break;

            case CutsceneType.Comic:
                await PlayComicCutSceneAsync(id, token, tcs);
                break;
            

            default: break;
        }
    }


    /// <summary>
    /// 추후 다른 값들에 대한 처리, position, loop, blocking 등
    /// 
    /// </summary>
    /// <param name="data"></param>
    /// <param name="token"></param>
    /// <param name="tcs"></param>
    /// <returns></returns>
    private async UniTask PlaySpriteAnimationCutScene(
        SpriteCutscene data, 
        CancellationToken token,
        UniTaskCompletionSource tcs = null)
    {
        UniTaskCompletionSource cameraTcs = new UniTaskCompletionSource();

        var handle = await ResourceLoader.TryLoadAsync<AnimationClip>(data.Id, token);

        await ExecuteEffect("fade_in", data.EnterDuration.Value);
        cameraZoom.ActionZoomAndBack(cameraTcs);


        if (handle.HasValue)
        {
            spriteAnimationManager.ActiveSelf(true);
            spriteAnimationManager.SetClip(ANIM_SLOT, handle);
            spriteAnimationManager.PlayAnimation(ANIM_SLOT, token);
            await UniTask.WaitForSeconds(handle.Value.Result.length);
        }
        else
        {
            await UniTask.WaitForSeconds(1f);
        }
            
        if (tcs != null)
            tcs.TrySetResult();

        await ExecuteEffect("fade_out", data.EnterDuration.Value);
        cameraTcs.TrySetResult();

        ResourceLoader.ReleaseHandle<AnimationClip>(ref handle);
    }



    private async UniTask PlayComicCutSceneAsync(
        string id, 
        CancellationToken token, 
        UniTaskCompletionSource tcs = null)
    {
        cutSceneCanvas.worldCamera = Camera.main;

        if (!data.spriteCutsceneCachedById.TryGetValue(id, out SpriteCutscene cutScene))
        {
            Debug.LogWarning($"[CutsceneManager] 컷씬 ID를 찾을 수 없음: {id}");
            return;
        }

        //await RunSequenceAsync(cutScene);

        if (tcs != null)
            tcs.TrySetResult();
        
    }


    async UniTask ExecuteEffect(string type, float duration, float intensity = 0)
    {
        switch (type)
        {
            case "fade_out":
                effectOverlay.color = new Color(1, 1, 1, 1);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(0f, duration).ToUniTask();
                effectOverlay.gameObject.SetActive(false);
                break;

            case "fade_in":
                effectOverlay.color = new Color(1, 1, 1, 0);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(1f, duration).ToUniTask();
                break;

            case "flash_white":
                effectOverlay.color = Color.white;
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(0f, duration).ToUniTask();
                effectOverlay.gameObject.SetActive(false);
                break;

            case "screen_shake":
                await canvasRect.DOShakeAnchorPos(duration, intensity, 20, 90, false, true)
                                .ToUniTask();
                break;

            case "zoom_pulse":

                await canvasRect.DOScale(intensity, duration * 0.5f)
                                .SetEase(Ease.OutQuad)
                                .ToUniTask();
                await canvasRect.DOScale(1f, duration * 0.5f)
                                .SetEase(Ease.InQuad)
                                .ToUniTask();
                break;

            case "vignette":
                // 비네트 스프라이트가 effectOverlay에 할당된 상태를 가정
                effectOverlay.gameObject.SetActive(true);
                effectOverlay.color = new Color(0, 0, 0, 0);
                await effectOverlay.DOFade(0.7f, duration).ToUniTask();
                break;

            // 신규: 반투명 검은 오버레이 (dim)
            case "dim":
                effectOverlay.color = new Color(0, 0, 0, 0);
                effectOverlay.gameObject.SetActive(true);
                await effectOverlay.DOFade(intensity, duration).ToUniTask();
                break;

            case "chromatic":
                // URP PostProcessing이 없는 경우 근사치: 오버레이로 대체
                // TODO: URP Volume 연동으로 교체 가능
                Debug.Log("[CutsceneManager] chromatic — 현재 오버레이 근사치 사용 중");
                await UniTask.Delay(TimeSpan.FromSeconds(duration));
                break;

            default:
                Debug.LogWarning($"[CutsceneManager] 알 수 없는 effect_type: {type}");
                break;
        }
    }



    public async UniTask PlayEffectAsync(string type, float duration, float intensity = 0f)
    {
        await ExecuteEffect(type, duration, intensity);
    }



    /* ************************************************************ */

    #region Comic
    /*
    

    // ── 시퀀스 실행 ───────────────────────────────────────────────────

    async UniTask RunSequenceAsync(SpriteCutscene cutScene)
    {
        float startTime = Time.time;

        //foreach (CutsceneStep step in cutScene.Steps)
        //{
        //    // 지정된 time까지 대기
        //    float targetTime = startTime + step.Time;
        //    await UniTask.WaitUntil(() => Time.time >= targetTime);

        //    // 핸들러 실행 — 대기 없이 발사(non-blocking)하여 다음 step 타이밍과 겹칠 수 있음
        //    if (actionHandlers.TryGetValue(step.Action, out var handler))
        //        FireAndForget(handler(step)).Forget();
        //    else
        //        Debug.LogWarning($"[CutsceneManager] 알 수 없는 action: {step.Action}");
        //}
    }

    async UniTaskVoid FireAndForget(UniTask task)
    {
        try   { await task; }
        catch (Exception e) { Debug.LogException(e); }
    }


    // ── Action 핸들러 구현 ────────────────────────────────────────────

    async UniTask ExecuteShowImage(CutsceneStep step)
    {
        Image img = GetPooledImage();
        if (img == null) return;

        // 스프라이트 로드 (Resources 폴더 기준 — 프로젝트 구조에 맞게 교체)
        Sprite sprite = Resources.Load<Sprite>($"Cutscenes/{step.Image}");
        if (sprite != null) img.sprite = sprite;

        SetImagePositionPreset(img, data.cutSceneData.PositionPresets[step.Position ?? "center"]);

        activeImages[step.Image] = img;

        float duration = step.EnterDuration ?? GetEnterDefaultDuration(step.Enter);
        await ApplyEnterAnimation(img, step.Enter ?? "cut", duration);
    }

    async UniTask ExecuteHideImage(CutsceneStep step)
    {
        if (!activeImages.TryGetValue(step.Image, out Image img)) return;

        string exitType = step.Exit ?? "fade_out";
        float duration = step.ExitDuration ?? GetExitDefaultDuration(exitType);
        await ApplyExitAnimation(img, exitType, duration);

        ReturnToPool(step.Image, img);
    }

    /// <summary>
    /// 신규: 현재 활성화된 모든 컷씬 이미지를 병렬로 퇴장시킨 뒤 풀에 반환
    /// Effect Overlay도 포함
    /// </summary>
    async UniTask ExecuteHideAll(CutsceneStep step)
    {
        if (activeImages.Count == 0) return;

        string exitType = step.Exit ?? "fade_out";
        float duration = step.ExitDuration ?? GetExitDefaultDuration(exitType);

        List<UniTask> exitTasks = new();
        List<KeyValuePair<string, Image>> snapshot = new(activeImages);

        foreach (var kvp in snapshot)
        {
            exitTasks.Add(ApplyExitAnimation(kvp.Value, exitType, duration));
        }

        //exitTasks.Add(ApplyExitAnimation(effectOverlay, exitType, duration));

        await UniTask.WhenAll(exitTasks);

        // 퇴장 완료 후 풀에 반환
        foreach (var kvp in snapshot)
        {
            ReturnToPool(kvp.Key, kvp.Value);
        }
    }

    async UniTask ExecuteShowLayout(CutsceneStep step)
    {
        if (!data.cutSceneData.LayoutPresets.TryGetValue(step.Layout, out LayoutPreset layout)) return;
        if (step.Images == null || step.Images.Length < layout.Slots.Length) return;

        List<Image> assigned = new();
        for (int i = 0; i < layout.Slots.Length; i++)
        {
            Image img = GetPooledImage();
            if (img == null) break;

            Sprite sprite = Resources.Load<Sprite>($"Cutscenes/{step.Images[i]}");
            if (sprite != null) img.sprite = sprite;

            assigned.Add(img);
            activeImages[step.Images[i]] = img;
        }

        SetImagesLayOut(assigned, layout);

        // ── enters 배열 지원 (신규) ──────────────────────────────────
        // enters 배열이 있으면 이미지별 개별 enter 적용,
        // 없으면 기존 enter 단일값을 모든 이미지에 적용 (하위호환)
        bool hasIndividualEnters = step.Enters != null && step.Enters.Length > 0;

        if (hasIndividualEnters && step.Enters.Length != assigned.Count)
        {
            Debug.LogWarning($"[CutsceneManager] enters 배열 길이({step.Enters.Length})와 " +
                             $"images 수({assigned.Count})가 불일치. 부족분은 'cut' 적용.");
        }

        List<UniTask> enterTasks = new();
        for (int i = 0; i < assigned.Count; i++)
        {
            assigned[i].gameObject.SetActive(true);   

            string enterType;
            float duration;

            if (hasIndividualEnters)
            {
                enterType = i < step.Enters.Length ? step.Enters[i] : "cut";
                duration = (step.EnterDurations != null && i < step.EnterDurations.Length)
                    ? step.EnterDurations[i]
                    : GetEnterDefaultDuration(enterType);
            }
            else
            {
                enterType = step.Enter ?? "cut";
                duration = step.EnterDuration ?? GetEnterDefaultDuration(enterType);
            }

            enterTasks.Add(ApplyEnterAnimation(assigned[i], enterType, duration));
        }

        await UniTask.WhenAll(enterTasks);
    }



    // ── Enter 애니메이션 ──────────────────────────────────────────────
    //아 이미지 위치 옮겨지고 액티브되어야함.

    async UniTask ApplyEnterAnimation(Image img, string enterType, float duration)
    {
        RectTransform rect = img.GetComponent<RectTransform>();

        switch (enterType)
        {
            case "cut":
                img.color = Color.white;
                img.gameObject.SetActive(true);
                break;

            case "fade_in":
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await img.DOFade(1f, duration).ToUniTask();
                break;

            case "slide_left":
            {
                Vector2 origin = rect.anchoredPosition + new Vector2(canvasRect.rect.width, 0);
                rect.anchoredPosition = origin;
                img.gameObject.SetActive(true);

                await rect.DOAnchorPos(origin - new Vector2(canvasRect.rect.width, 0), duration)
                           .SetEase(Ease.OutCubic).ToUniTask();
                break;
            }
            case "slide_right":
            {
                Vector2 origin = rect.anchoredPosition - new Vector2(canvasRect.rect.width, 0);
                rect.anchoredPosition = origin;
                img.gameObject.SetActive(true);
                await rect.DOAnchorPos(origin + new Vector2(canvasRect.rect.width, 0), duration)
                           .SetEase(Ease.OutCubic).ToUniTask();
                break;
            }
            case "slide_up":
            {
                Vector2 origin = rect.anchoredPosition - new Vector2(0, canvasRect.rect.height);
                rect.anchoredPosition = origin;
                    img.gameObject.SetActive(true);
                    await rect.DOAnchorPos(origin + new Vector2(0, canvasRect.rect.height), duration)
                           .SetEase(Ease.OutCubic).ToUniTask();
                break;
            }
            case "slide_down":
            {
                Vector2 origin = rect.anchoredPosition + new Vector2(0, canvasRect.rect.height);
                rect.anchoredPosition = origin;
                    img.gameObject.SetActive(true);
                    await rect.DOAnchorPos(origin - new Vector2(0, canvasRect.rect.height), duration)
                           .SetEase(Ease.OutCubic).ToUniTask();
                break;
            }
            case "zoom_in":
                rect.localScale = Vector3.one * 0.5f;
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await UniTask.WhenAll(
                    rect.DOScale(1f, duration).SetEase(Ease.OutBack).ToUniTask(),
                    img.DOFade(1f, duration).ToUniTask()
                );
                break;

            case "zoom_out":
                rect.localScale = Vector3.one * 1.5f;
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await UniTask.WhenAll(
                    rect.DOScale(1f, duration).SetEase(Ease.OutCubic).ToUniTask(),
                    img.DOFade(1f, duration).ToUniTask()
                );
                break;

            default:
                img.color = Color.white;
                img.gameObject.SetActive(true);
                Debug.LogWarning($"[CutsceneManager] 알 수 없는 enter type: {enterType}");
                break;
        }
    }

    // ── Exit 애니메이션 ─────────────────────────────────────────
    async UniTask ApplyExitAnimation(Image img, string exitType, float duration)
    {
        RectTransform rect = img.GetComponent<RectTransform>();

        switch (exitType)
        {
            case "cut":
                img.color = new Color(1, 1, 1, 0);
                break;

            case "fade_out":
                await img.DOFade(0f, duration).ToUniTask();
                break;

            case "slide_left":
            {
                Vector2 target = rect.anchoredPosition - new Vector2(canvasRect.rect.width, 0);
                await rect.DOAnchorPos(target, duration)
                           .SetEase(Ease.InCubic).ToUniTask();
                break;
            }
            case "slide_right":
            {
                Vector2 target = rect.anchoredPosition + new Vector2(canvasRect.rect.width, 0);
                await rect.DOAnchorPos(target, duration)
                           .SetEase(Ease.InCubic).ToUniTask();
                break;
            }
            case "slide_up":
            {
                Vector2 target = rect.anchoredPosition + new Vector2(0, canvasRect.rect.height);
                await rect.DOAnchorPos(target, duration)
                           .SetEase(Ease.InCubic).ToUniTask();
                break;
            }
            case "slide_down":
            {
                Vector2 target = rect.anchoredPosition - new Vector2(0, canvasRect.rect.height);
                await rect.DOAnchorPos(target, duration)
                           .SetEase(Ease.InCubic).ToUniTask();
                break;
            }
            case "zoom_out":
                await UniTask.WhenAll(
                    rect.DOScale(0f, duration).SetEase(Ease.InBack).ToUniTask(),
                    img.DOFade(0f, duration).ToUniTask()
                );
                break;

            default:
                await img.DOFade(0f, duration).ToUniTask();
                Debug.LogWarning($"[CutsceneManager] 알 수 없는 exit type: {exitType}, fade_out으로 대체");
                break;
        }
    }


    float GetEnterDefaultDuration(string enterType) => enterType switch
    {
        "cut"        => 0.0f,
        "fade_in"    => 0.4f,
        "slide_left" => 0.3f,
        "slide_right"=> 0.3f,
        "slide_up"   => 0.3f,
        "slide_down" => 0.3f,
        "zoom_in"    => 0.4f,
        "zoom_out"   => 0.4f,
        _            => 0.3f,
    };
    float GetExitDefaultDuration(string exitType) => exitType switch
    {
        "cut"         => 0.0f,
        "fade_out"    => 0.3f,
        "slide_left"  => 0.3f,
        "slide_right" => 0.3f,
        "slide_up"    => 0.3f,
        "slide_down"  => 0.3f,
        "zoom_out"    => 0.4f,
        _             => 0.3f,
    };

    // ── 포지션/레이아웃 ───────────────────────────────────────────────

    public void SetImagePositionPreset(Image img, PositionPreset preset)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        Vector2 anchor = anchorPreset[preset.Anchor];

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = anchor;

        float w = canvasRect.rect.width - padding_X;
        float h = canvasRect.rect.height - padding_Y;

        rect.sizeDelta        = new Vector2(w * preset.Width, h * preset.Height);
        rect.anchoredPosition = new Vector2(w * preset.OffsetX, h * preset.OffsetY);
    }
    public void SetImagesLayOut(List<Image> imgs, LayoutPreset layoutPreset)
    {
        for (int i = 0; i < imgs.Count; i++)
        {
            LayoutSlot slot = layoutPreset.Slots[i];
            PositionPreset preset = default;

            if (!string.IsNullOrEmpty(slot.Position))
            {
                preset = data.cutSceneData.PositionPresets[slot.Position];
                if (slot.WidthOverride  != null) preset.Width  = slot.WidthOverride.Value;
                if (slot.HeightOverride != null) preset.Height = slot.HeightOverride.Value;
            }
            else
            {
                preset.Anchor  = slot.Anchor.Value;
                preset.Width   = slot.Width.Value;
                preset.Height  = slot.Height.Value;
                preset.OffsetX = slot.OffsetX.Value;
                preset.OffsetY = slot.OffsetY.Value;
            }

            SetImagePositionPreset(imgs[i], preset);

            // dim 슬롯 처리: 이미지 대신 검은 반투명 오버레이로 사용
            if (slot.Dim.HasValue)
            {
                imgs[i].sprite = null;
                imgs[i].color = new Color(0, 0, 0, slot.Dim.Value);
            }
        }
    }

    
    */
    #endregion 



    // ── Image 풀 관리 ─────────────────────────────────────────────────

    Image GetPooledImage()
    {
        if (imagePool.Count == 0)
        {
            Debug.LogWarning("[CutsceneManager] 이미지 풀이 비어있음");
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
