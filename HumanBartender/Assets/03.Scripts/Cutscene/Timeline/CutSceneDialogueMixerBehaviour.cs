using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutSceneDialogueMixerBehaviour : PlayableBehaviour
{
    private CutSceneTimelineManager manager;

    static readonly System.Collections.Generic.Dictionary<AnchorType, Vector2> AnchorMap = new()
    {
        { AnchorType.Center,      new Vector2(0.5f, 0.5f) },
        { AnchorType.Left,        new Vector2(0.0f, 0.5f) },
        { AnchorType.Right,       new Vector2(1.0f, 0.5f) },
        { AnchorType.TopLeft,     new Vector2(0.0f, 1.0f) },
        { AnchorType.TopRight,    new Vector2(1.0f, 1.0f) },
        { AnchorType.BottomLeft,  new Vector2(0.0f, 0.0f) },
        { AnchorType.BottomRight, new Vector2(1.0f, 0.0f) },
    };

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null) return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<CutSceneDialogueBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;

            if (weight > 0f && !behaviour.isActive)
            {
                // ── 클립 시작 → 말풍선 등장 ──────────────────────────
                behaviour.isActive = true;
                behaviour.exitStarted = false;
                behaviour.enterDone = false;
                behaviour.lastVisibleCount = 0;
                behaviour.prevVisibleCount = -1;

                var bubble = manager.GetDialogueBubble(behaviour.bubbleIndex);
                if (bubble == null) continue;

                SetupBubble(bubble, behaviour);
                PlayBubbleEnter(bubble, behaviour).Forget();
            }
            else if (weight > 0f && behaviour.isActive)
            {
                // ── 클립 진행 중 → 타이핑 + 리사이즈 ─────────────────
                var bubble = manager.GetDialogueBubble(behaviour.bubbleIndex);
                if (bubble == null) continue;

                UpdateTyping(bubble, behaviour, inputPlayable);

                // 대상 이미지 추적
                if (!string.IsNullOrEmpty(behaviour.targetImagePath))
                    FollowTargetImage(bubble, behaviour);
            }
            else if (weight <= 0f && behaviour.isActive)
            {
                // ── 클립 종료 → 말풍선 퇴장 ──────────────────────────
                behaviour.isActive = false;

                var bubble = manager.GetDialogueBubble(behaviour.bubbleIndex);
                if (bubble == null) continue;

                if (!behaviour.exitStarted)
                {
                    behaviour.exitStarted = true;
                    PlayBubbleExit(bubble, behaviour).Forget();
                }
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  세팅
    // ══════════════════════════════════════════════════════════════════

    void SetupBubble(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b)
    {
        // 이름
        if (bubble.nameLabel != null)
        {
            if (!string.IsNullOrEmpty(b.speakerName))
            {
                bubble.nameLabel.text = b.speakerName;
                bubble.nameLabel.gameObject.SetActive(true);
            }
            else
            {
                bubble.nameLabel.gameObject.SetActive(false);
            }
        }

        // 텍스트 초기화 — 전체 텍스트를 세팅하되 아직 안 보이게
        bubble.textLabel.text = b.text;
        bubble.textLabel.maxVisibleCharacters = 0;
        bubble.BeginTyping();

        // 위치
        PositionBubble(bubble, b);

        // 초기 상태 (숨김)
        RectTransform rect = bubble.GetComponent<RectTransform>();
        rect.localScale = Vector3.zero;
        bubble.gameObject.SetActive(true);
    }

    void PositionBubble(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b)
    {
        RectTransform rect = bubble.GetComponent<RectTransform>();
        RectTransform canvasRect = manager.CanvasRect;
        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;

        // 대상 이미지가 있으면 그 위치 기준
        if (!string.IsNullOrEmpty(b.targetImagePath))
        {
            Image targetImg = manager.GetActiveImage(b.targetImagePath);
            if (targetImg != null)
            {
                RectTransform targetRect = targetImg.GetComponent<RectTransform>();
                rect.anchorMin = targetRect.anchorMin;
                rect.anchorMax = targetRect.anchorMax;
                rect.pivot = new Vector2(0.5f, 0f); // 말풍선 하단이 캐릭터 위

                rect.anchoredPosition = targetRect.anchoredPosition
                    + new Vector2(w * b.offset.x, h * b.offset.y);
                return;
            }
        }

        // 고정 위치
        Vector2 anchorVec = AnchorMap.TryGetValue(b.anchor, out var v) ? v : new Vector2(0.5f, 0.5f);
        rect.anchorMin = anchorVec;
        rect.anchorMax = anchorVec;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(w * b.offset.x, h * b.offset.y);
    }

    void FollowTargetImage(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b)
    {
        Image targetImg = manager.GetActiveImage(b.targetImagePath);
        if (targetImg == null) return;

        RectTransform rect = bubble.GetComponent<RectTransform>();
        RectTransform targetRect = targetImg.GetComponent<RectTransform>();
        RectTransform canvasRect = manager.CanvasRect;

        float w = canvasRect.rect.width;
        float h = canvasRect.rect.height;
        rect.anchoredPosition = targetRect.anchoredPosition
            + new Vector2(w * b.offset.x, h * b.offset.y);
    }

    // ══════════════════════════════════════════════════════════════════
    //  타이핑 + 실시간 리사이즈
    // ══════════════════════════════════════════════════════════════════

    void UpdateTyping(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b,
                      ScriptPlayable<CutSceneDialogueBehaviour> playable)
    {
        float elapsed = (float)playable.GetTime();
        float typingStart = b.enterDuration + b.typingDelay;

        if (elapsed < typingStart)
        {
            bubble.textLabel.maxVisibleCharacters = 0;
            return;
        }

        float typingElapsed = elapsed - typingStart;
        int totalChars = b.text.Length;
        int visibleChars = Mathf.Min(Mathf.FloorToInt(typingElapsed * b.charsPerSecond), totalChars);

        // 글자 수가 변했을 때만 리사이즈 (매 프레임 호출 방지)
        if (visibleChars != b.prevVisibleCount)
        {
            b.prevVisibleCount = visibleChars;
            bubble.textLabel.maxVisibleCharacters = visibleChars;

            // 현재까지 보이는 텍스트로 말풍선 크기 재계산
            string visibleText = b.text.Substring(0, visibleChars);
            bubble.ResizeToFit(visibleText);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  등장 / 퇴장
    // ══════════════════════════════════════════════════════════════════

    async UniTaskVoid PlayBubbleEnter(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b)
    {
        RectTransform rect = bubble.GetComponent<RectTransform>();
        rect.DOKill();

        switch (b.enterType)
        {
            case EEneterPreset.ScaleUp:
                rect.localScale = Vector3.zero;
                await rect.DOScale(1f, b.enterDuration).SetEase(b.enterEase).ToUniTask();
                break;

            case EEneterPreset.FadeIn:
                var cg = GetOrAddCanvasGroup(bubble.gameObject);
                cg.alpha = 0f;
                rect.localScale = Vector3.one;
                await DOTween.To(() => cg.alpha, x => cg.alpha = x, 1f, b.enterDuration).ToUniTask();
                break;

            case EEneterPreset.SlideUp:
            {
                float h = manager.CanvasRect.rect.height;
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest - new Vector2(0, h * 0.1f);
                rect.localScale = Vector3.one;
                await rect.DOAnchorPos(dest, b.enterDuration).SetEase(b.enterEase).ToUniTask();
                break;
            }
            case EEneterPreset.SlideDown:
            {
                float h = manager.CanvasRect.rect.height;
                Vector2 dest = rect.anchoredPosition;
                rect.anchoredPosition = dest + new Vector2(0, h * 0.1f);
                rect.localScale = Vector3.one;
                await rect.DOAnchorPos(dest, b.enterDuration).SetEase(b.enterEase).ToUniTask();
                break;
            }

            case EEneterPreset.Cut:
                rect.localScale = Vector3.one;
                break;

            default:
                rect.localScale = Vector3.one;
                break;
        }

        b.enterDone = true;
    }

    async UniTaskVoid PlayBubbleExit(DynamicSpeechBubble bubble, CutSceneDialogueBehaviour b)
    {
        RectTransform rect = bubble.GetComponent<RectTransform>();
        rect.DOKill();

        switch (b.exitType)
        {
            case EExitPreset.ScaleDown:
                await rect.DOScale(0f, b.exitDuration).SetEase(b.exitEase).ToUniTask();
                break;

            case EExitPreset.FadeOut:
                var cg = GetOrAddCanvasGroup(bubble.gameObject);
                await DOTween.To(() => cg.alpha, x => cg.alpha = x, 0f, b.exitDuration).ToUniTask();
                cg.alpha = 1f; // 복구
                break;

            case EExitPreset.Cut:
                break;

            default:
                rect.localScale = Vector3.zero;
                break;
        }

        // 정리
        bubble.gameObject.SetActive(false);
        bubble.textLabel.text = "";
        bubble.textLabel.maxVisibleCharacters = 99999;
        rect.localScale = Vector3.one;
    }

    // ══════════════════════════════════════════════════════════════════
    //  유틸
    // ══════════════════════════════════════════════════════════════════

    CanvasGroup GetOrAddCanvasGroup(GameObject go)
    {
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        return cg;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (manager != null)
            manager.HideAllDialogueBubbles();
    }
}
