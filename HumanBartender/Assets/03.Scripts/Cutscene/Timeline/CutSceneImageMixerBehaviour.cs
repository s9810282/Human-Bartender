using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutSceneImageMixerBehaviour : PlayableBehaviour
{
    // Mixer가 Track에서 받아오는 바인딩
    internal CutSceneTimelineManager manager;

    // ══════════════════════════════════════════════════════════════════
    //  매 프레임 — 클립 활성/비활성 감지 후 Enter/Exit 트리거
    // ══════════════════════════════════════════════════════════════════

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

                // 스프라이트 로드: sheetPath가 있으면 시트에서 한 장, 없으면 단일 이미지
                Sprite sprite = LoadSprite(behaviour);
                if (sprite != null) img.sprite = sprite;
                img.SetNativeSize();

                // activeImages 등록 키 결정
                string activeKey = GetActiveKey(behaviour);

                manager.SetImagePosition(img, behaviour.anchor, behaviour.offsetX, behaviour.offsetY);
                behaviour.assignedImage = img;
                manager.RegisterActive(activeKey, img);

                PlayEnter(img, behaviour).Forget();
            }
            else if (weight <= 0f && behaviour.isActive)
            {
                // ── 클립 종료 → Exit ──────────────────────────────────
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
        // Timeline 정지 시 잔여 이미지 정리
        if (manager != null)
            manager.ResetImages();
    }

    // ══════════════════════════════════════════════════════════════════
    //  Enter 애니메이션
    // ══════════════════════════════════════════════════════════════════

    async UniTaskVoid PlayEnter(Image img, CutSceneImageBehaviour b)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = b.enterDuration;
        float dist     = b.enterSlideDistance > 0 ? b.enterSlideDistance : 1f;
        Ease  ease     = b.enterEase == Ease.Unset ? Ease.OutCubic : b.enterEase;
        RectTransform canvasRect = manager.CanvasRect;

        switch (b.enterType)
        {
            case EEneterPreset.Cut:
                img.color = Color.white;
                img.gameObject.SetActive(true);
                break;

            case EEneterPreset.FadeIn:
                img.color = new Color(1, 1, 1, 0);
                img.gameObject.SetActive(true);
                await img.DOFade(1f, duration)
                         .SetEase(b.enterEase == Ease.Unset ? Ease.Linear : b.enterEase)
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
                Ease scaleEase = b.enterEase == Ease.Unset ? Ease.OutBack : b.enterEase;
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
                break;
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Exit 애니메이션
    // ══════════════════════════════════════════════════════════════════

    async UniTaskVoid PlayExit(Image img, CutSceneImageBehaviour b)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        float duration = b.exitDuration;
        float dist     = b.exitSlideDistance > 0 ? b.exitSlideDistance : 1f;
        Ease  ease     = b.exitEase == Ease.Unset ? Ease.InCubic : b.exitEase;
        RectTransform canvasRect = manager.CanvasRect;

        switch (b.exitType)
        {
            case EExitPreset.Cut:
                img.color = new Color(1, 1, 1, 0);
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

        // 풀에 반환
        manager.ReturnToPool(GetActiveKey(b), img);
        b.assignedImage = null;
    }

    // ══════════════════════════════════════════════════════════════════
    //  스프라이트 로드 헬퍼
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// sheetPath가 있으면 시트에서 frameIndex 번째 로드.
    /// 없으면 Resources/Cutscenes/{imagePath}에서 단일 이미지 로드.
    /// </summary>
    Sprite LoadSprite(CutSceneImageBehaviour b)
    {
        // 시트에서 한 장
        if (!string.IsNullOrEmpty(b.sheetPath))
        {
            Sprite[] allSprites = Resources.LoadAll<Sprite>($"Cutscenes/{b.sheetPath}");
            if (allSprites == null || allSprites.Length == 0)
            {
                Debug.LogWarning($"[ImageTrack] 시트를 찾을 수 없음: {b.sheetPath}");
                return null;
            }

            System.Array.Sort(allSprites, (a, c) =>
            {
                int numA = int.Parse(a.name.Substring(a.name.LastIndexOf('_') + 1));
                int numC = int.Parse(c.name.Substring(c.name.LastIndexOf('_') + 1));
                return numA.CompareTo(numC);
            });

            int idx = Mathf.Clamp(b.frameIndex, 0, allSprites.Length - 1);
            return allSprites[idx];
        }

        // 단일 이미지
        if (!string.IsNullOrEmpty(b.imagePath))
            return Resources.Load<Sprite>($"Cutscenes/{b.imagePath}");

        return null;
    }

    /// <summary>
    /// activeImages 키는 항상 imagePath.
    /// 다른 트랙에서 참조할 때 imagePath만 맞추면 됨.
    /// </summary>
    string GetActiveKey(CutSceneImageBehaviour b)
    {
        return b.imagePath;
    }
}
