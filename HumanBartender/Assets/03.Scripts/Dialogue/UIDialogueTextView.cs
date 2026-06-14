using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[System.Serializable]
public class TypingData
{
    public string speaker = "";
    public Vector3 speakerPos;
    public string str;
    public Color32 nameColor;
    public bool isLunaSpeak = false;
    public EBubbleArrowType eBubbleArrowType = EBubbleArrowType.Center;

    public TypingData()
    {
    }

    public TypingData(
        string str, 
        string speaker, 
        Vector3 speakerPos, 
        Color32 nameColor, 
        bool isLunaSpeak,
        EBubbleArrowType eBubbleArrowType = EBubbleArrowType.Center)
    {
        this.speaker = speaker;
        this.speakerPos = speakerPos;
        this.str = str;
        this.nameColor = nameColor;
        this.isLunaSpeak = isLunaSpeak;
    }
}


public class UIDialogueTextView : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] TextTagDataSO textTagData;

    [Header("UI Components")]
    public DynamicSpeechBubble lunaSpeechBubble;
    public DynamicSpeechBubble customerSpeechBubble;
    public RectTransform canvasRect;

    [Header("Slot")]
    [SerializeField] Vector2 baseOffset;
    [SerializeField] Vector2 subOffset;



    private TypingData curTypingData;
    private CancellationTokenSource typingCts;

    private float defaultTypingDelay = 0.05f;

    void Start()
    {
        curTypingData = new TypingData();
    }

    DynamicSpeechBubble targetBubble;


    public async UniTask StartType(TypingData data)
    {
        if (data == null)
        {
            Logger.LogWarning("Typing Data is Null");
            return;
        }

        curTypingData = data;


        targetBubble = data.isLunaSpeak ? lunaSpeechBubble : customerSpeechBubble;

        if (!data.isLunaSpeak)
            SetBubblePosition(data.speakerPos);

        await TypeSentenceTMP(curTypingData);
    }

    public void SetBubblePosition(Vector3 characterTransform)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(characterTransform + (Vector3)subOffset);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

        localPoint.x = Mathf.RoundToInt(localPoint.x);
        localPoint.y = Mathf.RoundToInt(localPoint.y);
        targetBubble.bubble.localPosition = localPoint;
    }



    public void ClearText()
    {
        if (lunaSpeechBubble != null && lunaSpeechBubble.textLabel != null)
        {
            lunaSpeechBubble.textLabel.enableAutoSizing = false;
            lunaSpeechBubble.textLabel.text = "";
            lunaSpeechBubble.textLabel.fontSize = lunaSpeechBubble.baseFontSize;
            lunaSpeechBubble.gameObject.SetActive(false);
        }

        if (customerSpeechBubble != null && customerSpeechBubble.textLabel != null)
        {
            customerSpeechBubble.textLabel.enableAutoSizing = false;
            customerSpeechBubble.textLabel.text = "";
            customerSpeechBubble.textLabel.fontSize = customerSpeechBubble.baseFontSize;
            customerSpeechBubble.gameObject.SetActive(false);
        }
    }

    public void OnScreenClick()
    {
        StopTyping();
    }

    public void CompleteTyping()
    {
        curTypingData = null;
    }

    public void StopTyping()
    {
        if (typingCts != null)
        {
            typingCts.Cancel();
            typingCts.Dispose();
            typingCts = null;
        }
    }

    public async UniTask TypeSentenceTMP(TypingData data)
    {
        string rawSentence = data.str;
        if (!(rawSentence.Length > 0)) return;

        targetBubble.gameObject.SetActive(true);
        targetBubble.nameLabel.text = data.speaker;
        targetBubble.nameLabel.color = data.nameColor;

        StopTyping();
        typingCts = new CancellationTokenSource();
        CancellationToken token = typingCts.Token;

        string processed = ApplyCustomTags(rawSentence);

        string cleanSentence = processed;
        Dictionary<int, float> delayDict = new Dictionary<int, float>();
        Regex tagRegex = new Regex(@"<(\d+)>");
        MatchCollection matches = tagRegex.Matches(processed);

        int offset = 0;
        foreach (Match match in matches)
        {
            int delayMs = int.Parse(match.Groups[1].Value);
            int targetIndex = match.Index - offset;
            delayDict[targetIndex] = delayMs / 1000f;
            cleanSentence = cleanSentence.Remove(targetIndex, match.Length);
            offset += match.Length;
        }


        
        if (targetBubble != null)
        {
            targetBubble.PrepareForText(cleanSentence);
        }

        targetBubble.textLabel.maxVisibleCharacters = 0;
        int totalVisibleChars = targetBubble.TotalVisibleCharacters;


        try
        {
            for (int i = 0; i <= totalVisibleChars; i++)
            {
                targetBubble.textLabel.maxVisibleCharacters = i;
                targetBubble.UpdateForVisible(i);

                if (delayDict.ContainsKey(i))
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delayDict[i]), cancellationToken: token);

                if (i < totalVisibleChars)
                    await UniTask.Delay(System.TimeSpan.FromSeconds(defaultTypingDelay), cancellationToken: token);
            }
        }
        catch (Exception)
        {
            targetBubble.textLabel.maxVisibleCharacters = totalVisibleChars;
            targetBubble.UpdateForVisible(totalVisibleChars);
        }

        CompleteTyping();
    }


    string ApplyCustomTags(string raw)
    {
        if (textTagData == null || textTagData.textTagData?.TextTags == null)
            return raw;

        string result = raw;

        foreach (var pair in textTagData.textTagData.TextTags)
        {
            string key = pair.Key;
            string color = pair.Value.Color;
            if (string.IsNullOrEmpty(color)) continue;

            // # 보장
            if (!color.StartsWith("#")) color = "#" + color;

            // <key>...</key> 매칭 (내용은 비탐욕적으로)
            string pattern = $@"<{Regex.Escape(key)}>(.*?)</{Regex.Escape(key)}>";
            string replacement = $"<color={color}>$1</color>";

            result = Regex.Replace(result, pattern, replacement);
        }

        return result;
    }
    string GetVisibleSubstring(string fullText, int visibleCharCount)
    {
        if (visibleCharCount <= 0) return "";
        if (visibleCharCount >= targetBubble.textLabel.textInfo.characterCount)
            return fullText;

        var charInfo = targetBubble.textLabel.textInfo.characterInfo[visibleCharCount - 1];
        int endIndex = charInfo.index + 1;

        if (endIndex < fullText.Length && fullText[endIndex] == '\n')
            endIndex++;

        return fullText.Substring(0, endIndex);
    }
}
