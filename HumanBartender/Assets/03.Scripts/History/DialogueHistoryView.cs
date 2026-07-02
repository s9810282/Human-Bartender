using UnityEngine;
using UnityEngine.UI;
using VContainer;

/// <summary>
/// 히스토리 오버레이 패널. DialogueHistory의 기록을 ScrollRect 안에 프리팹으로 나열한다.
/// HUD의 '대화 기록' 버튼 onClick에 Open()을 연결해 사용한다.
/// </summary>
public class DialogueHistoryView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] GameObject panel;            // 풀스크린 오버레이 루트 (Raycast 막는 Image 포함)
    [SerializeField] ScrollRect scrollRect;
    [SerializeField] RectTransform content;       // VerticalLayoutGroup + ContentSizeFitter
    [SerializeField] GameObject emptyLabel;
    [SerializeField] Button closeButton;

    [Header("Prefabs")]
    [SerializeField] HistoryEntryItem entryPrefab;       // 대화/시스템
    [SerializeField] HistoryChoiceItem choicePrefab;     // 선택지
    [SerializeField] HistorySeparatorItem separatorPrefab; // 그룹 구분선

    DialogueHistory _history;

    public bool IsOpen => panel != null && panel.activeSelf;

    // VContainer 주입 (권장)
    [Inject]
    public void Construct(DialogueHistory history) => _history = history;

    // DI 안 쓸 경우 매니저에서 직접 주입
    public void Init(DialogueHistory history) => _history = history;




    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (panel) panel.SetActive(false);
    }

    public void Open()
    {
        if (_history == null) { Debug.LogWarning("[History] DialogueHistory 미주입"); return; }
        Rebuild();
        panel.SetActive(true);
        ScrollToBottom();
    }

    public void Close() { if (panel) panel.SetActive(false); }
    public void Toggle() { if (IsOpen) Close(); else Open(); }

    void Rebuild()
    {
        // content 비우기
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        if (emptyLabel) emptyLabel.SetActive(_history.Count == 0);

        string lastGroup = null;
        foreach (var r in _history.Records)
        {
            if (!string.IsNullOrEmpty(r.group) && r.group != lastGroup)  // 그룹 바뀔 때 구분선
            {
                lastGroup = r.group;
                Instantiate(separatorPrefab, content).Bind(r.group);
            }

            if (r.kind == HistoryKind.Choice)
                Instantiate(choicePrefab, content).Bind(r);
            else
                Instantiate(entryPrefab, content).Bind(r);   // 대화/시스템 공용
        }
    }

    void ScrollToBottom()
    {
        Canvas.ForceUpdateCanvases();
        if (content) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        if (scrollRect) scrollRect.verticalNormalizedPosition = 0f;
    }
}
