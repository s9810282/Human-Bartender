// Editor/PourSceneSetup.cs
// 메뉴: Tools > Tycoon > Setup Pour Scene

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
/// Pour(따르기) 미니게임을 혼자 켜서 확인할 수 있는 독립 테스트 씬(Assets/00.Scenes/Pour.unity)을
/// 새로 만들어 저장한다. Shake.unity/Stur.unity와 같은 역할 — isTest=true로 바로 Play 가능하게 배선한다.
/// 병/잔은 위쪽(주둥이 방향)이 뚫린 U자 콜라이더 컨테이너를 가지며, 그 안을 SPH 파티클로 채운다.
/// 병/잔 비주얼은 1유닛=1스케일짜리 흰 사각 스프라이트에 색만 입힌 placeholder이며, 실제 아트로 교체해야 한다.
/// 이미 Pour.unity가 있으면 덮어쓰지 않고 중단한다(재생성하려면 기존 씬을 지우고 다시 실행).
/// </summary>
public static class PourSceneSetup
{
    const string ScenePath = "Assets/00.Scenes/Pour.unity";
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";
    const string GradientMaterialPath = "Assets/03.Scripts/MiniGame/Shaker/New Material.mat";
    const string CraftStationDataPath = "Assets/03.Scripts/CraftLiquidData.asset";
    const string CategoryColorDataPath = "Assets/03.Scripts/MiniGame/Shaker/CategoryColorData.asset";
    const string CocktailDataSOPath = "Assets/SO/CocktailData.asset";
    const string CraftServePath = "Assets/03.Scripts/Craft/CraftServe.asset";
    const string CraftRetryPath = "Assets/03.Scripts/Craft/CraftRetry.asset";

    // PourManager.bottleInteriorHalfExtents / glassInteriorHalfExtents 기본값과 반드시 맞춰야 한다 —
    // 파티클이 처음 채워지는 영역(스크립트 쪽 계산)과 실제로 막아주는 벽(여기서 만드는 콜라이더)이
    // 어긋나면 파티클이 벽 밖에서 시작해버린다.
    static readonly Vector2 ContainerHalfExtents = new Vector2(0.48f, 0.48f);

    [MenuItem("Tools/Tycoon/Setup Pour Scene")]
    public static void Run()
    {
        if (File.Exists(ScenePath))
        {
            Debug.LogWarning($"[PourSceneSetup] '{ScenePath}'가 이미 있습니다. 덮어쓰지 않고 중단합니다. " +
                              "다시 만들려면 기존 씬 파일을 지우고 실행하세요.");
            return;
        }

        Sprite placeholderSprite = CreateOrLoadPlaceholderSprite();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        Material gradientMaterial = AssetDatabase.LoadAssetAtPath<Material>(GradientMaterialPath);
        CraftStationData craftStationData = AssetDatabase.LoadAssetAtPath<CraftStationData>(CraftStationDataPath);
        CategoryColorData categoryColorData = AssetDatabase.LoadAssetAtPath<CategoryColorData>(CategoryColorDataPath);
        CocktailDataSO cocktailDataSO = AssetDatabase.LoadAssetAtPath<CocktailDataSO>(CocktailDataSOPath);
        VoidEvent craftServe = AssetDatabase.LoadAssetAtPath<VoidEvent>(CraftServePath);
        VoidEvent craftRetry = AssetDatabase.LoadAssetAtPath<VoidEvent>(CraftRetryPath);

        if (font == null) Debug.LogWarning($"[PourSceneSetup] 폰트를 찾지 못했습니다: {FontAssetPath}");
        if (gradientMaterial == null) Debug.LogWarning($"[PourSceneSetup] 게이지 머티리얼을 찾지 못했습니다: {GradientMaterialPath}");
        if (craftStationData == null) Debug.LogWarning($"[PourSceneSetup] CraftStationData를 찾지 못했습니다: {CraftStationDataPath}");
        if (categoryColorData == null) Debug.LogWarning($"[PourSceneSetup] CategoryColorData를 찾지 못했습니다: {CategoryColorDataPath}");
        if (cocktailDataSO == null) Debug.LogWarning($"[PourSceneSetup] CocktailDataSO를 찾지 못했습니다: {CocktailDataSOPath}");
        if (craftServe == null) Debug.LogWarning($"[PourSceneSetup] CraftServe 이벤트를 찾지 못했습니다: {CraftServePath}");
        if (craftRetry == null) Debug.LogWarning($"[PourSceneSetup] CraftRetry 이벤트를 찾지 못했습니다: {CraftRetryPath}");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        SetupCamera();
        CreateEventSystem();

        Transform root = new GameObject("Pour Root").transform;

        BottleTiltController bottle = CreateBottle(root, placeholderSprite);
        Transform glassCenter = CreateGlass(root, placeholderSprite);
        SphLiquidRenderer liquidRenderer = CreateLiquidRenderer(root);
        CreateInputHandler(root, bottle);

        GradientRatioController gageBar = CreateGaugeCanvas(gradientMaterial);
        (Canvas buttonCanvas, Button serveButton, Button retryButton) = CreateButtonCanvas(font);

        PourManager manager = CreatePourManager(
            root, bottle, glassCenter, liquidRenderer, gageBar, buttonCanvas,
            craftStationData, categoryColorData, cocktailDataSO, craftServe, craftRetry);

        UnityEventTools.AddVoidPersistentListener(serveButton.onClick, manager.Serve);
        UnityEventTools.AddVoidPersistentListener(retryButton.onClick, manager.Retry);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        Debug.Log("[PourSceneSetup] 완료. Pour.unity가 생성되어 저장되었습니다. " +
                  "Play 전에 CraftLiquidData.asset의 targetCocktailId가 유효한 칵테일 id인지 확인하세요(Shake/Stur 테스트와 동일 조건). " +
                  "SPH 파티클 물리라 성능/느낌은 PourManager의 sphConfig, bottleParticleCount 값으로 튜닝해야 합니다. " +
                  "병/잔 비주얼과 배치는 placeholder이니 실제 아트로 교체해주세요.");
    }

    /// <summary>
    /// 4x4 흰색 텍스처를 PPU=4로 임포트한 스프라이트를 만들어(또는 이미 있으면 재사용) 반환한다.
    /// 내장 UISprite 리소스는 UI용으로 만들어져 있어 실제 PPU가 커서 월드 스페이스에 놓으면
    /// localScale을 아무리 키워도 매우 작게 보인다 — 그래서 1유닛=1스케일이 되는 스프라이트를 직접 만든다.
    /// PNG로 저장하는 실제 에셋이라 씬 저장 시에도 참조가 끊기지 않는다.
    /// </summary>
    static Sprite CreateOrLoadPlaceholderSprite()
    {
        const string path = "Assets/03.Scripts/MiniGame/Pour/PourPlaceholder.png";

        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null) return existing;

        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 4f;
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void SetupCamera()
    {
        Camera cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 3.5f;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f, 1f);
    }

    static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static BottleTiltController CreateBottle(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Bottle", typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(-1.6f, 1.2f, 0f);
        go.transform.localScale = new Vector3(1.4f, 3.6f, 1f);

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.75f, 0.8f, 0.85f, 0.4f); // 유리병 느낌의 반투명 placeholder — 안의 SPH 파티클이 실제 색을 보여준다
        sr.sortingOrder = 10;

        var controller = go.AddComponent<BottleTiltController>();
        SetSerializedField(controller, "bottleVisual", go.transform);

        // 위쪽(주둥이 방향, 로컬 +Y)만 뚫린 U자 컨테이너. Bottle 자신의 자식이라 기울기와 함께 회전한다.
        AddOpenTopWalls(go.transform, ContainerHalfExtents);

        return controller;
    }

    static Transform CreateGlass(Transform parent, Sprite sprite)
    {
        var go = new GameObject("Glass", typeof(SpriteRenderer));
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(1.4f, -1.0f, 0f);
        go.transform.localScale = new Vector3(1.6f, 2.7f, 1f);

        var sr = go.GetComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = new Color(0.85f, 0.85f, 0.9f, 0.35f);
        sr.sortingOrder = 5;

        // 잔은 회전하지 않으므로 그냥 위가 뚫린 U자 컨테이너를 월드에 고정.
        AddOpenTopWalls(go.transform, ContainerHalfExtents);

        return go.transform;
    }

    /// <summary>좌/우/바닥 3개의 BoxCollider2D로 위(로컬 +Y)만 뚫린 U자 컨테이너를 만든다.</summary>
    static void AddOpenTopWalls(Transform parent, Vector2 halfExtents)
    {
        // 너무 얇으면 압력힘으로 빠르게 밀린 파티클이 한 프레임 만에 통과(터널링)할 수 있어 넉넉히 둔다.
        const float thickness = 0.09f;

        AddWallSegment(parent, "Wall Left", new Vector2(-halfExtents.x, 0f), new Vector2(thickness, halfExtents.y * 2f));
        AddWallSegment(parent, "Wall Right", new Vector2(halfExtents.x, 0f), new Vector2(thickness, halfExtents.y * 2f));
        AddWallSegment(parent, "Wall Bottom", new Vector2(0f, -halfExtents.y), new Vector2(halfExtents.x * 2f, thickness));
    }

    static void AddWallSegment(Transform parent, string name, Vector2 localPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(BoxCollider2D));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;

        go.GetComponent<BoxCollider2D>().size = size;
    }

    static SphLiquidRenderer CreateLiquidRenderer(Transform parent)
    {
        var go = new GameObject("Liquid Renderer");
        go.transform.SetParent(parent, false);

        return go.AddComponent<SphLiquidRenderer>();
    }

    static void CreateInputHandler(Transform parent, BottleTiltController bottle)
    {
        var go = new GameObject("Input Handler");
        go.transform.SetParent(parent, false);

        var handler = go.AddComponent<PourInputHandler>();
        SetSerializedField(handler, "bottle", bottle);
    }

    static GradientRatioController CreateGaugeCanvas(Material gradientMaterial)
    {
        var canvasGO = new GameObject("Gauge Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);

        var gaugeGO = new GameObject("Gauge", typeof(RectTransform), typeof(Image));
        gaugeGO.transform.SetParent(canvasGO.transform, false);

        var rect = gaugeGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(300f, 24f);
        rect.anchoredPosition = new Vector2(0f, -20f);

        var image = gaugeGO.GetComponent<Image>();
        if (gradientMaterial != null) image.material = gradientMaterial;

        var gageBar = gaugeGO.AddComponent<GradientRatioController>();
        SetSerializedField(gageBar, "targetImage", image);

        return gageBar;
    }

    static (Canvas, Button, Button) CreateButtonCanvas(TMP_FontAsset font)
    {
        var canvasGO = new GameObject("Button Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);

        Button serveButton = CreateButton(canvasGO.transform, "Serve Button", "서빙", font, new Vector2(-70f, 40f));
        Button retryButton = CreateButton(canvasGO.transform, "Retry Button", "재시도", font, new Vector2(70f, 40f));

        canvasGO.SetActive(false); // OnNextButton()이 호출될 때까지 숨김 (Shake/Stur의 buttonCanvas와 동일)

        return (canvas, serveButton, retryButton);
    }

    static Button CreateButton(Transform parent, string name, string label, TMP_FontAsset font, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(120f, 48f);
        rect.anchoredPosition = anchoredPosition;

        go.GetComponent<Image>().color = new Color(0.82f, 0.62f, 0.38f, 1f);

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);

        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        var text = labelGO.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        if (font != null) text.font = font;

        return go.GetComponent<Button>();
    }

    static PourManager CreatePourManager(
        Transform parent, BottleTiltController bottle, Transform glassCenter,
        SphLiquidRenderer liquidRenderer,
        GradientRatioController gageBar, Canvas buttonCanvas,
        CraftStationData craftStationData, CategoryColorData categoryColorData, CocktailDataSO cocktailDataSO,
        VoidEvent craftServe, VoidEvent craftRetry)
    {
        var go = new GameObject("Pour Manager");
        go.transform.SetParent(parent, false);

        var manager = go.AddComponent<PourManager>();

        SetSerializedField(manager, "isTest", true);
        SetSerializedField(manager, "data", craftStationData);
        SetSerializedField(manager, "colorData", categoryColorData);
        SetSerializedField(manager, "cocktailDataSO", cocktailDataSO);
        SetSerializedField(manager, "bottle", bottle);
        SetSerializedField(manager, "glassCenter", glassCenter);
        SetSerializedField(manager, "liquidRenderer", liquidRenderer);
        SetSerializedField(manager, "gageBar", gageBar);
        SetSerializedField(manager, "buttonCanvas", buttonCanvas);
        SetSerializedField(manager, "craftServe", craftServe);
        SetSerializedField(manager, "craftRetry", craftRetry);

        return manager;
    }

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
}
