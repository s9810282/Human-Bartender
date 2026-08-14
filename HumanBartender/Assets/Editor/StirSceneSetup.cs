// Editor/StirSceneSetup.cs
// 메뉴: Tools > Tycoon > Setup Stir Scene

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 스터(Stir) 미니게임을 혼자 켜서 확인할 수 있는 독립 테스트 씬(Assets/00.Scenes/Stir.unity)을 만든다.
///
/// 화면 구성은 프로토타입 "이미지 레퍼런스/스터 미니게임.html"을 그대로 옮긴 것이다 —
/// 상단바(브랜드 + ELAPSED/CIRCLE/COMBO 스탯) · 왼쪽 사선 캐릭터 무대 · 사선 분할선 위의 진행 게이지 ·
/// 오른쪽 입력 무대(라운드 타이머 카드 + 아레나 + 하단 버튼) · 시작 오버레이.
///
/// 1500x844 기준의 프로토타입 치수를 960x540 캔버스로 환산해 배치했다(비율 0.64).
/// 캐릭터 무대 안의 그림(§2.1)과 잔 속 얼음(§2.2)은 아트 확정 후에 붙일 자리라 비워뒀다.
/// </summary>
public static class StirSceneSetup
{
    const string ScenePath = "Assets/00.Scenes/Stir.unity";
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";
    const string CraftStationDataPath = "Assets/03.Scripts/CraftLiquidData.asset";
    const string CocktailDataSOPath = "Assets/SO/CocktailData.asset";
    const string BalanceDataSOPath = "Assets/03.Scripts/DataNew/DataNewSO/NewBalanceDataSO.asset";
    const string CraftServePath = "Assets/03.Scripts/Craft/CraftServe.asset";
    const string CraftRetryPath = "Assets/03.Scripts/Craft/CraftRetry.asset";

    // ── 화면 치수 (960x540) ─────────────────────────────────────────────
    const float CanvasW = 960f;
    const float CanvasH = 540f;
    /// <summary>상단바 높이. 프로토타입 68px / 844px 비율.</summary>
    const float TopbarH = 44f;
    /// <summary>상단바 아래 놀이 영역의 높이.</summary>
    const float PlayH = CanvasH - TopbarH;

    /// <summary>사선이 놀이 영역 위쪽과 만나는 x (프로토타입 32%).</summary>
    const float DiagonalTopX = 0.32f * CanvasW;
    /// <summary>사선이 놀이 영역 아래쪽과 만나는 x (캐릭터 무대 폭 44%).</summary>
    const float DiagonalBottomX = 0.44f * CanvasW;

    /// <summary>아레나(원형 입력판) 지름.</summary>
    const float ArenaSize = 280f;
    /// <summary>아레나 중심에서 키 노드까지 (프로토타입의 8%/92% 배치 = 반지름의 84%).</summary>
    const float NodeRadius = ArenaSize * 0.42f;
    const float NodeSize = 50f;
    /// <summary>잔 지름. 아레나의 55%.</summary>
    const float GlassSize = ArenaSize * 0.55f;
    /// <summary>스푼 전체 길이. 아레나의 49%.</summary>
    const float SpoonLength = ArenaSize * 0.49f;
    /// <summary>스푼 피벗의 세로 위치(아래에서부터). 프로토타입 transform-origin 50% 86%.</summary>
    const float SpoonPivotY = 0.14f;

    // ── 팔레트 (프로토타입 :root 변수) ──────────────────────────────────
    static readonly Color Bg = Hex("090e12");
    static readonly Color Panel = Hex("0b1115");
    static readonly Color PanelSoft = Hex("10191e");
    static readonly Color TopbarBg = Hex("060a0d");
    static readonly Color Ink = Hex("edf7f5");
    static readonly Color Muted = Hex("7c8f93");
    static readonly Color Line = new Color(0.72f, 0.87f, 0.86f, 0.16f);
    static readonly Color Cyan = Hex("69f6e1");
    static readonly Color Lime = Hex("dfff6b");
    static readonly Color Violet = Hex("a58cff");
    /// <summary>아레나 원판. 패널보다 아주 조금 밝게 잡아 배경에서 살짝 떠 보이게 한다.</summary>
    static readonly Color ArenaFace = Hex("0d151a");

    static Sprite circleSprite;
    static Sprite roundedSprite;
    static TMP_FontAsset font;

    [MenuItem("Tools/Tycoon/Setup Stir Scene")]
    public static void Run()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog(
                "Stir 씬 다시 만들기",
                $"'{ScenePath}'가 이미 있습니다.\n덮어쓰면 씬에 직접 준 수정은 모두 사라집니다.\n계속할까요?",
                "덮어쓰기", "취소"))
        {
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        CraftStationData craftStationData = AssetDatabase.LoadAssetAtPath<CraftStationData>(CraftStationDataPath);
        CocktailDataSO cocktailDataSO = AssetDatabase.LoadAssetAtPath<CocktailDataSO>(CocktailDataSOPath);
        NewBalanceDataSO balanceDataSO = AssetDatabase.LoadAssetAtPath<NewBalanceDataSO>(BalanceDataSOPath);
        VoidEvent craftServe = AssetDatabase.LoadAssetAtPath<VoidEvent>(CraftServePath);
        VoidEvent craftRetry = AssetDatabase.LoadAssetAtPath<VoidEvent>(CraftRetryPath);

        if (font == null) Debug.LogWarning($"[StirSceneSetup] 폰트를 찾지 못했습니다: {FontAssetPath}");
        if (craftStationData == null) Debug.LogWarning($"[StirSceneSetup] CraftStationData를 찾지 못했습니다: {CraftStationDataPath}");
        if (cocktailDataSO == null) Debug.LogWarning($"[StirSceneSetup] CocktailDataSO를 찾지 못했습니다: {CocktailDataSOPath}");
        if (balanceDataSO == null) Debug.LogWarning($"[StirSceneSetup] NewBalanceDataSO를 찾지 못했습니다: {BalanceDataSOPath}");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SetupCamera();
        CreateEventSystem();

        Transform root = new GameObject("Stir Root").transform;
        Transform canvas = CreateCanvas("Game Canvas", 10).transform;

        CreateImage(canvas, "Shell", null, Bg, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);

        // 놀이 영역(상단바 아래). 이 안에 캐릭터 무대 · 입력 무대 · 사선 게이지가 겹쳐 놓인다.
        // 높이를 상단바만큼 줄이고 그 절반만큼 내려야 위가 상단바 바로 아래에서 시작한다.
        RectTransform playfield = CreateRect(canvas, "Playfield",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, -TopbarH * 0.5f), new Vector2(0f, -TopbarH));

        CreateImage(playfield, "Input Stage BG", null, Panel, Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        CreateCharacterStage(playfield);

        StirGaugeView gauge = CreateDiagonalGauge(playfield);
        (TMP_Text roundLimitText, TMP_Text roundTimeText, Image roundFill) = CreateRoundTimerCard(playfield);
        StirGlassView glass = CreateArena(playfield, out TMP_Text judgeText);
        CreateFooterLine(playfield);

        (TMP_Text elapsedText, TMP_Text circleText, TMP_Text comboText, TMP_Text drinkNameText) =
            CreateTopbar(canvas);

        GameObject startOverlay = CreateStartOverlay(playfield);

        StirHudView hud = CreateHud(root, elapsedText, circleText, comboText,
            roundLimitText, roundTimeText, roundFill, gauge, startOverlay, judgeText);

        (Canvas buttonCanvas, Button serveButton, Button retryButton) = CreateButtonCanvas();

        StirManager manager = CreateStirManager(root, glass, hud, buttonCanvas, drinkNameText,
            craftStationData, cocktailDataSO, balanceDataSO, craftServe, craftRetry);

        CreateInputHandler(root, manager);

        // AddListener는 런타임 등록이라 씬에 저장되지 않는다. 에디터에서 만드는 씬은 영속 리스너로 걸어야 한다.
        UnityEventTools.AddVoidPersistentListener(serveButton.onClick, manager.Serve);
        UnityEventTools.AddVoidPersistentListener(retryButton.onClick, manager.Retry);

        Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        Debug.Log($"[StirSceneSetup] '{ScenePath}' 생성 완료. 화면 구성은 '스터 미니게임.html'을 따랐습니다.\n" +
                  "W / ↑ 로 시작하고, 이후 스푼이 가리키는 방위의 시계 방향 이웃 키만 누르면 됩니다 (W→D→S→A→W).\n" +
                  "왼쪽 사선 무대(캐릭터 연출)와 잔 속 얼음은 아트 확정 후에 붙일 자리라 비워뒀습니다.");
    }

    // ── 뼈대 ────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 3.5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Bg;
    }

    static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static Canvas CreateCanvas(string name, int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    // ── 상단바 ──────────────────────────────────────────────────────────

    static (TMP_Text, TMP_Text, TMP_Text, TMP_Text) CreateTopbar(Transform canvas)
    {
        RectTransform bar = CreateRect(canvas, "Topbar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(0f, TopbarH));
        bar.pivot = new Vector2(0.5f, 1f);
        bar.anchoredPosition = Vector2.zero;

        AddImage(bar.gameObject, null, TopbarBg);

        // 아래쪽 구분선.
        CreateImage(bar, "Bottom Line", null, Line,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), Vector2.zero);

        // 브랜드 — 원형 마크 + 이름.
        RectTransform mark = CreateRect(bar, "Brand Mark",
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(17f, 0f), new Vector2(17f, 17f));
        mark.pivot = new Vector2(0f, 0.5f);
        AddImage(mark.gameObject, circleSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.14f));
        CreateText(mark, "L", "L", 9f, Cyan, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        CreateText(bar, "Brand Name", "PROJECT L.U.N.A", 10f, Ink, TextAlignmentOptions.Left,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(200f, 20f),
            new Vector2(0f, 0.5f));

        // 스탯 카드 3개 — 오른쪽 끝에서 왼쪽으로 쌓는다.
        const float cardW = 96f;
        const float cardH = 32f;
        const float gap = 6f;

        TMP_Text combo = CreateStatCard(bar, "Stat Combo", "COMBO / BEST", "0 / 0", Lime,
            -17f, cardW, cardH);
        TMP_Text circle = CreateStatCard(bar, "Stat Circle", "CIRCLE", "0 / 10", Ink,
            -17f - (cardW + gap), cardW, cardH);
        TMP_Text elapsed = CreateStatCard(bar, "Stat Elapsed", "ELAPSED TIME", "00:00.00", Cyan,
            -17f - (cardW + gap) * 2f, cardW, cardH);

        // 프로토타입에는 없지만 실제 흐름에서 어떤 칵테일인지 알아야 해서 가운데에 둔다.
        TMP_Text drinkName = CreateText(bar, "Drink Name", "드라이 마티니", 15f, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240f, 22f));

        return (elapsed, circle, combo, drinkName);
    }

    static TMP_Text CreateStatCard(Transform parent, string name, string caption, string value,
                                   Color valueColor, float right, float width, float height)
    {
        RectTransform card = CreateRect(parent, name,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(right, 0f), new Vector2(width, height));
        card.pivot = new Vector2(1f, 0.5f);
        AddImage(card.gameObject, null, new Color(1f, 1f, 1f, 0.018f));

        CreateImage(card, "Border", roundedSprite, new Color(0.72f, 0.87f, 0.86f, 0.13f),
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero).type = Image.Type.Sliced;

        CreateText(card, "Caption", caption, 7f, Muted, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -3f), new Vector2(width - 12f, 10f),
            new Vector2(1f, 1f));

        return CreateText(card, "Value", value, 13f, valueColor, TextAlignmentOptions.Right,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 3f), new Vector2(width - 12f, 16f),
            new Vector2(1f, 0f));
    }

    // ── 왼쪽 사선 무대 ──────────────────────────────────────────────────

    /// <summary>
    /// 캐릭터 연출이 들어갈 왼쪽 무대. 프로토타입은 clip-path로 오른쪽 변을 사선으로 잘랐는데,
    /// uGUI에는 그런 클리핑이 없어서 큼직한 사각형을 사선 각도만큼 돌려 오른쪽 변을 맞춘다.
    /// </summary>
    static void CreateCharacterStage(Transform playfield)
    {
        float dx = DiagonalBottomX - DiagonalTopX;
        float angle = Mathf.Atan2(dx, PlayH) * Mathf.Rad2Deg;   // 세로 기준 기울기

        var go = new GameObject("Character Stage", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(playfield, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);              // 오른쪽 변이 사선이 된다
        rect.sizeDelta = new Vector2(900f, PlayH * 1.6f); // 회전해도 왼쪽이 비지 않도록 넉넉하게
        rect.anchoredPosition = new Vector2((DiagonalTopX + DiagonalBottomX) * 0.5f, 0f);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        var image = go.GetComponent<Image>();
        image.color = PanelSoft;
        image.raycastTarget = false;

        // 아직 아무것도 없는 자리라는 걸 알려두는 안내. 아트가 들어오면 지운다.
        CreateText(rect, "Placeholder", "CHARACTER STAGE\n(연출 · 아트 대기)", 11f,
            new Color(Muted.r, Muted.g, Muted.b, 0.5f), TextAlignmentOptions.Center,
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(260f, 60f))
            .transform.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    /// <summary>사선 분할선 위에 얹히는 진행 게이지. 아래에서 위로 칸이 찬다.</summary>
    static StirGaugeView CreateDiagonalGauge(Transform playfield)
    {
        float dx = DiagonalBottomX - DiagonalTopX;
        float angle = Mathf.Atan2(dx, PlayH) * Mathf.Rad2Deg;

        const float gaugeW = 19f;
        float gaugeH = PlayH * 1.08f;

        var go = new GameObject("Diagonal Gauge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(playfield, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);              // 프로토타입 transform-origin: 50% 0
        rect.sizeDelta = new Vector2(gaugeW, gaugeH);
        rect.anchoredPosition = new Vector2(DiagonalTopX + gaugeW * 0.5f, PlayH * 0.02f);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.02f, 0.04f, 0.055f, 0.9f);
        image.raycastTarget = false;

        CreateImage(rect, "Border", roundedSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.33f),
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero).type = Image.Type.Sliced;

        // 칸 10개를 아래에서 위로 쌓는다.
        const int segmentCount = 10;
        const float pad = 4f;
        float inner = gaugeH - pad * 2f;
        float segH = (inner - (segmentCount - 1) * 3f) / segmentCount;

        var segments = new Image[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            float y = pad + i * (segH + 3f);
            RectTransform seg = CreateRect(rect, $"Segment {i}",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, y + segH * 0.5f),
                new Vector2(gaugeW - pad * 2f, segH));
            segments[i] = AddImage(seg.gameObject, null, new Color(1f, 1f, 1f, 0.025f));
        }

        var gauge = go.AddComponent<StirGaugeView>();
        SetSerializedArray(gauge, "segments", segments);

        return gauge;
    }

    // ── 오른쪽 입력 무대 ────────────────────────────────────────────────

    /// <summary>입력 무대 오른쪽 위의 라운드 타이머 카드.</summary>
    static (TMP_Text, TMP_Text, Image) CreateRoundTimerCard(Transform playfield)
    {
        const float cardW = 150f;
        const float cardH = 62f;

        RectTransform card = CreateRect(playfield, "Round Timer Card",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-18f, -16f), new Vector2(cardW, cardH));
        card.pivot = new Vector2(1f, 1f);
        AddImage(card.gameObject, null, new Color(1f, 1f, 1f, 0.018f));

        CreateImage(card, "Border", roundedSprite, new Color(0.72f, 0.87f, 0.86f, 0.14f),
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero).type = Image.Type.Sliced;

        CreateText(card, "Head", "ROUND LIMIT", 7f, Muted, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(80f, 10f),
            new Vector2(0f, 1f));

        TMP_Text limit = CreateText(card, "Limit", "2.00 SEC", 7f, Muted, TextAlignmentOptions.Right,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-8f, -6f), new Vector2(70f, 10f),
            new Vector2(1f, 1f));

        TMP_Text time = CreateText(card, "Time", "2.00", 20f, Lime, TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -18f), new Vector2(cardW - 16f, 24f),
            new Vector2(0f, 1f));

        RectTransform track = CreateRect(card, "Track",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 9f), new Vector2(-16f, 4f));
        track.anchoredPosition = new Vector2(0f, 9f);
        AddImage(track.gameObject, null, new Color(1f, 1f, 1f, 0.06f));

        Image fill = CreateImage(track, "Fill", roundedSprite, Lime,
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        return (limit, time, fill);
    }

    /// <summary>원형 입력판. 잔 · 스푼 시침 · 4방위 키 노드가 전부 이 안에 들어간다.</summary>
    static StirGlassView CreateArena(Transform playfield, out TMP_Text judgeText)
    {
        // 입력 무대(왼쪽 40% 제외)의 가로 중앙. 세로는 타이머 카드와 하단 버튼 사이의 가운데.
        float centerX = (0.40f * CanvasW + CanvasW) * 0.5f - CanvasW * 0.5f;
        const float centerY = -8f;

        RectTransform arena = CreateRect(playfield, "Arena",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(centerX, centerY),
            new Vector2(ArenaSize, ArenaSize));

        // 제한시간 링. 부채꼴로 차오르는 원을 깔고 그 위에 살짝 작은 원판을 덮어 고리만 남긴다.
        Image timerRing = CreateImage(arena, "Timer Ring", circleSprite, Cyan,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ArenaSize, ArenaSize));
        timerRing.type = Image.Type.Filled;
        timerRing.fillMethod = Image.FillMethod.Radial360;
        timerRing.fillOrigin = (int)Image.Origin360.Top;
        timerRing.fillClockwise = true;
        timerRing.fillAmount = 1f;

        // 이 원판은 불투명해야 한다 — Radial360은 원이 아니라 부채꼴로 차오르기 때문에
        // 가운데를 덮지 않으면 타이머가 고리가 아니라 파이 조각으로 보인다.
        CreateImage(arena, "Arena Face", circleSprite, ArenaFace,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(ArenaSize - 6f, ArenaSize - 6f));

        // 십자 보조선 (프로토타입 ::before / ::after).
        CreateImage(arena, "Cross H", null, new Color(Cyan.r, Cyan.g, Cyan.b, 0.06f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ArenaSize * 0.78f, 1f));
        CreateImage(arena, "Cross V", null, new Color(Cyan.r, Cyan.g, Cyan.b, 0.06f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1f, ArenaSize * 0.78f));

        CreateGlass(arena);
        RectTransform spoon = CreateSpoon(arena);

        var badges = new StirKeyBadge[StirDirections.Count];
        for (int i = 0; i < badges.Length; i++) badges[i] = CreateKeyNode(arena, i);

        // 프로토타입은 판정 문구를 화면에 띄우지 않지만(sr-only), 테스트 씬에서는 보이는 편이 낫다.
        // 아레나 아래, 노드의 순번 표시와 하단 구분선 사이에 끼워 넣는다.
        judgeText = CreateText(playfield, "Judge", string.Empty, 14f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(centerX, centerY - ArenaSize * 0.5f - 25f), new Vector2(420f, 20f));

        var view = arena.gameObject.AddComponent<StirGlassView>();
        SetSerializedField(view, "spoon", spoon);
        SetSerializedField(view, "timerRing", timerRing);
        SetSerializedArray(view, "badges", badges);

        return view;
    }

    static void CreateGlass(Transform arena)
    {
        CreateImage(arena, "Glass Shadow", circleSprite, new Color(0f, 0f, 0f, 0.45f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -ArenaSize * 0.04f),
            new Vector2(ArenaSize * 0.6f, ArenaSize * 0.6f));

        // 테두리(밝은 링) 위에 안쪽 원판을 덮어 프로토타입의 두꺼운 유리 테두리를 흉내낸다.
        CreateImage(arena, "Glass Rim", circleSprite, new Color(0.82f, 0.93f, 0.93f, 0.43f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(GlassSize, GlassSize));

        CreateImage(arena, "Glass Body", circleSprite, new Color(0.17f, 0.35f, 0.35f, 0.95f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(GlassSize - 9f, GlassSize - 9f));

        CreateImage(arena, "Liquid Swirl", circleSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.10f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(GlassSize * 0.82f, GlassSize * 0.82f));

        // TODO(얼음): 잔 속 얼음은 아트 확정 후 여기에 붙인다. StirManager의 정답 판정 지점에서
        //             휘돌림 임펄스를 받아 도는 구조로 계획해 뒀다.
    }

    /// <summary>
    /// 바 스푼 시침. 프로토타입처럼 헤드(bowl)가 잔 중심에 놓이고 손잡이 쪽 긴 축이 현재 방위를 가리킨다.
    /// 피벗을 아래에서 14% 지점에 두면 그 점이 곧 회전 중심이 된다.
    /// </summary>
    static RectTransform CreateSpoon(Transform arena)
    {
        RectTransform orbit = CreateRect(arena, "Spoon Orbit",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, SpoonLength));
        orbit.pivot = new Vector2(0.5f, SpoonPivotY);
        orbit.anchoredPosition = Vector2.zero;

        // 축 — 위(바깥)로 뻗는다.
        CreateImage(orbit, "Shaft", roundedSprite, new Color(0.72f, 0.78f, 0.79f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 0f),
            new Vector2(3f, SpoonLength * 0.88f)).GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1f);

        // 손잡이 — 축의 바깥 끝.
        CreateImage(orbit, "Handle", circleSprite, new Color(0.84f, 0.88f, 0.89f),
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 5f), new Vector2(6f, 12f));

        // 헤드 — 잔 중심(피벗)에 온다.
        CreateImage(orbit, "Bowl", circleSprite, new Color(0.78f, 0.83f, 0.84f),
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, SpoonLength * SpoonPivotY),
            new Vector2(10f, 18f));

        return orbit;
    }

    static StirKeyBadge CreateKeyNode(Transform arena, int direction)
    {
        // W가 위, 시계 방향으로 D·S·A.
        float radians = (StirDirections.ToAngle(direction) + 90f) * Mathf.Deg2Rad;
        var position = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * NodeRadius;

        RectTransform node = CreateRect(arena, $"Key {StirDirections.KeyLabel(direction)}",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(NodeSize, NodeSize));

        // Frame이 바깥, Fill이 그 위에 조금 작게 얹히며 남는 가장자리가 테두리가 된다. 색이 바뀌는 건 Frame이다.
        Image frame = CreateImage(node, "Frame", circleSprite, new Color(0.67f, 0.79f, 0.79f, 0.3f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(NodeSize, NodeSize));

        CreateImage(node, "Fill", circleSprite, new Color(0.07f, 0.10f, 0.12f, 0.98f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(NodeSize - 4f, NodeSize - 4f));

        TMP_Text letter = CreateText(node, "Letter", StirDirections.KeyLabel(direction), 18f,
            new Color(0.93f, 0.97f, 0.96f, 0.58f), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(NodeSize, NodeSize));

        TMP_Text alias = CreateText(node, "Alias", StirDirections.ArrowLabel(direction), 8f,
            Muted, TextAlignmentOptions.Right,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-6f, 5f), new Vector2(16f, 12f),
            new Vector2(1f, 0f));

        TMP_Text order = CreateText(node, "Step Order", StepOrderLabel(direction), 7f,
            Muted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, -8f), new Vector2(70f, 10f),
            new Vector2(0.5f, 1f));

        var badge = node.gameObject.AddComponent<StirKeyBadge>();
        SetSerializedField(badge, "frame", frame);
        SetSerializedField(badge, "letter", letter);
        SetSerializedField(badge, "alias", alias);
        SetSerializedField(badge, "stepOrder", order);

        return badge;
    }

    /// <summary>노드 아래 순번. W는 시작점이자 한 바퀴의 끝이라 프로토타입처럼 START / 04로 적는다.</summary>
    static string StepOrderLabel(int direction) =>
        direction == (int)EStirDirection.Up ? "START / 04" : $"0{direction}";

    static void CreateFooterLine(Transform playfield)
    {
        // 하단 버튼 줄 위의 구분선. 프로토타입 .input-footer의 border-top.
        RectTransform line = CreateRect(playfield, "Footer Line",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 56f), new Vector2(0f, 1f));
        line.offsetMin = new Vector2(0.40f * CanvasW, line.offsetMin.y);
        AddImage(line.gameObject, null, Line);
    }

    // ── 오버레이 ────────────────────────────────────────────────────────

    /// <summary>시작 대기 카드. 첫 W 입력에 StirManager가 끈다.</summary>
    static GameObject CreateStartOverlay(Transform playfield)
    {
        RectTransform overlay = CreateRect(playfield, "Start Overlay",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        AddImage(overlay.gameObject, null, new Color(0.024f, 0.04f, 0.05f, 0.56f));

        // 프로토타입은 padding-left 36%로 카드를 입력 무대 쪽에 붙인다.
        float cardX = (0.36f * CanvasW + CanvasW) * 0.5f - CanvasW * 0.5f;

        RectTransform card = CreateRect(overlay, "Start Card",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(cardX, 0f), new Vector2(250f, 170f));
        AddImage(card.gameObject, null, new Color(0.03f, 0.055f, 0.067f, 0.93f));

        CreateImage(card, "Border", roundedSprite, new Color(Cyan.r, Cyan.g, Cyan.b, 0.31f),
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero).type = Image.Type.Sliced;

        CreateText(card, "Kicker", "STIR GIMMICK", 8f, Violet, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(220f, 12f),
            new Vector2(0.5f, 1f));

        CreateText(card, "Title", "10바퀴를 저어 완성하세요", 16f, Ink, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(230f, 24f),
            new Vector2(0.5f, 1f));

        CreateText(card, "Desc",
            "처음 W로 시작점을 잡습니다.\n실패하면 그 위치가 새로운 시작점이 되며,\n같은 위치로 돌아오면 한 바퀴가 완성됩니다.",
            9f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(220f, 44f),
            new Vector2(0.5f, 1f));

        RectTransform key = CreateRect(card, "Start Key",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(46f, 46f));
        AddImage(key.gameObject, circleSprite, new Color(Lime.r, Lime.g, Lime.b, 0.08f));
        CreateText(key, "Letter", "W", 18f, Lime, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return overlay.gameObject;
    }

    static (Canvas, Button, Button) CreateButtonCanvas()
    {
        Canvas canvas = CreateCanvas("Button Canvas", 30);

        // 프로토타입의 '제조 종료' 버튼 자리(오른쪽 아래)에 맞춘다.
        Button serve = CreateButton(canvas.transform, "Serve Button", "서빙", new Vector2(-104f, 18f));
        Button retry = CreateButton(canvas.transform, "Retry Button", "재시도", new Vector2(-18f, 18f));

        canvas.gameObject.SetActive(false); // OnNextButton()이 호출될 때까지 숨김 (Cap/Pour와 동일)

        return (canvas, serve, retry);
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(80f, 30f);
        rect.anchoredPosition = anchoredPosition;

        go.GetComponent<Image>().color = Lime;

        CreateText(rect, "Label", label, 12f, new Color(0.06f, 0.09f, 0.04f), TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return go.GetComponent<Button>();
    }

    // ── 컴포넌트 ────────────────────────────────────────────────────────

    static StirHudView CreateHud(Transform parent,
        TMP_Text elapsed, TMP_Text circle, TMP_Text combo,
        TMP_Text roundLimit, TMP_Text roundTime, Image roundFill,
        StirGaugeView gauge, GameObject startOverlay, TMP_Text judge)
    {
        var go = new GameObject("Stir HUD");
        go.transform.SetParent(parent, false);

        var hud = go.AddComponent<StirHudView>();

        SetSerializedField(hud, "elapsedText", elapsed);
        SetSerializedField(hud, "circleText", circle);
        SetSerializedField(hud, "comboText", combo);
        SetSerializedField(hud, "roundLimitText", roundLimit);
        SetSerializedField(hud, "roundTimeText", roundTime);
        SetSerializedField(hud, "roundTimeFill", roundFill);
        SetSerializedField(hud, "gauge", gauge);
        SetSerializedField(hud, "startOverlay", startOverlay);
        SetSerializedField(hud, "judgeText", judge);

        return hud;
    }

    static StirManager CreateStirManager(
        Transform parent, StirGlassView glass, StirHudView hud, Canvas buttonCanvas, TMP_Text drinkNameText,
        CraftStationData craftStationData, CocktailDataSO cocktailDataSO, NewBalanceDataSO balanceDataSO,
        VoidEvent craftServe, VoidEvent craftRetry)
    {
        var go = new GameObject("Stir Manager");
        go.transform.SetParent(parent, false);

        var manager = go.AddComponent<StirManager>();

        SetSerializedField(manager, "isTest", true);
        SetSerializedField(manager, "data", craftStationData);
        SetSerializedField(manager, "cocktailDataSO", cocktailDataSO);
        SetSerializedField(manager, "balanceData", balanceDataSO);
        SetSerializedField(manager, "glass", glass);
        SetSerializedField(manager, "hud", hud);
        SetSerializedField(manager, "buttonCanvas", buttonCanvas);
        SetSerializedField(manager, "drinkNameText", drinkNameText);
        SetSerializedField(manager, "craftServe", craftServe);
        SetSerializedField(manager, "craftRetry", craftRetry);

        return manager;
    }

    static void CreateInputHandler(Transform parent, StirManager manager)
    {
        var go = new GameObject("Stir Input");
        go.transform.SetParent(parent, false);

        var handler = go.AddComponent<StirInputHandler>();
        SetSerializedField(handler, "stir", manager);
    }

    // ── UI 헬퍼 ─────────────────────────────────────────────────────────

    static RectTransform CreateRect(Transform parent, string name,
                                    Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        return rect;
    }

    static Image AddImage(GameObject go, Sprite sprite, Color color)
    {
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    static Image CreateImage(Transform parent, string name, Sprite sprite, Color color,
                             Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, anchoredPosition, sizeDelta);
        return AddImage(rect.gameObject, sprite, color);
    }

    static TMP_Text CreateText(Transform parent, string name, string content, float size, Color color,
                               TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 anchoredPosition, Vector2 sizeDelta)
        => CreateText(parent, name, content, size, color, align, anchorMin, anchorMax,
                      anchoredPosition, sizeDelta, new Vector2(0.5f, 0.5f));

    static TMP_Text CreateText(Transform parent, string name, string content, float size, Color color,
                               TextAlignmentOptions align, Vector2 anchorMin, Vector2 anchorMax,
                               Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = anchoredPosition;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = color;
        text.raycastTarget = false;
        if (font != null) text.font = font;

        return text;
    }

    static Color Hex(string rgb)
    {
        ColorUtility.TryParseHtmlString("#" + rgb, out Color color);
        return color;
    }

    // ── 직렬화 헬퍼 ─────────────────────────────────────────────────────

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetSerializedField(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).boolValue = value;
        so.ApplyModifiedProperties();
    }

    static void SetSerializedArray(Object target, string fieldName, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty property = so.FindProperty(fieldName);

        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        so.ApplyModifiedProperties();
    }
}
