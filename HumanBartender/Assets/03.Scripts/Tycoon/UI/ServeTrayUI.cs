using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 완성한 잔이 놓이는 화면 하단 가운데 트레이.
///
/// 제조가 끝나면 잔 하나가 여기에 생기고, 손님 앞 코스터 자리로 끌어다 놓으면 그 손님에게 나간다.
///
/// 여기에 잔이 남아 있는 동안에는 새 잔을 만들 수 없다. 만들어만 두고 아무에게도 내지 않은 잔이
/// 쌓이면 어느 잔이 누구 것인지가 흐려지고, 제한시간이 있는 서빙이 미리 만들어 두는 것으로 풀린다.
/// 그래도 자리는 한 칸이 아니라 줄로 둔다 — 잔이 늘 하나뿐인 것은 지금의 규칙이지 트레이의 생김새가 아니다.
///
/// 잔 그림은 아직 없어서 칵테일 색으로 칠한 임시 도형으로 그린다. 목록 아이콘을 색 사각형으로
/// 두고 있는 CraftMenuPanel과 같은 방식이라, 실제 잔 스프라이트가 나오면 여기만 갈아 끼우면 된다.
/// </summary>
public class ServeTrayUI : MonoBehaviour
{
    [Header("Scene")]
    [Tooltip("제조 완료를 알려주는 곳. 여기서 잔 하나가 만들어졌다는 소식을 받는다.")]
    [SerializeField] CraftFlowController craftFlow;

    [Tooltip("트레이를 올려놓을 Screen Space Overlay 캔버스. 코스터 트레이가 쓰는 것을 같이 써도 된다.")]
    [SerializeField] Canvas rootCanvas;

    [Header("Data")]
    [SerializeField] NewCocktailDataSO cocktailData;

    [Header("Font")]
    [SerializeField] TMP_FontAsset font; // 한글 미지원 기본 폰트 대신 NeoDunggeunmo SDF를 써야 한다.

    [Header("Layout")]
    [SerializeField] Vector2 itemSize = new(84f, 104f);
    [SerializeField] float spacing = 12f;

    [Tooltip("화면 아래쪽 끝에서 트레이까지의 여백.")]
    [SerializeField] float bottomMargin = 16f;

    [Header("Style")]
    [SerializeField] Color slotColor = new(1f, 1f, 1f, 0.06f);
    [SerializeField] Color glassColor = new(1f, 1f, 1f, 0.55f);

    RectTransform trayRoot;
    readonly List<DrinkDragItem> items = new();

    /// <summary>지금 트레이에 놓여 있는 잔의 수.</summary>
    public int Count => items.Count;

    void Awake()
    {
        if (rootCanvas == null)
        {
            Debug.LogError("[ServeTray] rootCanvas가 비어 있어 트레이를 만들 수 없습니다.");
            return;
        }

        BuildTrayRoot();
    }

    void OnEnable()
    {
        if (craftFlow == null)
        {
            Debug.LogError("[ServeTray] craftFlow가 비어 있어 완성한 잔을 받을 수 없습니다.");
            return;
        }

        craftFlow.CraftCompleted += OnCraftCompleted;
        craftFlow.AddCraftBlocker(DescribeCraftBlock);
    }

    void OnDisable()
    {
        if (craftFlow == null) return;

        craftFlow.CraftCompleted -= OnCraftCompleted;

        // 트레이가 없으면 막을 사람도 없다. 걸어 둔 채로 꺼지면 제조가 영영 막힌다.
        craftFlow.RemoveCraftBlocker(DescribeCraftBlock);
    }

    /// <summary>
    /// 아직 내지 않은 잔이 남아 있으면 새 제조를 막는 이유를 댄다. 막을 것이 없으면 null이다.
    /// </summary>
    string DescribeCraftBlock()
    {
        if (items.Count == 0) return null;

        return $"아직 내지 않은 잔({items[0].Drink.DisplayName})이 트레이에 있어 새로 만들 수 없습니다.";
    }

    /// <summary>잔들이 가로로 늘어설 자리를 화면 하단 가운데에 만든다. 잔 수에 따라 폭은 스스로 늘어난다.</summary>
    void BuildTrayRoot()
    {
        var go = new GameObject("Serve Tray",
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        go.transform.SetParent(rootCanvas.transform, false);

        trayRoot = (RectTransform)go.transform;
        trayRoot.anchorMin = new Vector2(0.5f, 0f);
        trayRoot.anchorMax = new Vector2(0.5f, 0f);
        trayRoot.pivot = new Vector2(0.5f, 0f);
        trayRoot.anchoredPosition = new Vector2(0f, bottomMargin);

        var layout = go.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        var fitter = go.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>제조가 끝나 기록이 확정되면 그 잔을 트레이에 올린다.</summary>
    void OnCraftCompleted(CraftSession session, CraftJudgement judgement)
    {
        if (trayRoot == null) return;

        if (!cocktailData.TryGet(session.SelectedCocktailId, out NewCocktailData cocktail))
        {
            Debug.LogError($"[ServeTray] {session.SelectedCocktailId} 칵테일을 데이터에서 찾지 못해 잔을 만들지 못했습니다.");
            return;
        }

        AddDrink(new CraftedDrink(session, judgement, cocktail));
    }

    /// <summary>완성한 잔 하나를 트레이 맨 뒤에 올린다.</summary>
    public void AddDrink(CraftedDrink drink)
    {
        DrinkDragItem item = CreateItem(drink);
        item.Served += OnItemServed;
        items.Add(item);

        craftFlow?.RefreshCraftAvailability();

        Debug.Log($"[ServeTray] {drink.DisplayName} 완성 — 트레이에 올렸습니다. (총 {items.Count}잔)");
    }

    /// <summary>손님에게 나간 잔을 목록에서 지운다. 오브젝트를 치우는 것은 아이콘 쪽이 한다.</summary>
    void OnItemServed(DrinkDragItem item)
    {
        item.Served -= OnItemServed;
        items.Remove(item);

        craftFlow?.RefreshCraftAvailability();
    }

    // ── 임시 잔 아이콘 ──────────────────────────────────────────────────

    /// <summary>
    /// 잔 아이콘 하나를 만든다. 칸 위에 칵테일 색을 칠한 잔 몸통과 다리·받침, 그 아래 이름이 놓인다.
    /// 그라데이션 칵테일(color2)은 몸통 아래쪽 절반을 두 번째 색으로 칠해 두 층으로 보이게 한다.
    /// </summary>
    DrinkDragItem CreateItem(CraftedDrink drink)
    {
        var go = new GameObject($"Drink {drink.CocktailId}",
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement), typeof(DrinkDragItem));
        go.transform.SetParent(trayRoot, false);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = itemSize;

        go.GetComponent<Image>().color = slotColor;

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = itemSize.x;
        layout.preferredHeight = itemSize.y;

        RectTransform body = CreateRect(rect, "Body",
            new Vector2(itemSize.x * 0.52f, itemSize.y * 0.44f),
            new Vector2(0f, itemSize.y * 0.19f), drink.Color);

        if (drink.Color2.HasValue)
            CreateBottomHalf(body, drink.Color2.Value);

        CreateRect(rect, "Stem", new Vector2(6f, 14f), new Vector2(0f, -itemSize.y * 0.11f), glassColor);
        CreateRect(rect, "Base", new Vector2(itemSize.x * 0.31f, 4f), new Vector2(0f, -itemSize.y * 0.19f), glassColor);

        CreateName(rect, drink.DisplayName);

        var item = go.GetComponent<DrinkDragItem>();
        item.Initialize(drink, rootCanvas);
        return item;
    }

    /// <summary>부모 중앙을 기준으로 size 크기의 단색 사각형을 만든다.</summary>
    static RectTransform CreateRect(RectTransform parent, string name, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false; // 잡는 것은 칸 전체다. 안쪽 도형이 따로 입력을 먹을 이유가 없다.

        return rect;
    }

    /// <summary>몸통 아래쪽 절반을 두 번째 색으로 덮는다.</summary>
    static void CreateBottomHalf(RectTransform body, Color color)
    {
        var go = new GameObject("Body2", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(body, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    void CreateName(RectTransform parent, string displayName)
    {
        var go = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(4f, 6f);
        rect.offsetMax = new Vector2(-4f, 6f + itemSize.y * 0.21f);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = displayName;
        text.fontSize = 12f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        if (font != null) text.font = font;
    }
}
