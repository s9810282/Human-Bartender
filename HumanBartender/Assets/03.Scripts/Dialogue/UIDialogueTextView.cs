using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TypingData
{
    public string str;

    public TypingData()
    {
    }

    public TypingData(string str)
    {
        this.str = str;
    }
}


public class UIDialogueTextView : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject dialoguePanel;      // 대화창 전체 패널
    public TextMeshProUGUI nameTMPText;      // 이름 텍스트
    public TextMeshProUGUI dialogueTMPText;  // 대사 텍스트

    public Text nameText;
    public Text dialogueText;

    
    private TypingData curTypingData;
    private CancellationTokenSource typingCts;

    private float defaultTypingDelay = 0.05f;

    void Start()
    {
        curTypingData = new TypingData();
    }



    public async UniTask StartType(TypingData data)
    {
        if (data == null)
        {
            Logger.LogWarning("Typing Data is Null");
            return;
        }

        dialogueText.fontStyle = FontStyle.Normal;
        dialogueTMPText.fontStyle = FontStyles.Normal;

        curTypingData = data;
        await TypeSentence(curTypingData.str);
    }
    public void SetNameColor(Color32 color)
    {
        nameText.color = color;
        nameTMPText.color = color;
    }
    public void SetNameText(string str)
    {
        nameText.text = str;
        nameTMPText.text = str;
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

    public async UniTask TypeSentenceTMP(string rawSentence)
    {
        if (!(rawSentence.Length > 0)) return;

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

        dialogueTMPText.text = cleanSentence;
        dialogueTMPText.maxVisibleCharacters = 0;
        dialogueTMPText.ForceMeshUpdate();
        int totalVisibleChars = dialogueTMPText.textInfo.characterCount;

        try
        {
            for (int i = 0; i <= totalVisibleChars; i++)
            {
                dialogueTMPText.maxVisibleCharacters = i;

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
            Debug.Log("타이핑이 스킵되었습니다!");
            dialogueTMPText.maxVisibleCharacters = dialogueTMPText.textInfo.characterCount;
        }

        CompleteTyping();
    }


    #region Legacy Text
    private string currentCleanText = "";
    public async UniTask TypeSentence(string rawSentence)
    {
        if (rawSentence == null) return;
        if (!(rawSentence.Length > 0)) return;

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

        currentCleanText = cleanSentence;

        dialogueText.text = "";
        int totalChars = cleanSentence.Length;

        try
        {
            for (int i = 0; i <= totalChars; i++)
            {
                dialogueText.text = cleanSentence.Substring(0, i);

                if (delayDict.ContainsKey(i))
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(delayDict[i]), cancellationToken: token);
                }

                if (i < totalChars)
                {
                    await UniTask.Delay(System.TimeSpan.FromSeconds(defaultTypingDelay), cancellationToken: token);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log("타이핑이 스킵되었습니다!");
            dialogueText.text = cleanSentence;    
        }

        CompleteTyping();
    }

    #endregion
}
