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

/// <summary>말풍선 타이핑에 필요한 데이터(대사 내용, 화자, 위치, 색상 등)를 담는 컨테이너.</summary>
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


/// <summary>
/// 루나(플레이어)/손님 말풍선 두 개를 관리하며 텍스트 타이핑 연출을 담당한다.
/// DynamicBubbleEffect와 태그 파싱·타이핑 로직이 거의 동일하게 중복 구현되어 있으니 함께 참고할 것.
/// </summary>
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

    [SerializeField] private float defaultTypingDelay = 0.05f;

    void Start()
    {
        curTypingData = new TypingData();
    }

    DynamicSpeechBubble targetBubble;


    /// <summary>
    /// 화자에 맞는 말풍선(루나/손님)을 선택하고, 손님 발화면 캐릭터 위치에 맞춰 말풍선 위치를 조정한 뒤 타이핑을 시작한다.
    /// </summary>
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

    /// <summary>캐릭터의 월드 좌표를 화면 좌표로 변환해 말풍선 위치를 캐릭터 머리 위(subOffset)로 맞춘다.</summary>
    public void SetBubblePosition(Vector3 characterTransform)
    {
        Vector2 screenPoint = Camera.main.WorldToScreenPoint(characterTransform + (Vector3)subOffset);

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

        localPoint.x = Mathf.RoundToInt(localPoint.x);
        localPoint.y = Mathf.RoundToInt(localPoint.y);
        targetBubble.bubble.localPosition = localPoint;
    }



    /// <summary>두 말풍선의 텍스트와 폰트 크기를 초기화하고 비활성화한다.</summary>
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

    /// <summary>화면 클릭 시 타이핑을 중단(스킵)한다.</summary>
    public void OnScreenClick()
    {
        StopTyping();
    }

    /// <summary>타이핑 완료 후 현재 타이핑 데이터 참조를 정리한다.</summary>
    public void CompleteTyping()
    {
        curTypingData = null;
    }

    /// <summary>진행 중인 타이핑 코루틴을 취소한다.</summary>
    public void StopTyping()
    {
        if (typingCts != null)
        {
            typingCts.Cancel();
            typingCts.Dispose();
            typingCts = null;
        }
    }

    /// <summary>
    /// 말풍선에 한 글자씩 순차 표시(타이핑 효과)한다. 문장 안의 "&lt;숫자&gt;" 태그는 해당 위치에서
    /// 지정 시간(ms)만큼 추가 딜레이를 주는 용도로 파싱되어 제거된다. DynamicBubbleEffect.TypeSentenceTMP와 로직이 동일.
    /// </summary>
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


    /// <summary>TextTagDataSO에 등록된 커스텀 태그(&lt;key&gt;...&lt;/key&gt;)를 TMP의 &lt;color&gt; 태그로 치환한다.</summary>
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
    /// <summary>표시 중인 글자 수(visibleCharCount)까지의 부분 문자열을 잘라 반환한다. (현재 미사용)</summary>
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
