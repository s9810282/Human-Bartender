using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class DynamicSpeechBubble : MonoBehaviour
{
    [Header("References")]
    public TMP_Text textLabel;
    public TMP_Text nameLabel;
    public RectTransform bubble;

    [Header("Size Limits")]
    public Vector2 minSize = new Vector2(80, 40);
    public Vector2 maxSize = new Vector2(400, 240);

    [Header("Padding")]
    public float paddingLeft = 24f;
    public float paddingRight = 24f;
    public float paddingTop = 16f;
    public float paddingBottom = 16f;

    [Header("Font")]
    public float baseFontSize = 28f;
    public float minFontSize = 14f;

    [Header("Layout")]
    [Tooltip("측정 너비에 추가할 안전 마진 (wrap 경계 흔들림 방지)")]
    public float widthSafetyMargin = 4f;

    float typingMaxWidth = 0f;
    float lockedTypingWidth = -1f; // -1 = 잠금 없음 (일반 모드)

    public void ResetToMinSize()
    {
        textLabel.enableAutoSizing = false;
        textLabel.fontSize = baseFontSize;

        bubble.sizeDelta = minSize;
        LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
        textLabel.ForceMeshUpdate();
    }

    public void SetText(string content)
    {
        textLabel.enableAutoSizing = false;
        textLabel.fontSize = baseFontSize;
        textLabel.text = content;
        BeginTyping();
        ResizeToFit(content);
    }

    /// <summary>전체 텍스트를 모를 때 — 기존처럼 점진 확장.</summary>
    public void BeginTyping()
    {
        typingMaxWidth = 0f;
        lockedTypingWidth = -1f;
        ResetToMinSize();
    }

    /// <summary>전체 텍스트를 알 때 — 줄바꿈 시점에 최종 너비로 점프.</summary>
    public void BeginTyping(string fullText)
    {
        typingMaxWidth = 0f;
        ResetToMinSize();
        lockedTypingWidth = CalcTargetWidth(fullText);
    }

    /// <summary>전체 텍스트 기준 가장 긴 줄 너비 (패딩 제외, 안전 마진 포함).</summary>
    float CalcTargetWidth(string fullText)
    {
        float paddingH = paddingLeft + paddingRight;
        float maxTextW = maxSize.x - paddingH;

        float longest = 0f;
        if (!string.IsNullOrEmpty(fullText))
        {
            string[] lines = fullText.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                Vector2 size = textLabel.GetPreferredValues(line, float.PositiveInfinity, 0f);
                if (size.x > longest) longest = size.x;
            }
        }

        if (longest <= 0f) longest = minSize.x - paddingH;
        return Mathf.Min(Mathf.Ceil(longest) + widthSafetyMargin, maxTextW);
    }
    public void ResizeToFit(string content, float typingProgress = -1f)
    {
        float paddingH = paddingLeft + paddingRight;
        float paddingV = paddingTop + paddingBottom;

        float maxTextW = maxSize.x - paddingH;
        float maxTextH = maxSize.y - paddingV;

        float longestLineW = 0f;
        if (!string.IsNullOrEmpty(content))
        {
            string[] lines = content.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                Vector2 lineSize = textLabel.GetPreferredValues(line, float.PositiveInfinity, 0f);
                if (lineSize.x > longestLineW) longestLineW = lineSize.x;
            }
        }

        float naturalW = Mathf.Min(longestLineW, maxTextW);
        if (naturalW <= 0f) naturalW = minSize.x - paddingH;
        naturalW = Mathf.Min(Mathf.Ceil(naturalW) + widthSafetyMargin, maxTextW);


        float textW;
        if (lockedTypingWidth > 0f && typingProgress >= 0f)
        {
            float minW = minSize.x - paddingH;
            float progress = Mathf.Clamp01(typingProgress);
            float interpolated = Mathf.Lerp(minW, lockedTypingWidth, progress);

            textW = Mathf.Max(interpolated, naturalW);

            if (textW > typingMaxWidth) typingMaxWidth = textW;
            textW = typingMaxWidth;
        }
        else
        {
            if (naturalW > typingMaxWidth) typingMaxWidth = naturalW;
            textW = typingMaxWidth;
        }


        Vector2 wrapped = textLabel.GetPreferredValues(content, textW, 0f);

        Vector2 newSize;
        if (wrapped.y > maxTextH)
        {
            newSize = new Vector2(maxSize.x, maxSize.y);
            textLabel.enableAutoSizing = true;
            textLabel.fontSizeMin = minFontSize;
            textLabel.fontSizeMax = baseFontSize;
        }
        else
        {
            newSize = new Vector2(
                Mathf.Clamp(textW + paddingH, minSize.x, maxSize.x),
                Mathf.Clamp(wrapped.y + paddingV, minSize.y, maxSize.y)
            );
        }

        if (bubble.sizeDelta != newSize)
        {
            bubble.sizeDelta = newSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(bubble);
            textLabel.ForceMeshUpdate();
        }
    }
}