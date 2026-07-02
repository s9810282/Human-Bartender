using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>히스토리의 대화·시스템 한 줄 항목 UI. 화자 이름에 캐릭터 NameColor를 그대로 적용한다.</summary>
public class HistoryEntryItem : MonoBehaviour
{
    [SerializeField] TMP_Text speakerText;
    [SerializeField] TMP_Text bodyText;
    [SerializeField] Image leftBar;   // 선택: 좌측 색 바
    [SerializeField] Color systemColor = new Color(0.376f, 0.812f, 0.627f); // #60cfa0

    public void Bind(HistoryRecord r)
    {
        bool isSystem = r.kind == HistoryKind.System;
        Color c = isSystem ? systemColor : r.nameColor;
        string name = isSystem
            ? (string.IsNullOrEmpty(r.speakerName) ? "SYSTEM" : r.speakerName)
            : (string.IsNullOrEmpty(r.speakerName) ? r.speakerId : r.speakerName);

        if (speakerText) { speakerText.text = name; speakerText.color = c; }
        if (bodyText) bodyText.text = r.text;
        if (leftBar) leftBar.color = c;
    }
}
