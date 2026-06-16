using TMPro;
using UnityEngine;

// 씬/손님이 바뀌는 지점의 구분선 라벨.
public class HistorySeparatorItem : MonoBehaviour
{
    [SerializeField] TMP_Text label;

    public void Bind(string group)
    {
        if (label) label.text = group;
    }
}
