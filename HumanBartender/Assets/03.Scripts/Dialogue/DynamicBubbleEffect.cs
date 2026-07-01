using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 말풍선 텍스트 타이핑 연출 유틸리티. UIDialogueTextView에도 거의 동일한 로직(ApplyCustomTags/TypeSentenceTMP)이
/// 중복 구현되어 있어 함께 참고할 것.
/// </summary>
public static class DynamicBubbleEffect
{
    public static TextTagDataSO textTagData;
    private static float defaultTypingDelay = 0.025f;

    /// <summary>
    /// TextTagDataSO에 등록된 커스텀 태그(&lt;key&gt;...&lt;/key&gt;)를 TMP의 &lt;color&gt; 태그로 치환한다.
    /// </summary>
    public static string ApplyCustomTags(string raw)
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
    /// <summary>
    /// 말풍선(targetBubble)에 한 글자씩 순차 표시(타이핑 효과)한다.
    /// 문장 안의 "&lt;숫자&gt;" 태그는 해당 위치에서 지정 시간(ms)만큼 추가 딜레이를 주는 용도로 파싱되어 제거된다.
    /// </summary>
    public async static UniTask TypeSentenceTMP
        (
        TypingData data,
        DynamicSpeechBubble targetBubble,
         CancellationToken token = default
        )
    {
        string rawSentence = data.str;
        if (!(rawSentence.Length > 0)) return;

        targetBubble.gameObject.SetActive(true);
        targetBubble.nameLabel.text = data.speaker;
        targetBubble.nameLabel.color = data.nameColor;

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
    }
}
