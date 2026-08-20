// Editor/CraftLoopSetup.cs
// 메뉴: Tools > Craft > Setup Craft Loop
// 선행 조건: Play 씬을 열어 두고 실행한다. 좌측 제조 메뉴(CraftMenuPanel)가 이미 있어야 한다.

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 열려 있는 씬에 1부 제조 루프의 오브젝트를 만들어 배선한다.
///
/// 흐름 컨트롤러와 기믹 실행기를 'Craft Loop' 아래에 놓고, 데이터 에셋과 좌측 제조 메뉴를 연결한다.
/// 메뉴에서 칵테일을 고르면 흐름이 시작되도록 끊겨 있던 자리를 잇는 게 목적이다.
///
/// 기믹은 바 위에 겹쳐 뜨는 게 아니라 화면을 통째로 차지하는 별도 화면이다. 그래서 기믹만 비추는
/// 카메라를 바에서 멀리 떨어진 자리에 함께 두고, 제조 중에는 바의 UI를 잠시 꺼 둔다.
///
/// 기믹 프리팹은 있는 것만 꽂고 없는 것은 로그로 알린다.
///
/// 씬을 직접 고치므로 결과를 확인한 뒤 Ctrl+S로 저장해야 한다.
/// </summary>
public static class CraftLoopSetup
{
    const string RootName = "Craft Loop";
    const string GimmickRootName = "Gimmick Root";
    const string HudName = "Gimmick HUD";
    const string GimmickCameraName = "Gimmick Camera";

    /// <summary>
    /// 기믹을 놓을 자리. 바에서 멀찍이 떨어뜨려 두면 기믹 카메라에 바가 함께 담기지 않는다.
    /// 레이어를 새로 만들거나 메인 카메라를 건드리지 않고 두 화면을 갈라놓는 방법이다.
    /// </summary>
    static readonly Vector3 GimmickStagePosition = new Vector3(0f, 1000f, 0f);
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";

    const string CocktailSOPath = "Assets/03.Scripts/DataNew/DataNewSO/NewCocktailDataSO.asset";
    const string ShelfSOPath = "Assets/03.Scripts/DataNew/DataNewSO/NewShelfItemDataSO.asset";
    const string BalanceSOPath = "Assets/03.Scripts/DataNew/DataNewSO/NewBalanceDataSO.asset";

    /// <summary>기믹 종류별 프리팹 경로.</summary>
    static readonly (ECraftGimmick type, string path)[] GimmickPrefabs =
    {
        (ECraftGimmick.Open, "Assets/04.Prefabs/Game/NewCapGame.prefab"),
        (ECraftGimmick.Pour, "Assets/04.Prefabs/Game/NewPourGame.prefab"),
        (ECraftGimmick.Shake, "Assets/04.Prefabs/Game/NewShakingMinigame.prefab"),
        (ECraftGimmick.Stir, "Assets/04.Prefabs/Game/NewStirGame.prefab"),
        // 필업은 따르기와 같은 화면을 쓴다. 실행기가 알아서 따르기 프리팹으로 넘기므로 항목을 두지 않는다.
    };

    [MenuItem("Tools/Craft/Setup Craft Loop")]
    public static void Run()
    {
        var menuPanel = Object.FindAnyObjectByType<CraftMenuPanel>(FindObjectsInactive.Include);
        if (menuPanel == null)
        {
            Debug.LogError("[CraftLoop] 씬에서 CraftMenuPanel을 찾지 못했습니다. " +
                           "Tools > Tycoon > Setup Craft Menu Panel 을 먼저 실행하세요.");
            return;
        }

        GameObject root = GameObject.Find(RootName) ?? new GameObject(RootName);

        var runner = GetOrAdd<GimmickRunner>(root);
        var flow = GetOrAdd<CraftFlowController>(root);

        // 띄운 기믹이 놓일 자리. 바에서 멀리 떨어뜨려 기믹 카메라에 바가 안 담기게 한다.
        Transform gimmickRoot = root.transform.Find(GimmickRootName);
        if (gimmickRoot == null)
        {
            gimmickRoot = new GameObject(GimmickRootName).transform;
            gimmickRoot.SetParent(root.transform, false);
        }
        gimmickRoot.position = GimmickStagePosition;

        CraftGimmickHud hud = BuildHud(root);
        Camera gimmickCamera = BuildGimmickCamera(gimmickRoot);

        // 기믹이 자기를 비추는 카메라를 찾아갈 수 있게 무대에 표시를 남긴다.
        var stage = gimmickRoot.GetComponent<CraftGimmickStage>() ??
                    gimmickRoot.gameObject.AddComponent<CraftGimmickStage>();

        var stageSo = new SerializedObject(stage);
        stageSo.FindProperty("stageCamera").objectReferenceValue = gimmickCamera;
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        WireRunner(runner, gimmickRoot, hud, gimmickCamera);
        WireFlow(flow, runner, menuPanel);

        EditorUtility.SetDirty(root);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = root;

        Debug.Log("[CraftLoop] 배선 완료. Ctrl+S로 씬을 저장하세요.\n" +
                  "메뉴에서 칵테일을 고르면 CraftFlowController가 시도를 열고, 제조 준비 화면이 없는 동안에는 " +
                  "정답 구성으로 자동 준비한 뒤 기믹 큐를 실행합니다(autoPrepareForTest).");
    }

    static void WireRunner(GimmickRunner runner, Transform gimmickRoot,
                           CraftGimmickHud hud, Camera gimmickCamera)
    {
        var so = new SerializedObject(runner);

        so.FindProperty("gimmickRoot").objectReferenceValue = gimmickRoot;
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("gimmickCamera").objectReferenceValue = gimmickCamera;

        SerializedProperty prefabs = so.FindProperty("prefabs");
        prefabs.arraySize = GimmickPrefabs.Length;

        for (int i = 0; i < GimmickPrefabs.Length; i++)
        {
            (ECraftGimmick type, string path) = GimmickPrefabs[i];

            SerializedProperty entry = prefabs.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("type").enumValueIndex = (int)type;

            GameObject prefab = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(path);

            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;

            if (prefab == null)
            {
                Debug.LogWarning($"[CraftLoop] '{type}' 기믹 프리팹이 없습니다. " +
                                 "해당 미니게임 씬의 루트를 프리팹으로 만들어 이 칸에 꽂아야 큐가 띄울 수 있습니다.");
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void WireFlow(CraftFlowController flow, GimmickRunner runner, CraftMenuPanel menuPanel)
    {
        var so = new SerializedObject(flow);

        so.FindProperty("menuPanel").objectReferenceValue = menuPanel;
        so.FindProperty("runner").objectReferenceValue = runner;

        // 메뉴를 품고 있는 슬라이드 패널. 제조가 시작되면 이걸 닫아야 메뉴가 제조 화면 위에 남지 않는다.
        var slidePanel = menuPanel.GetComponentInParent<LeftSlidePanel>(true);
        if (slidePanel == null)
            Debug.LogWarning("[CraftLoop] CraftMenuPanel 위쪽에서 LeftSlidePanel을 찾지 못했습니다. 패널이 자동으로 닫히지 않습니다.");

        so.FindProperty("craftPanel").objectReferenceValue = slidePanel;
        so.FindProperty("cocktailData").objectReferenceValue =
            LoadRequired<NewCocktailDataSO>(CocktailSOPath);
        so.FindProperty("shelfData").objectReferenceValue =
            LoadRequired<NewShelfItemDataSO>(ShelfSOPath);
        so.FindProperty("balanceData").objectReferenceValue =
            LoadRequired<NewBalanceDataSO>(BalanceSOPath);

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── 공통 표시 ───────────────────────────────────────────────────────

    /// <summary>
    /// 기믹 화면의 공통 표시를 만든다. 네 귀퉁이의 자리만 잡아 두는 placeholder이고,
    /// 실제 모양은 아트가 들어온 뒤에 붙인다.
    /// </summary>
    static CraftGimmickHud BuildHud(GameObject root)
    {
        Transform existing = root.transform.Find(HudName);
        if (existing != null) return existing.GetComponent<CraftGimmickHud>();

        var canvasGO = new GameObject(HudName, typeof(RectTransform), typeof(Canvas),
                                      typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(root.transform, false);

        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 기믹이 자기 캔버스를 갖고 있어서, 공통 표시는 그 위에 오도록 순서를 올려 둔다.
        canvas.sortingOrder = 100;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 실제 표시는 이 묶음 아래에 둔다. 기믹이 끝나면 통째로 꺼야 하기 때문이다.
        var panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasGO.transform, false);
        Stretch(panel.GetComponent<RectTransform>());

        TMP_Text progress = CreateLabel(panel.transform, "Progress", "",
            new Vector2(0f, 1f), new Vector2(40f, -40f), TextAlignmentOptions.TopLeft, 36f);
        TMP_Text subject = CreateLabel(panel.transform, "Subject", "",
            new Vector2(0.5f, 1f), new Vector2(0f, -40f), TextAlignmentOptions.Top, 40f);
        TMP_Text time = CreateLabel(panel.transform, "Time", "",
            new Vector2(1f, 1f), new Vector2(-40f, -40f), TextAlignmentOptions.TopRight, 36f);

        GameObject ok = CreateLabel(panel.transform, "OK", "O.K",
            new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center, 96f).gameObject;
        ok.SetActive(false);

        Button next = CreateNextButton(panel.transform);

        var hud = canvasGO.AddComponent<CraftGimmickHud>();
        var so = new SerializedObject(hud);
        so.FindProperty("root").objectReferenceValue = panel;
        so.FindProperty("progressText").objectReferenceValue = progress;
        so.FindProperty("subjectText").objectReferenceValue = subject;
        so.FindProperty("timeText").objectReferenceValue = time;
        so.FindProperty("okMark").objectReferenceValue = ok;
        so.FindProperty("nextButton").objectReferenceValue = next;
        so.ApplyModifiedPropertiesWithoutUndo();

        return hud;
    }

    /// <summary>
    /// 기믹만 비추는 카메라. 기믹이 놓인 먼 자리에 함께 두고, 배경을 단색으로 지운 뒤 그 위에 그린다.
    ///
    /// depth를 메인보다 높여 나중에 그리게 하고 화면 전체를 단색으로 덮으므로, 바의 배경과 손님이
    /// 통째로 사라진다. 화면에 겹쳐 그리는 UI는 카메라와 무관하게 맨 위에 나오므로 이걸로는 못 가린다 —
    /// 그쪽은 실행기가 제조 중에 잠시 꺼 둔다.
    /// </summary>
    static Camera BuildGimmickCamera(Transform gimmickRoot)
    {
        Transform existing = gimmickRoot.Find(GimmickCameraName);
        if (existing != null) return existing.GetComponent<Camera>();

        var go = new GameObject(GimmickCameraName, typeof(Camera));
        go.transform.SetParent(gimmickRoot, false);

        // 기믹은 이 자리의 z=0 평면에 놓인다. 카메라만 뒤로 물러서서 그 평면을 바라본다.
        go.transform.localPosition = new Vector3(0f, 0f, -10f);

        var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        // 메인 카메라보다 나중에 그려야 그 위를 덮는다.
        camera.depth = 100f;
        camera.orthographic = true;

        // 제조 중에만 켠다. 실행기가 켜고 끄면서 메인 카메라의 화각을 복사해 온다.
        go.SetActive(false);

        return camera;
    }

    static Button CreateNextButton(Transform parent)
    {
        var go = new GameObject("Next Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.sizeDelta = new Vector2(220f, 88f);
        rect.anchoredPosition = new Vector2(-40f, 40f);

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.15f);
        image.raycastTarget = true;

        // 코드로 붙인 버튼은 대상 그래픽이 비어 있다. 눌렀을 때 색이 변하는 반응이 없어서
        // 먹었는지 안 먹었는지 구분이 안 되므로 직접 이어 준다.
        go.GetComponent<Button>().targetGraphic = image;

        TMP_Text label = CreateLabel(go.transform, "Label", "다음",
            new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center, 36f);
        Stretch(label.rectTransform);

        return go.GetComponent<Button>();
    }

    static TMP_Text CreateLabel(Transform parent, string name, string content,
                                Vector2 anchor, Vector2 offset, TextAlignmentOptions align, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = new Vector2(600f, 120f);
        rect.anchoredPosition = offset;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = size;
        text.alignment = align;
        text.color = Color.white;

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font != null) text.font = font;

        return text;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    static T LoadRequired<T>(string path) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) Debug.LogError($"[CraftLoop] {typeof(T).Name}을 찾지 못했습니다: {path}");

        return asset;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
}
