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

    public TypingData()
    {
    }

    public TypingData(string str, string speaker, Vector3 speakerPos, Color32 nameColor, bool isLunaSpeak)
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

        string cleanSentence = rawSentence;
        Dictionary<int, float> delayDict = new Dictionary<int, float>();
        Regex tagRegex = new Regex(@"<(\d+)>");
        MatchCollection matches = tagRegex.Matches(rawSentence);

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
            targetBubble.textLabel.enableAutoSizing = false;
            targetBubble.textLabel.fontSize = targetBubble.baseFontSize;
            targetBubble.textLabel.text = cleanSentence;
            targetBubble.BeginTyping();
        }

        targetBubble.textLabel.maxVisibleCharacters = 0;
        targetBubble.textLabel.ForceMeshUpdate();

        int totalVisibleChars = targetBubble.textLabel.textInfo.characterCount;

        try
        {
            for (int i = 0; i <= totalVisibleChars; i++)
            {
                targetBubble.textLabel.maxVisibleCharacters = i;

                if (targetBubble != null)
                {
                    string visiblePart = cleanSentence.Substring(0, i);
                    if (i < cleanSentence.Length && cleanSentence[i] == '\n')
                        visiblePart = cleanSentence.Substring(0, i + 1);

                    targetBubble.ResizeToFit(visiblePart);
                }

                if (delayDict.ContainsKey(i))
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delayDict[i]), cancellationToken: token);
                }

                if (i < totalVisibleChars)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(defaultTypingDelay), cancellationToken: token);
                }
            }
        }
        catch (Exception ex)
        {
            targetBubble.textLabel.maxVisibleCharacters = targetBubble.textLabel.textInfo.characterCount;

            if (targetBubble != null)
                targetBubble.ResizeToFit(cleanSentence);
        }

        CompleteTyping();
    }
}
