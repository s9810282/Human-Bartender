using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutSceneBGMixerBehaviour : PlayableBehaviour
{
    private CutSceneTimelineManager manager;
    private string currentBgPath;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null || manager.BgImage == null) return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<CutSceneBGBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;

            if (weight > 0f && !behaviour.isActive)
            {
                // ── 클립 시작 → 배경 전환 ─────────────────────────────
                behaviour.isActive = true;
                behaviour.fadeOutStarted = false;

                Sprite sprite = behaviour.LoadSprite();
                if (sprite == null) continue;

                Image bg = manager.BgImage;
                bg.SetNativeSize();

                // 이전 배경과 다르면 전환
                if (behaviour.bgPath != currentBgPath)
                {
                    currentBgPath = behaviour.bgPath;
                    TransitionIn(bg, sprite, behaviour).Forget();
                }
            }
            else if (weight <= 0f && behaviour.isActive)
            {
                // ── 클립 종료 → 페이드 아웃 ───────────────────────────
                behaviour.isActive = false;

                if (!behaviour.fadeOutStarted)
                {
                    behaviour.fadeOutStarted = true;

                    // 다음 클립이 있는지 확인
                    bool hasNext = HasActiveClipAfter(playable, i);

                    if (!hasNext)
                    {
                        // 다음 배경 클립이 없으면 페이드 아웃
                        currentBgPath = null;
                        TransitionOut(manager.BgImage, behaviour).Forget();
                    }
                    // 다음 클립이 있으면 그 클립의 TransitionIn이 처리
                }
            }
        }
    }

    /// <summary> 현재 클립 이후에 활성화될 클립이 있는지 확인 </summary>
    bool HasActiveClipAfter(Playable playable, int currentIndex)
    {
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            if (i == currentIndex) continue;

            float weight = playable.GetInputWeight(i);
            if (weight > 0f) return true;

            // 아직 weight가 0이지만 바로 다음에 활성화될 클립
            var input = (ScriptPlayable<CutSceneBGBehaviour>)playable.GetInput(i);
            var b = input.GetBehaviour();
            if (!b.isActive && !b.fadeOutStarted) 
            {
                // 이 클립이 아직 시작 안 했으면 다음 클립으로 간주
                // (weight 체크만으로 충분하므로 여기서는 skip)
            }
        }
        return false;
    }

    async UniTaskVoid TransitionIn(Image bg, Sprite sprite, CutSceneBGBehaviour behaviour)
    {
        bg.DOKill();

        // 위치/크기/피벗 적용
        ApplyBGTransform(bg, behaviour);

        if (behaviour.fadeInDuration > 0)
        {
            bg.color = new Color(behaviour.tint.r, behaviour.tint.g, behaviour.tint.b, 0);
            bg.sprite = sprite;
            bg.gameObject.SetActive(true);
            await bg.DOFade(behaviour.tint.a, behaviour.fadeInDuration).ToUniTask();
        }
        else
        {
            bg.sprite = sprite;
            bg.color = behaviour.tint;
            bg.gameObject.SetActive(true);
        }
    }

    async UniTaskVoid TransitionOut(Image bg, CutSceneBGBehaviour behaviour)
    {
        bg.DOKill();

        if (behaviour.fadeOutDuration > 0)
        {
            await bg.DOFade(0f, behaviour.fadeOutDuration).ToUniTask();
            bg.gameObject.SetActive(false);
        }
        else
        {
            bg.color = new Color(1, 1, 1, 0);
            bg.gameObject.SetActive(false);
        }

        // 트랜스폼 초기화
        ResetBGTransform(bg);
        bg.sprite = null;
    }

    /// <summary>
    /// 배경 Image에 offset, scale, pivot 적용.
    /// 캔버스보다 큰 배경의 보이는 영역을 조절.
    /// </summary>
    void ApplyBGTransform(Image bg, CutSceneBGBehaviour behaviour)
    {
        RectTransform rect = bg.GetComponent<RectTransform>();
        RectTransform canvasRect = manager.CanvasRect;

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        // 피벗
        rect.pivot = behaviour.bgPivot;

        // 스케일
        rect.localScale = Vector3.one * behaviour.bgScale;

        // 오프셋 (캔버스 비율 기준)
        rect.anchoredPosition = new Vector2(
            w * behaviour.bgOffset.x,
            h * behaviour.bgOffset.y
        );
    }

    void ResetBGTransform(Image bg)
    {
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.anchoredPosition = Vector2.zero;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (manager != null)
            manager.ClearBackground();

        currentBgPath = null;
    }
}
