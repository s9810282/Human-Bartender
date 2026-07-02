using TMPro;
using UnityEngine;

/// <summary>히스토리의 선택지 기록 항목 UI. 선택한 항목은 강조, 나머지는 취소선으로 표시한다.</summary>
public class HistoryChoiceItem : MonoBehaviour
{
    [SerializeField] TMP_Text headerText;       // "선택지"
    [SerializeField] RectTransform itemRoot;    // VerticalLayoutGroup
    [SerializeField] TMP_Text itemPrefab;       // 한 줄짜리 TMP
    [SerializeField] Color chosenColor = new Color(0.78f, 0.85f, 0.94f);
    [SerializeField] Color dimColor = new Color(0.16f, 0.23f, 0.31f);

    public void Bind(HistoryRecord r)
    {
        if (headerText) headerText.text = "선택지";

        for (int i = itemRoot.childCount - 1; i >= 0; i--)
            Destroy(itemRoot.GetChild(i).gameObject);

        for (int i = 0; i < r.choices.Length; i++)
        {
            bool chosen = i == r.chosen;
            var line = Instantiate(itemPrefab, itemRoot);
            line.text = r.choices[i];
            line.color = chosen ? chosenColor : dimColor;
            line.fontStyle = chosen ? FontStyles.Bold : FontStyles.Strikethrough; // TMP 취소선
        }
    }
}
