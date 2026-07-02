using TMPro;
using UnityEngine;

/// <summary>히스토리에서 씬·손님이 바뀌는 지점을 표시하는 구분선 라벨 항목 UI.</summary>
public class HistorySeparatorItem : MonoBehaviour
{
    [SerializeField] TMP_Text label;

    public void Bind(string group)
    {
        if (label) label.text = group;
    }
}
