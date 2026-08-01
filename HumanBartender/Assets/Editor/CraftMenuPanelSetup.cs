// Editor/CraftMenuPanelSetup.cs
// 메뉴: Tools > Tycoon > Setup Craft Menu Panel
// 선행 조건: Tools > Tycoon > Setup Left Slide Panel 을 먼저 실행해 'Craft Panel'이 씬에 있어야 한다.

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 'Craft Panel'(LeftSlidePanelSetup이 만든 좌측 슬라이드 패널) 안에 "제조하기" 버튼이 있는 메뉴 뷰와,
/// 누르면 나타나는 칵테일 선택 목록 뷰를 만들어 CraftMenuPanel 컴포넌트로 엮는다. 현재 열려있는 씬을
/// 직접 수정하므로, 결과를 확인한 뒤 Ctrl+S로 저장해야 한다. 레이아웃/색상은 placeholder다.
/// </summary>
public static class CraftMenuPanelSetup
{
    const string CraftPanelName = "Craft Panel";
    const string MenuViewName = "Menu View";
    const string CocktailListViewName = "Cocktail List View";
    const string DetailViewName = "Detail View";
    const string CocktailDataAssetPath = "Assets/03.Scripts/DataNew/DataNewSO/NewCocktailDataSO.asset";
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";
    const float HeaderHeight = 48f;

    [MenuItem("Tools/Tycoon/Setup Craft Menu Panel")]
    public static void Run()
    {
        var craftPanelGO = GameObject.Find(CraftPanelName);
        if (craftPanelGO == null)
        {
            Debug.LogError($"[CraftMenuPanelSetup] '{CraftPanelName}'을 찾지 못했습니다. " +
                            "Tools > Tycoon > Setup Left Slide Panel을 먼저 실행하세요.");
            return;
        }

        if (craftPanelGO.transform.Find(DetailViewName) != null)
        {
            Debug.LogWarning($"[CraftMenuPanelSetup] '{CraftPanelName}'에 이미 '{DetailViewName}'가 있습니다. 중복 생성을 막기 위해 건너뜁니다.");
            return;
        }

        var cocktailData = AssetDatabase.LoadAssetAtPath<NewCocktailDataSO>(CocktailDataAssetPath);
        if (cocktailData == null)
        {
            Debug.LogError($"[CraftMenuPanelSetup] {CocktailDataAssetPath}에서 NewCocktailDataSO를 찾지 못했습니다.");
            return;
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
            Debug.LogWarning($"[CraftMenuPanelSetup] {FontAssetPath}에서 폰트를 찾지 못했습니다. 기본 폰트로 생성합니다(한글 미지원 가능).");

        var craftPanelRect = (RectTransform)craftPanelGO.transform;

        // 이전에 이미 Setup Craft Menu Panel을 실행해 Menu View/Cocktail List View + CraftMenuPanel이 있는 상태라면
        // Detail View만 추가로 만들어 기존 컴포넌트에 연결한다. 처음 실행이면 전부 새로 만든다.
        var panel = craftPanelGO.GetComponent<CraftMenuPanel>();
        bool isFirstSetup = panel == null;

        if (isFirstSetup)
        {
            GameObject menuView = CreateMenuView(craftPanelRect, font, out Button craftButton);
            GameObject listView = CreateCocktailListView(craftPanelRect, font, out Button backButton, out RectTransform listContent);
            listView.SetActive(false);

            panel = craftPanelGO.AddComponent<CraftMenuPanel>();
            SetSerializedField(panel, "menuView", menuView);
            SetSerializedField(panel, "cocktailListView", listView);
            SetSerializedField(panel, "craftButton", craftButton);
            SetSerializedField(panel, "backButton", backButton);
            SetSerializedField(panel, "listContent", listContent);
            SetSerializedField(panel, "cocktailData", cocktailData);
            SetSerializedField(panel, "font", font);
        }

        GameObject detailView = CreateDetailView(craftPanelRect, font, out Button detailBackButton, out TextMeshProUGUI detailTitleText,
            out Image detailIconImage, out TextMeshProUGUI detailDescriptionText, out RectTransform detailTagsContent, out Button startCraftButton);
        detailView.SetActive(false);

        SetSerializedField(panel, "detailView", detailView);
        SetSerializedField(panel, "detailBackButton", detailBackButton);
        SetSerializedField(panel, "detailTitleText", detailTitleText);
        SetSerializedField(panel, "detailIconImage", detailIconImage);
        SetSerializedField(panel, "detailDescriptionText", detailDescriptionText);
        SetSerializedField(panel, "detailTagsContent", detailTagsContent);
        SetSerializedField(panel, "startCraftButton", startCraftButton);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[CraftMenuPanelSetup] 완료. Ctrl+S로 씬을 저장하세요. 레이아웃/색상은 placeholder이니 조정해주세요.");
    }

    static GameObject CreateMenuView(RectTransform parent, TMP_FontAsset font, out Button craftButton)
    {
        var viewGO = new GameObject(MenuViewName, typeof(RectTransform));
        viewGO.transform.SetParent(parent, false);

        var viewRect = (RectTransform)viewGO.transform;
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        const float buttonWidth = 200f;
        const float buttonHeight = 56f;

        var buttonGO = new GameObject("Craft Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(viewGO.transform, false);

        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 1f);
        buttonRect.anchorMax = new Vector2(0.5f, 1f);
        buttonRect.pivot = new Vector2(0.5f, 1f);
        buttonRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        buttonRect.anchoredPosition = new Vector2(0f, -40f);

        buttonGO.GetComponent<Image>().color = new Color(0.82f, 0.62f, 0.38f, 1f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);
        var labelRect = (RectTransform)labelGO.transform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        var label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = "제조하기";
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (font != null) label.font = font;

        craftButton = buttonGO.GetComponent<Button>();
        return viewGO;
    }

    static GameObject CreateCocktailListView(RectTransform parent, TMP_FontAsset font, out Button backButton, out RectTransform listContent)
    {
        var viewGO = new GameObject(CocktailListViewName, typeof(RectTransform));
        viewGO.transform.SetParent(parent, false);

        var viewRect = (RectTransform)viewGO.transform;
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        CreateHeader(viewGO.transform, "제조할 음료 선택", font, out backButton, out _);

        // ScrollRect (아래 나머지 영역을 채운다)
        var scrollGO = new GameObject("Scroll View", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(viewGO.transform, false);
        var scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = Vector2.zero;
        scrollRect.offsetMax = new Vector2(0f, -HeaderHeight);
        scrollGO.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.001f); // 레이캐스트용 투명 배경

        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewportGO.GetComponent<Image>().color = Color.white;
        viewportGO.GetComponent<Mask>().showMaskGraphic = false;

        var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;
        contentRect.anchoredPosition = Vector2.zero;

        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        listContent = contentRect;
        return viewGO;
    }

    /// <summary>
    /// 목록 뷰/상세 뷰가 공유하는 헤더(뒤로가기 버튼 + 제목)를 만들어 parent 위쪽에 고정한다.
    /// </summary>
    static GameObject CreateHeader(Transform parent, string title, TMP_FontAsset font, out Button backButton, out TextMeshProUGUI titleText)
    {
        var headerGO = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGO.transform.SetParent(parent, false);
        var headerRect = headerGO.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.sizeDelta = new Vector2(0f, HeaderHeight);
        headerRect.anchoredPosition = Vector2.zero;
        headerGO.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.1f, 1f);

        var backGO = new GameObject("Back Button", typeof(RectTransform), typeof(Image), typeof(Button));
        backGO.transform.SetParent(headerGO.transform, false);
        var backRect = backGO.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0f, 0.5f);
        backRect.anchorMax = new Vector2(0f, 0.5f);
        backRect.pivot = new Vector2(0f, 0.5f);
        backRect.sizeDelta = new Vector2(40f, 40f);
        backRect.anchoredPosition = new Vector2(8f, 0f);
        backGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);

        var backLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        backLabelGO.transform.SetParent(backGO.transform, false);
        var backLabelRect = (RectTransform)backLabelGO.transform;
        backLabelRect.anchorMin = Vector2.zero;
        backLabelRect.anchorMax = Vector2.one;
        backLabelRect.sizeDelta = Vector2.zero;
        var backLabel = backLabelGO.GetComponent<TextMeshProUGUI>();
        backLabel.text = "◀"; // ◀
        backLabel.fontSize = 20f;
        backLabel.alignment = TextAlignmentOptions.Center;
        backLabel.color = Color.white;
        if (font != null) backLabel.font = font;

        var titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(headerGO.transform, false);
        var titleRect = (RectTransform)titleGO.transform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(56f, 0f);
        titleRect.offsetMax = new Vector2(-8f, 0f);
        var titleLabel = titleGO.GetComponent<TextMeshProUGUI>();
        titleLabel.text = title;
        titleLabel.fontSize = 18f;
        titleLabel.fontStyle = FontStyles.Bold;
        titleLabel.alignment = TextAlignmentOptions.MidlineLeft;
        titleLabel.color = Color.white;
        if (font != null) titleLabel.font = font;

        backButton = backGO.GetComponent<Button>();
        titleText = titleLabel;
        return headerGO;
    }

    /// <summary>
    /// 목록에서 항목을 고르면 나타나는 상세 뷰(이름/아이콘/설명/태그 + 우측 하단 제조 시작 버튼)를 만든다.
    /// </summary>
    static GameObject CreateDetailView(RectTransform parent, TMP_FontAsset font, out Button backButton, out TextMeshProUGUI titleText,
        out Image iconImage, out TextMeshProUGUI descriptionText, out RectTransform tagsContent, out Button startCraftButton)
    {
        var viewGO = new GameObject(DetailViewName, typeof(RectTransform));
        viewGO.transform.SetParent(parent, false);

        var viewRect = (RectTransform)viewGO.transform;
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        CreateHeader(viewGO.transform, "", font, out backButton, out titleText);

        // 아이콘 (잔 placeholder)
        const float iconSize = 80f;
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(viewGO.transform, false);
        var iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        iconRect.anchoredPosition = new Vector2(0f, -(HeaderHeight + 24f));
        iconImage = iconGO.GetComponent<Image>();
        iconImage.color = Color.white;

        // 설명 라벨 + 본문
        const float descLabelY = HeaderHeight + 24f + iconSize + 20f;
        var descLabelGO = new GameObject("Description Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        descLabelGO.transform.SetParent(viewGO.transform, false);
        var descLabelRect = (RectTransform)descLabelGO.transform;
        descLabelRect.anchorMin = new Vector2(0f, 1f);
        descLabelRect.anchorMax = new Vector2(1f, 1f);
        descLabelRect.pivot = new Vector2(0.5f, 1f);
        descLabelRect.sizeDelta = new Vector2(0f, 20f);
        descLabelRect.anchoredPosition = new Vector2(0f, -descLabelY);
        var descLabel = descLabelGO.GetComponent<TextMeshProUGUI>();
        descLabel.text = "설명";
        descLabel.fontSize = 13f;
        descLabel.color = new Color(1f, 1f, 1f, 0.5f);
        descLabel.margin = new Vector4(24f, 0f, 24f, 0f);
        if (font != null) descLabel.font = font;

        const float descTextY = HeaderHeight + 24f + iconSize + 44f;
        var descTextGO = new GameObject("Description Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        descTextGO.transform.SetParent(viewGO.transform, false);
        var descTextRect = (RectTransform)descTextGO.transform;
        descTextRect.anchorMin = new Vector2(0f, 1f);
        descTextRect.anchorMax = new Vector2(1f, 1f);
        descTextRect.pivot = new Vector2(0.5f, 1f);
        descTextRect.sizeDelta = new Vector2(0f, 60f);
        descTextRect.anchoredPosition = new Vector2(0f, -descTextY);
        descriptionText = descTextGO.GetComponent<TextMeshProUGUI>();
        descriptionText.fontSize = 15f;
        descriptionText.color = Color.white;
        descriptionText.margin = new Vector4(24f, 0f, 24f, 0f);
        if (font != null) descriptionText.font = font;

        // 태그 칩 목록 (HorizontalLayoutGroup, 실제 칩은 런타임에 CraftMenuPanel이 채운다)
        const float tagsY = HeaderHeight + 24f + iconSize + 44f + 70f;
        var tagsGO = new GameObject("Tags", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        tagsGO.transform.SetParent(viewGO.transform, false);
        var tagsRect = tagsGO.GetComponent<RectTransform>();
        tagsRect.anchorMin = new Vector2(0f, 1f);
        tagsRect.anchorMax = new Vector2(1f, 1f);
        tagsRect.pivot = new Vector2(0.5f, 1f);
        tagsRect.sizeDelta = new Vector2(0f, 30f);
        tagsRect.anchoredPosition = new Vector2(0f, -tagsY);

        var hlg = tagsGO.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 0, 0);
        hlg.spacing = 8f;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;

        tagsContent = tagsRect;

        // 제조 시작 버튼 (우측 하단)
        const float startButtonSize = 48f;
        var startGO = new GameObject("Start Craft Button", typeof(RectTransform), typeof(Image), typeof(Button));
        startGO.transform.SetParent(viewGO.transform, false);
        var startRect = startGO.GetComponent<RectTransform>();
        startRect.anchorMin = new Vector2(1f, 0f);
        startRect.anchorMax = new Vector2(1f, 0f);
        startRect.pivot = new Vector2(1f, 0f);
        startRect.sizeDelta = new Vector2(startButtonSize, startButtonSize);
        startRect.anchoredPosition = new Vector2(-16f, 16f);
        startGO.GetComponent<Image>().color = new Color(0.82f, 0.62f, 0.38f, 1f);

        var startLabelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        startLabelGO.transform.SetParent(startGO.transform, false);
        var startLabelRect = (RectTransform)startLabelGO.transform;
        startLabelRect.anchorMin = Vector2.zero;
        startLabelRect.anchorMax = Vector2.one;
        startLabelRect.sizeDelta = Vector2.zero;
        var startLabel = startLabelGO.GetComponent<TextMeshProUGUI>();
        startLabel.text = "▶";
        startLabel.fontSize = 20f;
        startLabel.alignment = TextAlignmentOptions.Center;
        startLabel.color = Color.white;
        if (font != null) startLabel.font = font;

        startCraftButton = startGO.GetComponent<Button>();
        return viewGO;
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
