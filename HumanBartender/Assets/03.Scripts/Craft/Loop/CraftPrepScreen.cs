using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 제조 준비 화면(칵테일 제조 준비 시스템 §3).
///
/// 잔 → 도구 → 재료 순서로 선반을 보여 주고, 고른 것을 하단 트레이에 쌓는다. 어느 단계에서든
/// 앞뒤로 옮겨 다닐 수 있다 — 순서는 안내일 뿐 강제가 아니다.
///
/// 무엇을 고를 수 있는지는 CraftShelf가, 골라도 되는지·다 골랐는지는 CraftPreparation이 정한다.
/// 이 클래스는 그 둘을 화면으로 옮기고 클릭을 돌려보내기만 한다 — 판정을 여기서 다시 하면
/// 화면과 기록이 서로 다른 말을 하기 시작한다.
///
/// 칸은 아직 색 사각형이다. 선반 아트가 나오면 CreateCell만 갈아 끼우면 된다.
/// </summary>
public class CraftPrepScreen : MonoBehaviour
{
    /// <summary>화면이 지금 보여 주는 선반.</summary>
    public enum EStage
    {
        Glass,
        Tool,
        Ingredient,
    }

    [Header("Data")]
    [SerializeField] NewShelfItemDataSO shelfData;
    [SerializeField] NewCocktailDataSO cocktailData;

    [Header("Scene")]
    [Tooltip("제조 흐름. 연결하면 칵테일을 고를 때 이 화면이 저절로 열린다. 단독 테스트 씬에서는 비워 둔다.")]
    [SerializeField] CraftFlowController craftFlow;

    [Header("Layout")]
    [SerializeField] RectTransform stageTabsContent;
    [SerializeField] RectTransform shelfTabsContent;
    [SerializeField] RectTransform shelfContent;
    [SerializeField] RectTransform trayContent;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI noticeText;
    [SerializeField] Button nextButton;
    [SerializeField] Button recipeNoteButton;

    [Header("Font")]
    [SerializeField] TMP_FontAsset font;

    [Header("Style")]
    [SerializeField] Vector2 cellSize = new(104f, 116f);
    [SerializeField] Color cellColor = new(1f, 1f, 1f, 0.06f);
    [SerializeField] Color cellSelectedColor = new(0.83f, 0.55f, 0.18f, 0.28f);
    [SerializeField] Color guideColor = new(0.36f, 0.76f, 0.72f, 0.30f);
    [SerializeField] Color tabIdleColor = new(1f, 1f, 1f, 0.08f);
    [SerializeField] Color tabActiveColor = new(0.83f, 0.55f, 0.18f, 0.35f);

    [Header("Test")]
    [Tooltip("단독 테스트 씬용. 켜 두면 시작할 때 testCocktailId로 준비 화면을 연다.")]
    [SerializeField] bool openOnStartForTest;
    [SerializeField] string testCocktailId = "gin_fizz";
    [SerializeField] int testDay = 1;

    CraftPreparation preparation;
    EStage stage = EStage.Glass;
    ENewShelfGroup shelfGroup = ENewShelfGroup.Liquor;

    /// <summary>다시 만들 때 지워야 하는 칸들. 매번 자식을 통째로 훑지 않으려고 들고 있는다.</summary>
    readonly List<GameObject> spawned = new();

    /// <summary>지금 열려 있는 준비. 열려 있지 않으면 null이다.</summary>
    public CraftPreparation Preparation => preparation;

    void Awake()
    {
        if (nextButton != null) nextButton.onClick.AddListener(OnNext);
        if (recipeNoteButton != null) recipeNoteButton.onClick.AddListener(OnRecipeNoteClosed);

        BuildStageTabs();
        BuildShelfTabs();
    }

    void OnEnable()
    {
        if (craftFlow == null) return;

        craftFlow.CraftBegan += OnCraftBegan;
        craftFlow.PreparationReady += OnPreparationReady;
    }

    void OnDisable()
    {
        if (craftFlow == null) return;

        craftFlow.CraftBegan -= OnCraftBegan;
        craftFlow.PreparationReady -= OnPreparationReady;
    }

    void Start()
    {
        if (!openOnStartForTest) return;

        OpenAfterDataLoadAsync().Forget();
    }

    /// <summary>
    /// json 로딩이 끝나기를 기다렸다가 연다. 기다리지 않으면 선반이 빈 채로 뜨는데, 예외도 나지 않아
    /// 데이터가 없는 것인지 로딩이 덜 된 것인지 구분할 수 없다.
    /// </summary>
    async UniTaskVoid OpenAfterDataLoadAsync()
    {
        await NewDataLoadManager.WaitUntilLoadedAsync();

        if (this == null) return;

        OpenForTest();
    }

    // ── 열고 닫기 ───────────────────────────────────────────────────────

    /// <summary>준비를 화면에 건다. 이전에 걸려 있던 것은 놓는다.</summary>
    public void Open(CraftPreparation prep)
    {
        Close();

        preparation = prep;
        if (preparation == null) return;

        preparation.Changed += Refresh;

        stage = EStage.Glass;
        shelfGroup = ENewShelfGroup.Liquor;

        gameObject.SetActive(true);
        Refresh();
    }

    /// <summary>화면을 비운다. 신호를 놓지 않으면 다음 제조에서 두 번 갱신된다.</summary>
    public void Close()
    {
        if (preparation != null) preparation.Changed -= Refresh;

        preparation = null;
        ClearSpawned();
    }

    void OnDestroy()
    {
        Close();
    }

    void OnCraftBegan(CraftSession session)
    {
        Open(craftFlow.Preparation);
    }

    /// <summary>
    /// 단독 테스트 씬에서 쓰는 진입점. 제조 흐름 없이 준비만 열어 본다.
    /// 실제 게임에서는 CraftFlowController가 만든 준비를 Open으로 받는다.
    /// </summary>
    public void OpenForTest()
    {
        if (cocktailData == null || !cocktailData.TryGet(testCocktailId, out NewCocktailData cocktail))
        {
            Debug.LogError($"[CraftPrep] 테스트용 칵테일 '{testCocktailId}'을 찾지 못했습니다. " +
                           "cocktails.json이 로드된 뒤인지 확인하세요.");
            return;
        }

        GameStateManager.Instance.CurrentDay = testDay;

        Open(new CraftPreparation(cocktail, new ActualCraft()));
    }

    // ── 갱신 ────────────────────────────────────────────────────────────

    void Refresh()
    {
        if (preparation == null) return;

        RefreshTitle();
        RefreshTabs();
        RefreshShelf();
        RefreshTray();
        RefreshFooter();
    }

    void RefreshTitle()
    {
        if (titleText == null) return;

        string name = preparation.Note.Name.Ko;
        titleText.text = string.IsNullOrEmpty(name) ? preparation.Note.CocktailId : name;
    }

    void RefreshFooter()
    {
        if (nextButton != null) nextButton.interactable = preparation.CanProceed;

        if (noticeText == null) return;

        // 정답 구성을 다 갖춘 순간 한 번만 알린다. 넘어가는 것은 플레이어가 정한다(§3.8.1).
        if (preparation.ConsumeReadyAnnouncement())
            noticeText.text = "모든 재료를 선택하셨습니다. 다음 단계로 진행하십시오.";
        else if (!preparation.CanProceed)
            noticeText.text = "잔과 재료를 최소한 하나씩 골라야 다음으로 넘어갈 수 있습니다.";
    }

    // ── 선반 ────────────────────────────────────────────────────────────

    List<NewShelfItemData> CollectShelfItems()
    {
        int day = GameStateManager.Instance.CurrentDay;

        return stage switch
        {
            EStage.Glass => CraftShelf.GetGlasses(shelfData, day),
            EStage.Tool => CraftShelf.GetTools(shelfData, day),
            _ => CraftShelf.GetIngredients(shelfData, shelfGroup, day),
        };
    }

    void RefreshShelf()
    {
        ClearSpawned();

        if (shelfContent == null) return;

        foreach (var item in CollectShelfItems())
            spawned.Add(CreateCell(item));
    }

    /// <summary>
    /// 선반 칸 하나. 색은 재료의 액체 색을 쓰고, 색이 없는 잔·도구는 회색으로 둔다.
    ///
    /// 고른 칸과 가이드 칸을 서로 다른 색으로 칠한다. 가이드는 노트를 한 번 열어 본 뒤에만 켜지고,
    /// 켜져 있어도 다른 것을 고르는 것을 막지 않는다(§3.7.8).
    /// </summary>
    GameObject CreateCell(NewShelfItemData item)
    {
        var go = new GameObject($"Cell {item.Id}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(shelfContent, false);

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = cellSize.x;
        layout.preferredHeight = cellSize.y;

        bool selected = preparation.IsSelected(item.Id);
        bool guided = preparation.IsGuideTarget(item.Id);

        go.GetComponent<Image>().color = selected ? cellSelectedColor : guided ? guideColor : cellColor;

        CreateSwatch(go.transform, item);
        CreateLabel(go.transform, ResolveName(item), 12f, new Vector2(4f, 6f), new Vector2(-4f, 30f));

        string id = item.Id;
        go.GetComponent<Button>().onClick.AddListener(() => OnCellClicked(id));

        return go;
    }

    /// <summary>칸 위쪽의 색 조각. 아트가 나오기 전까지 무엇인지 구분하는 유일한 단서다.</summary>
    void CreateSwatch(Transform parent, NewShelfItemData item)
    {
        var go = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(cellSize.x * 0.5f, cellSize.y * 0.44f);
        rect.anchoredPosition = new Vector2(0f, -12f);

        var image = go.GetComponent<Image>();
        image.color = item.TryGetLiquidColor(out Color32 color) ? color : new Color(1f, 1f, 1f, 0.35f);
        image.raycastTarget = false;
    }

    void OnCellClicked(string id)
    {
        switch (stage)
        {
            case EStage.Glass: preparation.ToggleGlass(id); break;
            case EStage.Tool: preparation.ToggleTool(id); break;
            default: preparation.ToggleIngredient(id); break;
        }
    }

    // ── 하단 트레이 ─────────────────────────────────────────────────────

    /// <summary>
    /// 지금까지 고른 것. 잔·도구·재료를 한 줄에 늘어놓고, 재료는 고른 순서를 그대로 지킨다.
    /// 여기서 누르면 선택이 풀린다(§3.4.2).
    /// </summary>
    void RefreshTray()
    {
        if (trayContent == null) return;

        for (int i = trayContent.childCount - 1; i >= 0; i--)
            Destroy(trayContent.GetChild(i).gameObject);

        if (preparation.GlassId != null) CreateTrayChip(preparation.GlassId, EStage.Glass);
        if (preparation.ToolId != null) CreateTrayChip(preparation.ToolId, EStage.Tool);

        foreach (string ingredientId in preparation.IngredientIds)
            CreateTrayChip(ingredientId, EStage.Ingredient);
    }

    void CreateTrayChip(string id, EStage from)
    {
        var go = new GameObject($"Chip {id}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(trayContent, false);

        go.GetComponent<Image>().color = cellSelectedColor;

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = 96f;
        layout.preferredHeight = 28f;

        string label = shelfData != null && shelfData.TryGet(id, out var item) ? ResolveName(item) : id;
        CreateLabel(go.transform, label, 11.5f, new Vector2(6f, 2f), new Vector2(-6f, -2f));

        go.GetComponent<Button>().onClick.AddListener(() =>
        {
            switch (from)
            {
                case EStage.Glass: preparation.ToggleGlass(id); break;
                case EStage.Tool: preparation.ToggleTool(id); break;
                default: preparation.ToggleIngredient(id); break;
            }
        });
    }

    // ── 탭 ──────────────────────────────────────────────────────────────

    readonly Dictionary<EStage, Image> stageTabImages = new();
    readonly Dictionary<ENewShelfGroup, Image> shelfTabImages = new();

    void BuildStageTabs()
    {
        if (stageTabsContent == null) return;

        CreateTab(stageTabsContent, "잔", () => SetStage(EStage.Glass), img => stageTabImages[EStage.Glass] = img);
        CreateTab(stageTabsContent, "도구", () => SetStage(EStage.Tool), img => stageTabImages[EStage.Tool] = img);
        CreateTab(stageTabsContent, "재료", () => SetStage(EStage.Ingredient), img => stageTabImages[EStage.Ingredient] = img);
    }

    void BuildShelfTabs()
    {
        if (shelfTabsContent == null) return;

        CreateTab(shelfTabsContent, "술 선반", () => SetShelfGroup(ENewShelfGroup.Liquor),
                  img => shelfTabImages[ENewShelfGroup.Liquor] = img);
        CreateTab(shelfTabsContent, "냉장고", () => SetShelfGroup(ENewShelfGroup.Fridge),
                  img => shelfTabImages[ENewShelfGroup.Fridge] = img);
    }

    void CreateTab(RectTransform parent, string label, UnityEngine.Events.UnityAction onClick,
                   System.Action<Image> keep)
    {
        var go = new GameObject($"Tab {label}",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        var layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = 92f;
        layout.preferredHeight = 32f;

        var image = go.GetComponent<Image>();
        image.color = tabIdleColor;
        keep(image);

        CreateLabel(go.transform, label, 13f, new Vector2(4f, 2f), new Vector2(-4f, -2f));
        go.GetComponent<Button>().onClick.AddListener(onClick);
    }

    void SetStage(EStage next)
    {
        stage = next;
        Refresh();
    }

    void SetShelfGroup(ENewShelfGroup group)
    {
        shelfGroup = group;
        stage = EStage.Ingredient;
        Refresh();
    }

    void RefreshTabs()
    {
        foreach (var pair in stageTabImages)
            pair.Value.color = pair.Key == stage ? tabActiveColor : tabIdleColor;

        foreach (var pair in shelfTabImages)
            pair.Value.color = pair.Key == shelfGroup && stage == EStage.Ingredient ? tabActiveColor : tabIdleColor;

        // 선반 전환 탭은 재료 단계에서만 뜻이 있다.
        if (shelfTabsContent != null)
            shelfTabsContent.gameObject.SetActive(stage == EStage.Ingredient);
    }

    // ── 그 밖 ───────────────────────────────────────────────────────────

    void OnNext()
    {
        if (preparation == null || !preparation.CanProceed) return;

        if (craftFlow != null) craftFlow.StartGimmicks();
        else Debug.Log($"[CraftPrep] 다음 단계로: 잔 {preparation.GlassId} / 도구 {preparation.ToolId ?? "없음"} / " +
                       $"재료 {string.Join(", ", preparation.IngredientIds)}");
    }

    /// <summary>레시피 노트를 닫았을 때. 이때부터 정답 칸에 가이드가 켜진다(§3.7.5).</summary>
    void OnRecipeNoteClosed()
    {
        if (preparation == null) return;

        preparation.MarkRecipeNoteRead();
        Refresh();
    }

    void OnPreparationReady(CraftPreparation prep)
    {
        Refresh();
    }

    void ClearSpawned()
    {
        foreach (var go in spawned)
        {
            if (go != null) Destroy(go);
        }

        spawned.Clear();
    }

    /// <summary>화면에 적을 이름. 한국어 이름이 없으면 id를 그대로 보여 준다 — 빈 칸보다 낫다.</summary>
    static string ResolveName(NewShelfItemData item)
    {
        return string.IsNullOrEmpty(item.Name.Ko) ? item.Id : item.Name.Ko;
    }

    void CreateLabel(Transform parent, string text, float size, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.raycastTarget = false;

        if (font != null) label.font = font;
    }
}
