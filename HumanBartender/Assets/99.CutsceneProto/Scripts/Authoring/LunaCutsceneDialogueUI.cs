using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    /// <summary>
    /// 컷씬 대사 표시 컨트롤러.
    /// 말풍선 모양은 프리팹, 폰트·색·여백·제한 크기는 Style 에셋이 소유한다.
    /// </summary>
    public sealed class LunaCutsceneDialogueUI : MonoBehaviour
    {
        [Header("Speech Bubble Assets")]
        [SerializeField] private RectTransform bubbleLayer;
        [SerializeField] private LunaCutsceneSpeechBubbleView bubblePrefab;
        [SerializeField] private LunaCutsceneSpeechBubbleStyle bubbleStyle;

        [Header("Common Cutscene UI")]
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private RectTransform letterboxTop;
        [SerializeField] private RectTransform letterboxBottom;
        [Tooltip("특정 카메라를 고정할 때만 사용한다. 비우면 활성 카메라 전환을 자동 추적한다.")]
        [SerializeField] private Camera explicitWorldCamera;

        private Coroutine typingRoutine;
        private LunaCutsceneSpeechBubbleView bubbleInstance;
        private RectTransform canvasRect;
        private GameObject trackedSpeaker;
        private LunaCutsceneSpeechAnchor trackedAnchor;
        private Camera preferredWorldCamera;
        private bool missingSpeakerWarned;

        public bool IsTyping { get; private set; }
        public bool IsVisible => bubbleInstance != null && bubbleInstance.gameObject.activeSelf;
        public LunaCutsceneSpeechBubbleStyle BubbleStyle => bubbleStyle;
        public LunaCutsceneSpeechBubbleView BubblePrefab => bubblePrefab;
        public RectTransform BubbleLayer => bubbleLayer;
        public LunaCutsceneSpeechBubbleView ActiveBubble => bubbleInstance;
        public RectTransform LetterboxTop => letterboxTop;
        public RectTransform LetterboxBottom => letterboxBottom;
        public bool IsFadeBlack => fadeOverlay != null && fadeOverlay.color.a >= 0.999f;

        public void Configure(
            RectTransform layer,
            LunaCutsceneSpeechBubbleView prefab,
            LunaCutsceneSpeechBubbleStyle style,
            Image fade,
            Image flash,
            TMP_Text status,
            Camera camera = null)
        {
            bubbleLayer = layer;
            bubblePrefab = prefab;
            bubbleStyle = style;
            fadeOverlay = fade;
            flashOverlay = flash;
            statusText = status;
            explicitWorldCamera = camera;
            CacheCanvas();
        }

        public void ConfigurePresentation(RectTransform top, RectTransform bottom)
        {
            letterboxTop = top;
            letterboxBottom = bottom;
            ArrangePresentationOrder();
        }

        private void Awake()
        {
            CacheCanvas();
            HideDialogue();
            SetOverlayAlpha(flashOverlay, 0f);
            EnsurePresentationUi();
            SetLetterboxHidden();
        }

        public void ShowDialogue(
            LunaLocalizedLine line,
            bool english,
            float typeInterval,
            GameObject speakerTarget,
            Camera camera)
        {
            if (line == null || !EnsureBubble())
                return;
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);

            TrackSpeaker(speakerTarget, camera);
            string speaker = english ? line.speakerEn : line.speakerKo;
            string body = english ? line.textEn : line.textKo;
            string hint = english ? "SPACE / CLICK  NEXT" : "SPACE / 클릭  다음";
            bubbleInstance.gameObject.SetActive(true);
            bubbleInstance.Prepare(speaker, body, hint, bubbleStyle);
            if (bubbleInstance.WasHeightClamped)
            {
                Debug.LogWarning(
                    $"[LunaCutscene] 대사가 말풍선 최대 높이({bubbleStyle.maxHeight:0}px)를 넘어 줄임 표시됩니다: {body}",
                    this);
            }
            UpdateBubblePosition();
            typingRoutine = StartCoroutine(TypeText(Mathf.Max(0.005f, typeInterval)));
        }

        public void RevealImmediately()
        {
            if (!IsTyping || bubbleInstance == null)
                return;
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            typingRoutine = null;
            bubbleInstance.RevealAll();
            IsTyping = false;
        }

        public void HideDialogue()
        {
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            typingRoutine = null;
            IsTyping = false;
            if (bubbleInstance != null)
                bubbleInstance.SetVisible(false);
            trackedSpeaker = null;
            trackedAnchor = null;
            preferredWorldCamera = null;
            missingSpeakerWarned = false;
        }

        private void LateUpdate()
        {
            if (IsVisible)
                UpdateBubblePosition();
        }

        private bool EnsureBubble()
        {
            if (bubbleInstance != null)
                return true;
            if (bubblePrefab == null || bubbleStyle == null)
            {
                Debug.LogError(
                    "[LunaCutscene] Speech Bubble Prefab/Style이 연결되지 않았습니다. Studio의 'UI 프리팹·스타일 생성/복구'를 실행하세요.",
                    this);
                return false;
            }
            if (bubbleLayer == null)
            {
                Debug.LogError("[LunaCutscene] Speech Bubble Layer가 연결되지 않았습니다.", this);
                return false;
            }

            bubbleInstance = Instantiate(bubblePrefab, bubbleLayer, false);
            bubbleInstance.name = bubblePrefab.name;
            bubbleInstance.SetVisible(false);
            return true;
        }

        private void TrackSpeaker(GameObject speakerTarget, Camera camera)
        {
            trackedSpeaker = speakerTarget;
            trackedAnchor = speakerTarget != null
                ? speakerTarget.GetComponentInChildren<LunaCutsceneSpeechAnchor>(true)
                : null;
            preferredWorldCamera = camera;
            missingSpeakerWarned = false;
        }

        private void UpdateBubblePosition()
        {
            if (bubbleInstance == null || bubbleStyle == null)
                return;
            CacheCanvas();
            if (canvasRect == null)
                return;

            Camera worldCamera = ResolveActiveWorldCamera();
            bool hasSpeakerPosition = TryResolveSpeakerPosition(out Vector3 worldPosition);
            Vector2 speakerOnCanvas = default;
            bool hasCanvasPosition = hasSpeakerPosition
                                     && worldCamera != null
                                     && TryWorldToCanvas(worldCamera, worldPosition, out speakerOnCanvas);
            if (!hasCanvasPosition)
            {
                Vector2 fallback = ClampToSafeArea(bubbleStyle.unboundFallbackPosition);
                bubbleInstance.Root.anchoredPosition = fallback;
                bubbleInstance.SetTailVisible(false);
                if (!missingSpeakerWarned)
                {
                    Vector3 screenPosition = worldCamera != null && hasSpeakerPosition
                        ? worldCamera.WorldToScreenPoint(worldPosition)
                        : default;
                    Debug.LogWarning(
                        "[LunaCutscene] 화자 말풍선 위치 계산에 실패해 화면 기본 위치를 사용합니다. "
                        + $"speaker={(trackedSpeaker != null ? trackedSpeaker.name : "null")}, "
                        + $"speakerActive={trackedSpeaker != null && trackedSpeaker.activeInHierarchy}, "
                        + $"anchor={(trackedAnchor != null ? trackedAnchor.name : "null")}, "
                        + $"camera={(worldCamera != null ? worldCamera.name : "null")}, "
                        + $"cameraActive={IsUsableCamera(worldCamera)}, "
                        + $"world={worldPosition}, screen={screenPosition}",
                        this);
                    missingSpeakerWarned = true;
                }
                return;
            }

            Vector2 desired = speakerOnCanvas
                              + bubbleStyle.bubbleScreenOffset
                              + Vector2.up * (bubbleInstance.BubbleSize.y * 0.5f);
            Vector2 clamped = ClampToSafeArea(desired);
            bubbleInstance.Root.anchoredPosition = clamped;
            bubbleInstance.SetTailTarget(speakerOnCanvas, clamped);
        }

        private bool TryResolveSpeakerPosition(out Vector3 position)
        {
            position = default;
            if (trackedSpeaker == null || !trackedSpeaker.activeInHierarchy)
                return false;
            if (trackedAnchor != null && trackedAnchor.gameObject.activeInHierarchy)
            {
                position = trackedAnchor.WorldPosition;
                return true;
            }

            // 구 씬의 안전 폴백. 스프라이트 경계를 매 프레임 재계산하지 않는다.
            position = trackedSpeaker.transform.position;
            return true;
        }

        private Camera ResolveActiveWorldCamera()
        {
            if (IsUsableCamera(explicitWorldCamera))
                return explicitWorldCamera;

            Camera best = null;
            foreach (Camera candidate in Camera.allCameras)
            {
                if (!IsUsableCamera(candidate))
                    continue;
                if (best == null || candidate.depth > best.depth)
                    best = candidate;
            }
            if (best != null)
                return best;
            return IsUsableCamera(preferredWorldCamera) ? preferredWorldCamera : null;
        }

        private static bool IsUsableCamera(Camera camera)
        {
            return camera != null && camera.enabled && camera.gameObject.activeInHierarchy;
        }

        private bool TryWorldToCanvas(Camera camera, Vector3 worldPosition, out Vector2 localPosition)
        {
            localPosition = default;
            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition);
            if (screenPosition.z <= 0f)
                return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                null,
                out localPosition);
        }

        private Vector2 ClampToSafeArea(Vector2 position)
        {
            Vector2 size = bubbleInstance != null ? bubbleInstance.BubbleSize : Vector2.zero;
            Vector2 half = size * 0.5f;
            Rect safe = ResolveCanvasSafeArea();
            float padding = bubbleStyle != null ? bubbleStyle.safeAreaPadding : 0f;
            float minX = safe.xMin + half.x + padding;
            float maxX = safe.xMax - half.x - padding;
            float minY = safe.yMin + half.y + padding;
            float maxY = safe.yMax - half.y - padding;
            position.x = minX <= maxX ? Mathf.Clamp(position.x, minX, maxX) : safe.center.x;
            position.y = minY <= maxY ? Mathf.Clamp(position.y, minY, maxY) : safe.center.y;
            return position;
        }

        private Rect ResolveCanvasSafeArea()
        {
            if (canvasRect == null)
                return default;
            Rect screenSafe = Screen.safeArea;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenSafe.min, null, out Vector2 min)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenSafe.max, null, out Vector2 max))
            {
                return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            }
            return canvasRect.rect;
        }

        private void CacheCanvas()
        {
            Canvas canvas = GetComponent<Canvas>();
            canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : transform as RectTransform;
        }

        public void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }

        public IEnumerator Fade(bool toBlack, float duration)
        {
            float from = fadeOverlay != null ? fadeOverlay.color.a : 0f;
            float to = toBlack ? 1f : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOverlayAlpha(fadeOverlay, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
                yield return null;
            }
            SetOverlayAlpha(fadeOverlay, to);
        }

        public IEnumerator Flash(Color color, float duration)
        {
            if (flashOverlay == null)
                yield break;

            Color baseColor = color;
            float half = Mathf.Max(0.03f, duration * 0.5f);
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                baseColor.a = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(elapsed / half));
                flashOverlay.color = baseColor;
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                baseColor.a = Mathf.Lerp(0.9f, 0f, Mathf.Clamp01(elapsed / half));
                flashOverlay.color = baseColor;
                yield return null;
            }
            SetOverlayAlpha(flashOverlay, 0f);
        }

        public void SetFadeImmediate(bool black)
        {
            SetOverlayAlpha(fadeOverlay, black ? 1f : 0f);
        }

        public void PreparePresentation(LunaCutscenePresentationPreset preset)
        {
            EnsurePresentationUi();
            if (letterboxTop == null || letterboxBottom == null)
                return;

            bool active = preset != null && preset.useLetterbox;
            letterboxTop.gameObject.SetActive(active);
            letterboxBottom.gameObject.SetActive(active);
            if (!active)
                return;

            ApplyLetterboxColor(letterboxTop, preset.letterboxColor);
            ApplyLetterboxColor(letterboxBottom, preset.letterboxColor);
            SetLetterboxProgress(preset, 0f);
            ArrangePresentationOrder();
        }

        public void SetLetterboxProgress(LunaCutscenePresentationPreset preset, float progress)
        {
            if (preset == null || !preset.useLetterbox)
                return;
            EnsurePresentationUi();
            CacheCanvas();
            if (letterboxTop == null || letterboxBottom == null || canvasRect == null)
                return;

            float ratio = Mathf.Clamp(preset.letterboxHeightRatio, 0.10f, 0.15f);
            ConfigureLetterboxAnchors(letterboxTop, true, ratio);
            ConfigureLetterboxAnchors(letterboxBottom, false, ratio);

            float canvasHeight = canvasRect.rect.height > 1f ? canvasRect.rect.height : 720f;
            float hiddenOffset = canvasHeight * ratio;
            float remaining = 1f - Mathf.Clamp01(progress);
            letterboxTop.anchoredPosition = Vector2.up * (hiddenOffset * remaining);
            letterboxBottom.anchoredPosition = Vector2.down * (hiddenOffset * remaining);
        }

        public void SetLetterboxHidden()
        {
            if (letterboxTop != null)
                letterboxTop.gameObject.SetActive(false);
            if (letterboxBottom != null)
                letterboxBottom.gameObject.SetActive(false);
        }

        public void ClearTransientEffects()
        {
            StopAllCoroutines();
            typingRoutine = null;
            IsTyping = false;
            SetOverlayAlpha(flashOverlay, 0f);
        }

        private IEnumerator TypeText(float interval)
        {
            IsTyping = true;
            int count = bubbleInstance != null ? bubbleInstance.CharacterCount : 0;
            for (int index = 1; index <= count; index++)
            {
                bubbleInstance.SetVisibleCharacterCount(index);
                yield return new WaitForSecondsRealtime(interval);
            }
            bubbleInstance?.RevealAll();
            IsTyping = false;
            typingRoutine = null;
        }

        private static void SetOverlayAlpha(Image image, float alpha)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            image.raycastTarget = alpha > 0.001f;
        }

        private void EnsurePresentationUi()
        {
            CacheCanvas();
            if (canvasRect == null)
                return;
            if (letterboxTop == null)
                letterboxTop = CreateRuntimeLetterbox("LetterboxTop", true);
            if (letterboxBottom == null)
                letterboxBottom = CreateRuntimeLetterbox("LetterboxBottom", false);
            ArrangePresentationOrder();
        }

        private RectTransform CreateRuntimeLetterbox(string objectName, bool top)
        {
            Transform existing = canvasRect.Find(objectName);
            if (existing is RectTransform existingRect)
                return existingRect;

            GameObject target = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(canvasRect, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            ConfigureLetterboxAnchors(rect, top, 0.12f);
            Image image = target.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            return rect;
        }

        private void ArrangePresentationOrder()
        {
            if (letterboxTop != null)
                letterboxTop.SetAsLastSibling();
            if (letterboxBottom != null)
                letterboxBottom.SetAsLastSibling();
            if (flashOverlay != null)
                flashOverlay.transform.SetAsLastSibling();
            if (fadeOverlay != null)
                fadeOverlay.transform.SetAsLastSibling();
        }

        private static void ConfigureLetterboxAnchors(RectTransform rect, bool top, float ratio)
        {
            if (rect == null)
                return;
            rect.anchorMin = top ? new Vector2(0f, 1f - ratio) : Vector2.zero;
            rect.anchorMax = top ? Vector2.one : new Vector2(1f, ratio);
            rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void ApplyLetterboxColor(RectTransform rect, Color color)
        {
            if (rect != null && rect.TryGetComponent(out Image image))
            {
                image.color = color;
                image.raycastTarget = false;
            }
        }
    }
}
