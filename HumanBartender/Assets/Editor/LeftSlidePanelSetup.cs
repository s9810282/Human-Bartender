// Editor/LeftSlidePanelSetup.cs
// 메뉴: Tools > Tycoon > Setup Left Slide Panel

using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 버튼을 눌러 여닫는 슬라이드 패널(추후 제조 UI가 채워질 자리)을 현재 열려있는 씬에 자동으로
/// 배치한다. 씬을 새로 열거나 저장하지 않는다 — 현재 활성 씬을 직접 수정하므로, 결과를 확인한 뒤
/// 직접 Ctrl+S로 저장해야 한다. 위치/크기/색상은 자리만 잡아둔 placeholder이므로 에디터에서
/// 실제 비주얼에 맞게 다시 조정해야 한다.
/// </summary>
public static class LeftSlidePanelSetup
{
    const string CanvasName = "Left Slide Panel Canvas";
    const string PanelName = "Craft Panel";
    const string ButtonName = "Toggle Button";
    const string FontAssetPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";

    [MenuItem("Tools/Tycoon/Setup Left Slide Panel")]
    public static void Run()
    {
        if (GameObject.Find(CanvasName) != null)
        {
            Debug.LogWarning($"[LeftSlidePanelSetup] '{CanvasName}'가 이미 있습니다. 중복 생성을 막기 위해 건너뜁니다.");
            return;
        }

        var canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 21;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
            Debug.LogWarning($"[LeftSlidePanelSetup] {FontAssetPath}에서 폰트를 찾지 못했습니다. 기본 폰트로 생성합니다(한글 미지원 가능).");

        RectTransform panelRect = CreatePanel(canvasGO.transform);
        Button toggleButton = CreateToggleButton(panelRect, font);

        var slidePanel = canvasGO.AddComponent<LeftSlidePanel>();
        SetSerializedField(slidePanel, "panel", panelRect);
        SetSerializedField(slidePanel, "toggleButton", toggleButton);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[LeftSlidePanelSetup] 완료. Ctrl+S로 씬을 저장하세요. " +
                  "위치/크기/색상은 placeholder이니 실제 비주얼에 맞게 조정해주세요. 제조 UI 콘텐츠는 'Craft Panel' 안에 채워 넣으면 됩니다.");
    }

    static RectTransform CreatePanel(Transform parent)
    {
        const float width = 400f;

        var panelGO = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
        panelGO.transform.SetParent(parent, false);

        var rect = panelGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(width, 0f);
        rect.anchoredPosition = Vector2.zero; // 실제 위치는 LeftSlidePanel.Awake에서 화면 밖으로 옮긴다

        var image = panelGO.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.14f, 0.95f); // placeholder 배경색

        return rect;
    }

    static Button CreateToggleButton(RectTransform panelRect, TMP_FontAsset font)
    {
        const float buttonWidth = 48f;
        const float buttonHeight = 96f;

        var buttonGO = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(panelRect, false);

        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rect.anchoredPosition = Vector2.zero; // panel 오른쪽 바깥에 손잡이처럼 붙는다

        var image = buttonGO.GetComponent<Image>();
        image.color = new Color(0.82f, 0.62f, 0.38f, 1f); // placeholder 색 (코스터 트레이와 동일 톤)

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(buttonGO.transform, false);

        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        var label = labelGO.GetComponent<TextMeshProUGUI>();
        label.text = "제조";
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        if (font != null) label.font = font;

        return buttonGO.GetComponent<Button>();
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(fieldName).objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
