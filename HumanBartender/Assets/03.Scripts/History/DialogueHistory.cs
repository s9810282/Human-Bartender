using System.Collections.Generic;
using UnityEngine;

public enum HistoryKind { Dialogue, Choice, System }

/// <summary>히스토리에 저장되는 한 줄의 기록. 대화·선택지·시스템 메시지를 공용으로 표현한다.</summary>
public class HistoryRecord
{
    public HistoryKind kind;
    public string group;            // 기록 시점의 씬/손님 라벨

    // Dialogue / System
    public string speakerId;
    public string speakerName;
    public Color nameColor = Color.white;
    public string text;

    // Choice
    public string[] choices;
    public int chosen = -1;
}

/// <summary>
/// 진행하면서 보여준 대화·선택지를 누적하는 히스토리 저장소.
/// VContainer 싱글톤으로 등록되어 DialogueManager·DialogueHistoryView에 주입된다.
/// </summary>
public class DialogueHistory
{
    readonly List<HistoryRecord> _records = new List<HistoryRecord>();
    string _group = "";

    public IReadOnlyList<HistoryRecord> Records => _records;
    public int Count => _records.Count;

    // LoadScene 시 호출 → 이후 기록들이 이 그룹으로 묶임
    public void SetGroup(string label) => _group = label ?? "";

    // 일반 대화 한 줄 (이름/색은 SceneDirector가 resolve해서 넘겨줌)
    public void AddDialogue(DialogueData d, string speakerName, Color nameColor)
    {
        _records.Add(new HistoryRecord
        {
            kind = HistoryKind.Dialogue,
            group = _group,
            speakerId = d.Speaker,
            speakerName = string.IsNullOrEmpty(speakerName) ? d.Speaker : speakerName,
            nameColor = nameColor,
            text = d.Text,
        });
    }

    // 시스템/내레이션 줄을 로그에 남기고 싶을 때 직접 호출 (자동 기록 아님)
    public void AddSystem(string text, string label = "SYSTEM")
    {
        _records.Add(new HistoryRecord
        {
            kind = HistoryKind.System,
            group = _group,
            speakerName = label,
            text = text,
        });
    }

    // 선택지 확정 (모든 보기 + 고른 것)
    public void AddChoice(ChoiceData[] choices, ChoiceData chosen)
    {
        int count = choices?.Length ?? 0;
        var texts = new string[count];
        int chosenIdx = -1;

        for (int i = 0; i < count; i++)
        {
            texts[i] = choices[i].Text;   // ★ ChoiceData의 '표시 텍스트' 필드명에 맞게 수정
            if (choices[i].Next == chosen.Next) chosenIdx = i;  // Next로 고른 항목 식별
        }

        _records.Add(new HistoryRecord
        {
            kind = HistoryKind.Choice,
            group = _group,
            choices = texts,
            chosen = chosenIdx,
        });
    }

    public void Clear() => _records.Clear();
}
