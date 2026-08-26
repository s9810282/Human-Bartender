// Editor/CraftPrepSceneSetup.cs
// 메뉴: Tools > Tycoon > Setup Craft Prep Scene

using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 제조 준비 화면을 혼자 켜서 확인할 수 있는 독립 테스트 씬(Assets/00.Scenes/CraftPrep.unity)과,
/// 그 화면을 그대로 담은 프리팹(Assets/04.Prefabs/Game/CraftPrepScreen.prefab)을 만든다.
///
/// 미니게임 씬들과 같은 방식이다 — 화면을 코드로 조립해 두면 아트가 없어도 흐름을 돌려 볼 수 있고,
/// 나중에 프리팹만 Play 씬에 얹으면 된다.
///
/// 배치는 준비 명세 §3의 순서를 그대로 옮겼다: 위쪽에 단계 탭(잔·도구·재료), 재료 단계에서만 뜨는
/// 선반 탭(술 선반·냉장고), 가운데 선반 격자, 아래에 고른 것이 쌓이는 트레이와 다음 버튼.
/// </summary>
public static class CraftPrepSceneSetup
{
    const string ScenePath = "Assets/00.Scenes/CraftPrep.unity";
    const string PrefabPath = "Assets/04.Prefabs/Game/CraftPrepScreen.prefab";
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";
    const string ShelfDataPath = "Assets/03.Scripts/DataNew/DataNewSO/NewShelfItemDataSO.asset";
    const string CocktailDataPath = "Assets/03.Scripts/DataNew/DataNewSO/NewCocktailDataSO.asset";
    const string DataLoaderPrefabPath = "Assets/04.Prefabs/ProjectLifeScope.prefab";

    const float CanvasW = 960f;
    const float CanvasH = 540f;

    /// <summary>바깥 여백. 네 변을 같은 값으로 둬야 격자가 가운데로 읽힌다.</summary>
    const float Pad = 24f;
    const float TopbarH = 40f;
    const float StageTabsH = 32f;
    const float ShelfTabsH = 32f;
    const float FooterH = 64f;

    static readonly Color Bg = new(0.07f, 0.07f, 0.09f, 1f);
    static readonly Color Panel = new(1f, 1f, 1f, 0.04f);
    static readonly Color Accent = new(0.83f, 0.55f, 0.18f, 0.9f);

    static TMP_FontAsset font;

    [MenuItem("Tools/Tycoon/Setup Craft Prep Scene")]
    public static void Run()
    {
        if (File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog(
                "CraftPrep 씬 다시 만들기",
                $"'{ScenePath}'가 이미 있습니다.\n덮어쓰면 씬에 직접 준 수정은 모두 사라집니다.\n계속할까요?",
                "덮어쓰기", "취소"))
        {
            return;
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        var shelfData = AssetDatabase.LoadAssetAtPath<NewShelfItemDataSO>(ShelfDataPath);
        var cocktailData = AssetDatabase.LoadAssetAtPath<NewCocktailDataSO>(CocktailDataPath);
        var loaderPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DataLoaderPrefabPath);

        if (font == null) Debug.LogWarning($"[CraftPrepSceneSetup] 폰트를 찾지 못했습니다: {FontAssetPath}");
        if (shelfData == null) Debug.LogWarning($"[CraftPrepSceneSetup] 선반 데이터를 찾지 못했습니다: {ShelfDataPath}");
        if (cocktailData == null) Debug.LogWarning($"[CraftPrepSceneSetup] 칵테일 데이터를 찾지 못했습니다: {CocktailDataPath}");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SetupCamera();
        CreateEventSystem();

        // json을 읽어 오는 로더. 이게 없으면 선반이 비어 있는 채로 뜬다.
        if (loaderPrefab != null) PrefabUtility.InstantiatePrefab(loaderPrefab);
        else Debug.LogWarning($"[CraftPrepSceneSetup] 데이터 로더 프리팹이 없습니다: {DataLoaderPrefabPath}");

        GameObject screen = BuildScreen(shelfData, cocktailData);

        SaveAsPrefab(screen);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log($"[CraftPrepSceneSetup] 씬 '{ScenePath}'과 프리팹 '{PrefabPath}'을 만들었습니다.");
    }

    /// <summary>
    /// 화면 전체를 만든다. 반환한 오브젝트가 그대로 프리팹이 되므로 씬에만 있는 것(카메라·EventSystem·
    /// 데이터 로더)은 이 아래에 넣지 않는다.
    /// </summary>
    static GameObject BuildScreen(NewShelfItemDataSO shelfData, NewCocktailDataSO cocktailData)
    {
        var canvasGo = new GameObject("Craft Prep Screen",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(CanvasW, CanvasH);
        scaler.matchWidthOrHeight = 0.5f;

        Transform root = canvasGo.transform;

        CreateImage(root, "Shell", Bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 상단: 칵테일 이름 + 레시피 노트
        RectTransform topbar = CreateRect(root, "Topbar",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(Pad, -Pad - TopbarH), new Vector2(-Pad, -Pad));

        TextMeshProUGUI title = CreateText(topbar, "Title", "칵테일", 20f, TextAlignmentOptions.Left);
        StretchWithRight(title.rectTransform, 150f);

        Button noteButton = CreateButton(topbar, "Recipe Note", "레시피 노트", 130f, TopbarH);
        AnchorRight(noteButton.GetComponent<RectTransform>(), 0f);

        // 단계 탭
        RectTransform stageTabs = CreateRow(root, "Stage Tabs", -Pad - TopbarH - 8f, StageTabsH);

        // 선반 탭 (재료 단계에서만 보인다)
        RectTransform shelfTabs = CreateRow(root, "Shelf Tabs", -Pad - TopbarH - StageTabsH - 16f, ShelfTabsH);

        // 선반 격자
        float shelfTop = Pad + TopbarH + StageTabsH + ShelfTabsH + 24f;
        RectTransform shelfPanel = CreateRect(root, "Shelf Panel",
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(Pad, Pad + FooterH + 8f), new Vector2(-Pad, -shelfTop));
        AddImage(shelfPanel.gameObject, Panel);

        RectTransform shelfContent = CreateRect(shelfPanel, "Shelf Content",
            Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));

        var grid = shelfContent.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(104f, 116f);
        grid.spacing = new Vector2(10f, 10f);
        grid.padding = new RectOffset(2, 2, 2, 2);
        grid.childAlignment = TextAnchor.UpperLeft;

        // 하단: 고른 것 + 다음
        RectTransform footer = CreateRect(root, "Footer",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(Pad, Pad), new Vector2(-Pad, Pad + FooterH));
        AddImage(footer.gameObject, Panel);

        RectTransform trayContent = CreateRect(footer, "Tray Content",
            new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(10f, 8f), new Vector2(-160f, -26f));

        var trayLayout = trayContent.gameObject.AddComponent<HorizontalLayoutGroup>();
        trayLayout.spacing = 6f;
        trayLayout.childAlignment = TextAnchor.MiddleLeft;
        trayLayout.childForceExpandWidth = false;
        trayLayout.childForceExpandHeight = false;
        trayLayout.childControlWidth = false;
        trayLayout.childControlHeight = false;

        TextMeshProUGUI notice = CreateText(footer, "Notice", "", 11.5f, TextAlignmentOptions.Left);
        notice.color = new Color(1f, 1f, 1f, 0.6f);
        var noticeRect = notice.rectTransform;
        noticeRect.anchorMin = new Vector2(0f, 0f);
        noticeRect.anchorMax = new Vector2(1f, 0f);
        noticeRect.offsetMin = new Vector2(12f, 4f);
        noticeRect.offsetMax = new Vector2(-160f, 22f);

        Button next = CreateButton(footer, "Next", "다음", 128f, 40f);
        var nextRect = next.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(1f, 0.5f);
        nextRect.anchorMax = new Vector2(1f, 0.5f);
        nextRect.pivot = new Vector2(1f, 0.5f);
        nextRect.anchoredPosition = new Vector2(-12f, 0f);
        next.GetComponent<Image>().color = Accent;

        // 컴포넌트를 붙이고 방금 만든 자리들을 꽂는다.
        var prep = canvasGo.AddComponent<CraftPrepScreen>();
        SetField(prep, "shelfData", shelfData);
        SetField(prep, "cocktailData", cocktailData);
        SetField(prep, "stageTabsContent", stageTabs);
        SetField(prep, "shelfTabsContent", shelfTabs);
        SetField(prep, "shelfContent", shelfContent);
        SetField(prep, "trayContent", trayContent);
        SetField(prep, "titleText", title);
        SetField(prep, "noticeText", notice);
        SetField(prep, "nextButton", next);
        SetField(prep, "recipeNoteButton", noteButton);
        SetField(prep, "font", font);
        SetBool(prep, "openOnStartForTest", true);

        return canvasGo;
    }

    // ── 조각 만들기 ─────────────────────────────────────────────────────

    static RectTransform CreateRow(Transform parent, string name, float top, float height)
    {
        RectTransform row = CreateRect(parent, name,
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(Pad, top - height), new Vector2(-Pad, top));

        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        return row;
    }

    static RectTransform CreateRect(Transform parent, string name,
                                    Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        return rect;
    }

    static Image AddImage(GameObject go, Color color)
    {
        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        return image;
    }

    static void CreateImage(Transform parent, string name, Color color,
                            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = CreateRect(parent, name, anchorMin, anchorMax, offsetMin, offsetMax);
        AddImage(rect.gameObject, color);
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size,
                                      TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = Color.white;
        label.alignment = alignment;
        label.raycastTarget = false;

        if (font != null) label.font = font;

        return label;
    }

    static Button CreateButton(Transform parent, string name, string label, float width, float height)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.sizeDelta = new Vector2(width, height);

        go.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

        TextMeshProUGUI text = CreateText(go.transform, "Label", label, 13f, TextAlignmentOptions.Center);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = Vector2.zero;
        text.rectTransform.offsetMax = Vector2.zero;

        return go.GetComponent<Button>();
    }

    static void StretchWithRight(RectTransform rect, float rightGap)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(-rightGap, 0f);
    }

    static void AnchorRight(RectTransform rect, float x)
    {
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = new Vector2(-x, 0f);
    }

    static void SetupCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Bg;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    static void CreateEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null) return;

        var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
    }

    static void SaveAsPrefab(GameObject screen)
    {
        string dir = Path.GetDirectoryName(PrefabPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

        PrefabUtility.SaveAsPrefabAssetAndConnect(screen, PrefabPath, InteractionMode.AutomatedAction);
    }

    // ── private 필드 채우기 ─────────────────────────────────────────────

    static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            Debug.LogWarning($"[CraftPrepSceneSetup] '{fieldName}' 필드를 찾지 못했습니다.");
            return;
        }

        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetBool(Object target, string fieldName, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop == null)
        {
            Debug.LogWarning($"[CraftPrepSceneSetup] '{fieldName}' 필드를 찾지 못했습니다.");
            return;
        }

        prop.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
