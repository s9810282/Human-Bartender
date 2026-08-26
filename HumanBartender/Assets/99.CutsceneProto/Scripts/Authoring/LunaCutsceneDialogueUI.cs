using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    /// <summary>
    /// 컷씬 대사 표시 컨트롤러.
    /// 말풍선 모양은 프리팡, 폰트·색·여백·제한 크기는 Style 에셋이 소유한다.
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

        private void Awake()
        {
            CacheCanvas();
            HideDialogue();
            SetOverlayAlpha(flashOverlay, 0f);
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
                    "[LunaCutscene] Speech Bubble Prefab/Style이 연결되지 않았습니다. Studio의 'UI 프리팡·스타일 생성/복구'를 실행하세요.",
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
            if (!TryResolveSpeakerPosition(out Vector3 worldPosition)
                || worldCamera == null
                || !TryWorldToCanvas(worldCamera, worldPosition, out Vector2 speakerOnCanvas))
            {
                Vector2 fallback = ClampToSafeArea(bubbleStyle.unboundFallbackPosition);
                bubbleInstance.Root.anchoredPosition = fallback;
                bubbleInstance.SetTailVisible(false);
                if (!missingSpeakerWarned)
                {
                    Debug.LogWarning(
                        "[LunaCutscene] 화자 SpeechAnchor 또는 활성 카메라가 없어 말풍선을 화면 기본 위치에 표시합니다.",
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
    }
}
