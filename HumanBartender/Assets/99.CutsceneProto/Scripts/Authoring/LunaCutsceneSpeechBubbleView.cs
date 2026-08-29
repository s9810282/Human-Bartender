using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public sealed class LunaCutsceneSpeechBubbleView : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text continueHint;
        [SerializeField] private RectTransform tailRect;
        [SerializeField] private Image tailImage;
        [SerializeField] private CanvasGroup canvasGroup;

        private LunaCutsceneSpeechBubbleStyle style;
        private bool hasSpeaker;
        private float speakerHeight;
        private float bodyHeight;

        public RectTransform Root => root != null ? root : transform as RectTransform;
        public Vector2 BubbleSize => Root != null ? Root.sizeDelta : Vector2.zero;
        public bool IsTailVisible => tailRect != null && tailRect.gameObject.activeSelf;
        public bool WasHeightClamped { get; private set; }
        public int CharacterCount { get; private set; }

        public void Configure(
            RectTransform bubbleRoot,
            Image panelImage,
            TMP_Text speaker,
            TMP_Text body,
            TMP_Text hint,
            RectTransform tail,
            Image tailGraphic,
            CanvasGroup group)
        {
            root = bubbleRoot;
            background = panelImage;
            speakerText = speaker;
            bodyText = body;
            continueHint = hint;
            tailRect = tail;
            tailImage = tailGraphic;
            canvasGroup = group;
        }

        public void Prepare(
            string speaker,
            string fullBody,
            string hint,
            LunaCutsceneSpeechBubbleStyle bubbleStyle)
        {
            style = bubbleStyle;
            if (style == null || Root == null || speakerText == null || bodyText == null || continueHint == null)
            {
                Debug.LogError("[LunaCutscene] 말풍선 프리팹 참조 또는 Style 에셋이 비어 있습니다.", this);
                return;
            }
            ApplyStyle();

            string safeSpeaker = speaker ?? string.Empty;
            string safeBody = fullBody ?? string.Empty;
            string safeHint = hint ?? string.Empty;
            hasSpeaker = !string.IsNullOrWhiteSpace(safeSpeaker);

            speakerText.text = safeSpeaker;
            bodyText.text = safeBody;
            continueHint.text = safeHint;

            float maxContentWidth = Mathf.Max(80f, style.maxWidth - style.horizontalPadding * 2f);
            float naturalBodyWidth = bodyText.GetPreferredValues(safeBody, 10000f, 10000f).x;
            float naturalSpeakerWidth = hasSpeaker
                ? speakerText.GetPreferredValues(safeSpeaker, 10000f, 10000f).x
                : 0f;
            float naturalHintWidth = continueHint.GetPreferredValues(safeHint, 10000f, 10000f).x;
            float desiredContentWidth = Mathf.Max(naturalBodyWidth, naturalSpeakerWidth, naturalHintWidth);
            float width = Mathf.Clamp(
                desiredContentWidth + style.horizontalPadding * 2f,
                style.minWidth,
                style.maxWidth);
            float contentWidth = Mathf.Max(80f, width - style.horizontalPadding * 2f);

            speakerHeight = hasSpeaker
                ? speakerText.GetPreferredValues(safeSpeaker, contentWidth, 10000f).y
                : 0f;
            bodyHeight = bodyText.GetPreferredValues(safeBody, contentWidth, 10000f).y;
            float desiredHeight = style.verticalPadding * 2f
                                  + speakerHeight
                                  + (hasSpeaker ? style.speakerBodySpacing : 0f)
                                  + bodyHeight
                                  + style.bodyHintSpacing
                                  + style.hintHeight;
            float height = Mathf.Clamp(desiredHeight, style.minHeight, style.maxHeight);
            WasHeightClamped = desiredHeight > style.maxHeight + 0.5f;

            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);
            Root.pivot = new Vector2(0.5f, 0.5f);
            Root.sizeDelta = new Vector2(width, height);
            ApplyLayout(width, height);

            // 전체 문장으로 크기를 한 번만 확정한다. 타이핑 중에는 텍스트 메시의
            // maxVisibleCharacters만 변경하므로 말풍선 가로·세로 크기가 다시 계산되지 않는다.
            bodyText.text = safeBody;
            bodyText.maxVisibleCharacters = 0;
            bodyText.ForceMeshUpdate();
            CharacterCount = bodyText.textInfo.characterCount;
            SetVisible(true);
        }

        public void SetVisibleCharacterCount(int count)
        {
            if (bodyText != null)
                bodyText.maxVisibleCharacters = Mathf.Clamp(count, 0, CharacterCount);
        }

        public void RevealAll()
        {
            if (bodyText != null)
                bodyText.maxVisibleCharacters = int.MaxValue;
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(visible);
        }

        public void SetTailVisible(bool visible)
        {
            if (tailRect != null)
                tailRect.gameObject.SetActive(visible);
        }

        public void SetTailTarget(Vector2 targetOnCanvas, Vector2 bubbleOnCanvas)
        {
            if (tailRect == null || style == null)
                return;

            Vector2 size = BubbleSize;
            float localX = Mathf.Clamp(
                targetOnCanvas.x - bubbleOnCanvas.x,
                -size.x * 0.5f + style.tailEdgeInset,
                size.x * 0.5f - style.tailEdgeInset);
            bool targetIsBelow = targetOnCanvas.y <= bubbleOnCanvas.y;
            float localY = targetIsBelow
                ? -size.y * 0.5f - style.tailSize.y * 0.25f
                : size.y * 0.5f + style.tailSize.y * 0.25f;

            tailRect.anchorMin = tailRect.anchorMax = new Vector2(0.5f, 0.5f);
            tailRect.pivot = new Vector2(0.5f, 0.5f);
            tailRect.sizeDelta = style.tailSize;
            tailRect.anchoredPosition = new Vector2(localX, localY);
            tailRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tailRect.SetAsFirstSibling();
            tailRect.gameObject.SetActive(true);
        }

        private void ApplyStyle()
        {
            if (style == null)
                return;
            TMP_FontAsset font = style.font != null ? style.font : TMP_Settings.defaultFontAsset;
            ApplyTextStyle(speakerText, font, style.speakerFontSize, style.speakerColor, FontStyles.Bold);
            ApplyTextStyle(bodyText, font, style.bodyFontSize, style.bodyColor, FontStyles.Normal);
            ApplyTextStyle(continueHint, font, style.hintFontSize, style.hintColor, FontStyles.Normal);
            speakerText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            continueHint.alignment = TextAlignmentOptions.BottomRight;
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;
            bodyText.lineSpacing = style.lineSpacing;
            speakerText.textWrappingMode = TextWrappingModes.NoWrap;
            continueHint.textWrappingMode = TextWrappingModes.NoWrap;
            if (background != null)
                background.color = style.backgroundColor;
            if (tailImage != null)
                tailImage.color = style.tailColor;
        }

        private void ApplyLayout(float width, float height)
        {
            float left = style.horizontalPadding;
            float right = style.horizontalPadding;
            float top = style.verticalPadding;
            float bottom = style.verticalPadding;
            float hintTop = bottom + style.hintHeight;
            float bodyTop = top + speakerHeight + (hasSpeaker ? style.speakerBodySpacing : 0f);

            // 기본 Prefab은 루트 Image 자체를 배경으로 사용한다. 이 RectTransform까지
            // Stretch하면 위에서 확정한 말풍선 sizeDelta가 (0, 0)으로 덮어써진다.
            // 별도 배경 자식을 사용하는 커스텀 Prefab일 때만 내부를 가득 채운다.
            if (background != null && background.rectTransform != Root)
                Stretch(background.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            SetOffsets(
                speakerText.rectTransform,
                left,
                height - top - speakerHeight,
                right,
                top);
            speakerText.gameObject.SetActive(hasSpeaker);
            SetOffsets(
                bodyText.rectTransform,
                left,
                hintTop + style.bodyHintSpacing,
                right,
                bodyTop);
            SetOffsets(
                continueHint.rectTransform,
                left,
                bottom,
                right,
                height - hintTop);
        }

        private static void ApplyTextStyle(
            TMP_Text text,
            TMP_FontAsset font,
            float size,
            Color color,
            FontStyles fontStyle)
        {
            if (text == null)
                return;
            if (font != null)
                text.font = font;
            text.fontSize = size;
            text.color = color;
            text.fontStyle = fontStyle;
            text.raycastTarget = false;
            text.margin = Vector4.zero;
        }

        private static void SetOffsets(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Stretch(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
