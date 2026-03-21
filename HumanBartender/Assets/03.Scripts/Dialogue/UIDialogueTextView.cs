using NUnit.Framework.Constraints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class TypingData
{
    public string str;
    public Action callBackEvent;

    public TypingData()
    {
    }

    public TypingData(string str, Action callBackEvent)
    {
        this.str = str;
        this.callBackEvent = callBackEvent;
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

    private WaitForSeconds defaultTypingDelay = new WaitForSeconds(0.05f);
    private Coroutine typingCoroutine;
    private TypingData curTypingData;

    void Start()
    {
        curTypingData = new TypingData();
    }



    public void StartType(TypingData data)
    {
        if (data == null)
        {
            Logger.LogWarning("Typing Data is Null");
            return;
        }

        dialogueText.fontStyle = FontStyle.Normal;
        dialogueTMPText.fontStyle = FontStyles.Normal;

        curTypingData = data;
        typingCoroutine = StartCoroutine(TypeSentence(curTypingData.str));
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
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        dialogueTMPText.maxVisibleCharacters = dialogueTMPText.textInfo.characterCount;
        dialogueText.text = currentCleanText;
        CompleteTyping();
    }
    public void CompleteTyping()
    {
        curTypingData.callBackEvent.Invoke();

        curTypingData = null;
        typingCoroutine = null;
    }

    private IEnumerator TypeSentenceTMP(string rawSentence)
    {
        if (!(rawSentence.Length > 0)) yield break;

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

        for (int i = 0; i <= totalVisibleChars; i++)
        {
            dialogueTMPText.maxVisibleCharacters = i;

            if (delayDict.ContainsKey(i))
            {
                yield return new WaitForSeconds(delayDict[i]);
            }

            if (i < totalVisibleChars)
            {
                yield return defaultTypingDelay; // 캐싱된 WaitForSeconds 사용
            }
        }

        CompleteTyping();
    }


    #region Legacy Text
    private string currentCleanText = "";
    private IEnumerator TypeSentence(string rawSentence)
    {
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

        for (int i = 0; i <= totalChars; i++)
        {
            dialogueText.text = cleanSentence.Substring(0, i);

            if (delayDict.ContainsKey(i))
            {
                yield return new WaitForSeconds(delayDict[i]);
            }

            if (i < totalChars)
            {
                yield return defaultTypingDelay;
            }
        }

        CompleteTyping();
    }
    #endregion

    
}
