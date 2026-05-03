using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutSceneImageMixerBehaviour : PlayableBehaviour
{
    internal CutSceneTimelineManager manager;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<CutSceneImageBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();

            if (weight > 0f && !behaviour.isActive)
            {
                // ── 클립 시작 → Enter ─────────────────────────────────
                behaviour.isActive = true;
                behaviour.exitDone = false;

                Image img = manager.GetPooledImage();
                if (img == null) continue;

                // [수정됨] 위치 세팅 전, 눈에 보이지 않게 알파를 0으로 만들고 미리 활성화합니다.
                Color originalColor = img.color;
                img.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                img.gameObject.SetActive(true);

                Sprite sprite = Resources.Load<Sprite>($"Cutscenes/{behaviour.imagePath}");
                if (sprite != null) img.sprite = sprite;
                img.rectTransform.rotation = Quaternion.identity;
                img.SetNativeSize();

                // [수정됨] 활성화된 상태에서 위치를 세팅하여 RectTransform 계산을 정확하게 만듭니다.
                manager.SetImagePosition(img, behaviour.anchor, behaviour.offsetX, behaviour.offsetY);
                behaviour.assignedImage = img;
                manager.RegisterActive(behaviour.imagePath, img);

                PlayEnter(img, behaviour).Forget();
            }
            else if (weight <= 0f && behaviour.isActive)
            {
                behaviour.isActive = false;

                if (behaviour.assignedImage != null && !behaviour.exitDone)
                {
                    behaviour.exitDone = true;
                    PlayExit(behaviour.assignedImage, behaviour).Forget();
                }
            }
        }
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (manager != null)
            manager.ResetImages();
    }

    async UniTaskVoid PlayEnter(Image img, CutSceneImageBehaviour b)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = b.enterDuration;
        float dist = b.enterSlideDistance > 0 ? b.enterSlideDistance : 1f;
        Ease ease = b.enterEase == Ease.Unset ? Ease.OutCubic : b.enterEase;
        RectTransform canvasRect = manager.CanvasRect;

        // 알파를 1로 되돌릴 때 원래 이미지의 RGB 값을 유지하기 위한 변수
        Color targetColor = new Color(img.color.r, img.color.g, img.color.b, 1f);

        switch (b.enterType)
        {
            case EEneterPreset.Cut:
                img.color = targetColor;
                break;

            case EEneterPreset.FadeIn:
                await img.DOFade(1f, duration)
                         .SetEase(b.enterEase == Ease.Unset ? Ease.Linear : b.enterEase)
                         .ToUniTask();
                break;

            case EEneterPreset.SlideLeft:
                {
                    Vector2 dest = rect.anchoredPosition;
                    rect.anchoredPosition = dest + new Vector2(canvasRect.rect.width * dist, 0);
                    img.color = targetColor;
                    await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                    break;
                }
            case EEneterPreset.SlideRight:
                {
                    Vector2 dest = rect.anchoredPosition;
                    rect.anchoredPosition = dest - new Vector2(canvasRect.rect.width * dist, 0);
                    img.color = targetColor;
                    await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                    break;
                }
            case EEneterPreset.SlideUp:
                {
                    Vector2 dest = rect.anchoredPosition;
                    rect.anchoredPosition = dest - new Vector2(0, canvasRect.rect.height * dist);
                    img.color = targetColor;
                    await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                    break;
                }
            case EEneterPreset.SlideDown:
                {
                    Vector2 dest = rect.anchoredPosition;
                    rect.anchoredPosition = dest + new Vector2(0, canvasRect.rect.height * dist);
                    img.color = targetColor;
                    await rect.DOAnchorPos(dest, duration).SetEase(ease).ToUniTask();
                    break;
                }
            case EEneterPreset.ScaleUp:
                {
                    Ease scaleEase = b.enterEase == Ease.Unset ? Ease.OutBack : b.enterEase;
                    rect.localScale = Vector3.one * 0.5f;
                    // 페이드 처리 포함
                    await UniTask.WhenAll(
                        rect.DOScale(1f, duration).SetEase(scaleEase).ToUniTask(),
                        img.DOFade(1f, duration).ToUniTask()
                    );
                    break;
                }
            default:
                img.color = targetColor;
                break;
        }
    }


    async UniTaskVoid PlayExit(Image img, CutSceneImageBehaviour b)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = b.exitDuration;
        float dist = b.exitSlideDistance > 0 ? b.exitSlideDistance : 1f;
        Ease ease = b.exitEase == Ease.Unset ? Ease.InCubic : b.exitEase;
        RectTransform canvasRect = manager.CanvasRect;

        switch (b.exitType)
        {
            case EExitPreset.Cut:
                img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);
                break;

            case EExitPreset.FadeOut:
                await img.DOFade(0f, duration)
                         .SetEase(b.exitEase == Ease.Unset ? Ease.Linear : b.exitEase)
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
                    Ease scaleEase = b.exitEase == Ease.Unset ? Ease.InBack : b.exitEase;
                    await UniTask.WhenAll(
                        rect.DOScale(0f, duration).SetEase(scaleEase).ToUniTask(),
                        img.DOFade(0f, duration).ToUniTask()
                    );
                    break;
                }
            default:
                await img.DOFade(0f, duration).ToUniTask();
                break;
        }

        manager.ReturnToPool(b.imagePath, img);
        b.assignedImage = null;
    }
}