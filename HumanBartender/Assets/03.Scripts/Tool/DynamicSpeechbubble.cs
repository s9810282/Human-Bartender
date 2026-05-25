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

    public void ResetToMinSize()
    {
        Logger.Log("bubble Resize");
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


    public void BeginTyping()
    {
        typingMaxWidth = 0f;
        ResetToMinSize();
    }

    public void ResizeToFit(string content)
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

        float textW = Mathf.Min(longestLineW, maxTextW);
        if (textW <= 0f) textW = minSize.x - paddingH;


        textW = Mathf.Min(Mathf.Ceil(textW) + widthSafetyMargin, maxTextW);
        if (textW > typingMaxWidth) typingMaxWidth = textW;
        textW = typingMaxWidth;

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