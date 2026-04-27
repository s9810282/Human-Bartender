// Assets/Editor/TestCutSceneEditorTool.cs

using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEditor;
using UnityEngine;

public class TestCutSceneEditorTool : EditorWindow
{
    // ── 할당 ──────────────────────────────────────────────────────────
    private TestCutScene        targetCutScene;
    private TestCutSceneManager cutSceneManager;

    // ── 스크롤 ────────────────────────────────────────────────────────
    private Vector2 listScrollPos;
    private Vector2 detailScrollPos;

    // ── 선택 ──────────────────────────────────────────────────────────
    private int selectedStepIndex = -1;

    // ── 스타일 ────────────────────────────────────────────────────────
    private bool     stylesInit;
    private GUIStyle styleHeader;
    private GUIStyle styleTag;
    private GUIStyle styleSelectedBox;
    private GUIStyle styleStepBox;
    private GUIStyle stylePlayingBox;

    // ── 색상 ──────────────────────────────────────────────────────────
    static readonly Color ColSelected   = new(0.22f, 0.47f, 0.75f);
    static readonly Color ColPlaying    = new(0.85f, 0.65f, 0.15f);
    static readonly Color ColShowImage  = new(0.5f, 1f, 0.5f);
    static readonly Color ColHideImage  = new(1f, 0.6f, 0.4f);
    static readonly Color ColHideAll    = new(1f, 0.45f, 0.3f);
    static readonly Color ColShowLayout = new(0.4f, 0.8f, 1f);
    static readonly Color ColTag        = new(0.25f, 0.25f, 0.25f);
    static readonly Color ColDivider    = new(0.15f, 0.15f, 0.15f);
    static readonly Color ColEase       = new(0.7f, 0.85f, 1f);
    static readonly Color ColEnterBar   = new(0.4f, 0.85f, 0.5f);
    static readonly Color ColExitBar    = new(1f, 0.55f, 0.35f);

    [MenuItem("Tools/Test CutScene Tool")]
    public static void Open()
    {
        var w = GetWindow<TestCutSceneEditorTool>("Test CutScene");
        w.minSize = new Vector2(720, 520);
    }

    void OnInspectorUpdate()
    {
        if (cutSceneManager != null && cutSceneManager.IsPlaying)
            Repaint();
    }

    // ── 스타일 초기화 ─────────────────────────────────────────────────
    void InitStyles()
    {
        if (stylesInit) return;

        styleHeader = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };

        styleTag = new GUIStyle(EditorStyles.miniLabel)
        {
            normal      = { background = MakeTex(ColTag), textColor = Color.white },
            padding     = new RectOffset(6, 6, 2, 2),
            margin      = new RectOffset(0, 4, 0, 0),
            alignment   = TextAnchor.MiddleCenter,
            fixedHeight = 18
        };

        styleSelectedBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal  = { background = MakeTex(ColSelected) },
            padding = new RectOffset(8, 8, 6, 6),
            margin  = new RectOffset(0, 0, 2, 2)
        };

        stylePlayingBox = new GUIStyle(EditorStyles.helpBox)
        {
            normal  = { background = MakeTex(ColPlaying) },
            padding = new RectOffset(8, 8, 6, 6),
            margin  = new RectOffset(0, 0, 2, 2)
        };

        styleStepBox = new GUIStyle(EditorStyles.helpBox)
        {
            padding = new RectOffset(8, 8, 4, 4),
            margin  = new RectOffset(0, 0, 1, 1)
        };

        stylesInit = true;
    }

    Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, col);
        t.Apply();
        return t;
    }

    // ── GUI 루트 ──────────────────────────────────────────────────────
    void OnGUI()
    {
        InitStyles();
        DrawToolbar();

        if (targetCutScene == null)
        {
            EditorGUILayout.HelpBox("TestCutScene SO를 할당해주세요.", MessageType.Info);
            return;
        }

        DrawProgressBar();

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLeftPanel();
            DrawDivider();
            DrawRightPanel();
        }
    }

    // ── 상단 툴바 ─────────────────────────────────────────────────────
    void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("CutScene SO", EditorStyles.toolbarButton, GUILayout.Width(90));
            targetCutScene = (TestCutScene)EditorGUILayout.ObjectField(
                targetCutScene, typeof(TestCutScene), false, GUILayout.Width(200));

            GUILayout.Space(12);

            GUILayout.Label("Manager", EditorStyles.toolbarButton, GUILayout.Width(60));
            cutSceneManager = (TestCutSceneManager)EditorGUILayout.ObjectField(
                cutSceneManager, typeof(TestCutSceneManager), true, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("씬에서 찾기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                cutSceneManager = FindObjectOfType<TestCutSceneManager>();
        }
    }

    // ── 프로그레스 바 ─────────────────────────────────────────────────
    void DrawProgressBar()
    {
        if (cutSceneManager == null || !cutSceneManager.IsPlaying) return;
        if (targetCutScene.steps == null || targetCutScene.steps.Count == 0) return;

        int current = cutSceneManager.CurrentStepIndex;
        int total   = targetCutScene.steps.Count;
        float progress = (float)(current + 1) / total;

        EditorGUI.ProgressBar(
            EditorGUILayout.GetControlRect(false, 18),
            progress,
            $"재생 중  Step {current + 1} / {total}");

        GUILayout.Space(2);
    }

    // ══════════════════════════════════════════════════════════════════
    //  좌측 패널
    // ══════════════════════════════════════════════════════════════════
    void DrawLeftPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(300)))
        {
            DrawCameraInfo();
            GUILayout.Space(4);
            DrawBGInfo();

            DrawHorizontalLine();

            GUILayout.Label($"Steps ({targetCutScene.steps?.Count ?? 0})", EditorStyles.boldLabel);

            listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);
            DrawStepList();
            EditorGUILayout.EndScrollView();

            DrawPlayButtons();
        }
    }

    void DrawCameraInfo()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("Camera (Root 패닝)", styleHeader);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawBadge("move", targetCutScene.isCutSceneMove
                    ? targetCutScene.cameraMoveType.ToString()
                    : "None");
                if (targetCutScene.isCutSceneMove)
                {
                    DrawBadge("dur", $"{targetCutScene.cameraDuration:F2}s");
                    DrawBadge("ease", targetCutScene.easeGraph.ToString());
                }
            }
        }
    }

    void DrawBGInfo()
    {
        if (!targetCutScene.isSetBGSprite) return;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("BG", styleHeader, GUILayout.Width(30));
                DrawBadge("path", string.IsNullOrEmpty(targetCutScene.bgPath)
                    ? "-" : targetCutScene.bgPath);
            }
        }
    }

    // ── 스텝 목록 ─────────────────────────────────────────────────────
    void DrawStepList()
    {
        if (targetCutScene.steps == null || targetCutScene.steps.Count == 0)
        {
            EditorGUILayout.HelpBox("steps가 비어있습니다.", MessageType.None);
            return;
        }

        int playingIndex = (cutSceneManager != null && cutSceneManager.IsPlaying)
            ? cutSceneManager.CurrentStepIndex
            : -1;

        for (int i = 0; i < targetCutScene.steps.Count; i++)
        {
            var step = targetCutScene.steps[i];
            bool isSel     = (i == selectedStepIndex);
            bool isPlaying = (i == playingIndex);

            GUIStyle boxStyle;
            if (isPlaying)       boxStyle = stylePlayingBox;
            else if (isSel)      boxStyle = styleSelectedBox;
            else                 boxStyle = styleStepBox;

            using (new EditorGUILayout.VerticalScope(boxStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (isPlaying)
                        GUILayout.Label("▶", GUILayout.Width(14));

                    GUI.color = Color.cyan;
                    GUILayout.Label($"{step.time:F2}s", EditorStyles.boldLabel, GUILayout.Width(45));
                    GUI.color = Color.white;

                    GUI.color = ActionColor(step.action);
                    GUILayout.Label(step.action.ToString(), EditorStyles.boldLabel, GUILayout.Width(85));
                    GUI.color = Color.white;

                    GUILayout.Label(TruncatePath(step.path, 20), EditorStyles.miniLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(50);
                    GUILayout.Label(StepSummary(step), EditorStyles.miniLabel);
                }

                if (GUILayout.Button(isSel ? "▶ 선택됨" : "선택", GUILayout.Height(20)))
                    selectedStepIndex = i;
            }
            GUILayout.Space(1);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  우측 패널 — Step 상세
    // ══════════════════════════════════════════════════════════════════
    void DrawRightPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (selectedStepIndex < 0 ||
                targetCutScene.steps == null ||
                selectedStepIndex >= targetCutScene.steps.Count)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("좌측에서 Step을 선택하세요.", MessageType.None);
                GUILayout.FlexibleSpace();
                return;
            }

            var step = targetCutScene.steps[selectedStepIndex];

            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            GUILayout.Label($"Step #{selectedStepIndex}", styleHeader);
            GUILayout.Space(6);

            // ── Basic ─────────────────────────────────────────────────
            DrawSection("Basic", () =>
            {
                DrawRow("Action",   step.action.ToString(),   ActionColor(step.action));
                DrawRow("Time",     $"{step.time:F2}s",       Color.cyan);
                DrawRow("Path",     step.path ?? "-",         Color.white);
                DrawRow("Duration", $"{step.duration:F2}s",   Color.white);
            });

            // ── Position ──────────────────────────────────────────────
            DrawSection("Position", () =>
            {
                DrawRow("Anchor",  step.positionPreset.Anchor.ToString(), Color.white);
                DrawRow("Offset",  $"({step.positionPreset.OffsetX:F2}, {step.positionPreset.OffsetY:F2})", Color.white);
            });

            // ── Enter ─────────────────────────────────────────────────
            bool isEnterSlide = IsSlideEnterType(step.enterType);
            DrawSection("Enter", () =>
            {
                DrawRow("Type",     step.enterType.ToString(),     ColShowImage);
                DrawRow("Duration", $"{step.enterDuration:F2}s",   Color.white);

                string easeName = step.enterEase == Ease.Unset ? "OutCubic (기본)" : step.enterEase.ToString();
                DrawRow("Ease",     easeName,                      ColEase);

                if (isEnterSlide)
                {
                    float dist = step.enterSlideDistance > 0 ? step.enterSlideDistance : 1.0f;
                    DrawRow("Distance", $"{dist:F2}x", Color.white);
                    DrawDistanceBar(dist, ColEnterBar);

                    if (step.enterSlideDistance <= 0)
                        EditorGUILayout.HelpBox("enterSlideDistance = 0 → 기본값 1.0 적용", MessageType.Info);
                }
            });

            // ── Exit ──────────────────────────────────────────────────
            bool isExitSlide = IsSlideExitType(step.exitType);
            DrawSection("Exit", () =>
            {
                Color exitCol = step.exitType == EExitPreset.None ? Color.gray : ColHideImage;
                DrawRow("Type",     step.exitType.ToString(),    exitCol);
                DrawRow("Duration", $"{step.exitDuration:F2}s",  Color.white);

                string easeName = step.exitEase == Ease.Unset ? "InCubic (기본)" : step.exitEase.ToString();
                DrawRow("Ease",     easeName,                    ColEase);

                if (isExitSlide)
                {
                    float dist = step.exitSlideDistance > 0 ? step.exitSlideDistance : 1.0f;
                    DrawRow("Distance", $"{dist:F2}x", Color.white);
                    DrawDistanceBar(dist, ColExitBar);

                    if (step.exitSlideDistance <= 0)
                        EditorGUILayout.HelpBox("exitSlideDistance = 0 → 기본값 1.0 적용", MessageType.Info);
                }

                if (step.exitType == EExitPreset.None)
                    EditorGUILayout.HelpBox("Exit None → HideAll Step에서 일괄 퇴장", MessageType.Info);
            });

            // ── 슬라이드 비교 (Enter/Exit 둘 다 슬라이드일 때) ────────
            if (isEnterSlide && isExitSlide)
            {
                DrawSection("Slide 비교", () =>
                {
                    float enterDist = step.enterSlideDistance > 0 ? step.enterSlideDistance : 1.0f;
                    float exitDist  = step.exitSlideDistance  > 0 ? step.exitSlideDistance  : 1.0f;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Enter", EditorStyles.miniLabel, GUILayout.Width(40));
                        DrawDistanceBar(enterDist, ColEnterBar);
                        GUILayout.Label($"{enterDist:F2}x", EditorStyles.miniLabel, GUILayout.Width(40));
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("Exit",  EditorStyles.miniLabel, GUILayout.Width(40));
                        DrawDistanceBar(exitDist, ColExitBar);
                        GUILayout.Label($"{exitDist:F2}x",  EditorStyles.miniLabel, GUILayout.Width(40));
                    }
                });
            }

            EditorGUILayout.EndScrollView();
        }
    }

    // ── 상세 헬퍼 ─────────────────────────────────────────────────────
    void DrawSection(string title, System.Action content)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label(title, EditorStyles.boldLabel);
            content();
        }
        GUILayout.Space(4);
    }

    void DrawRow(string label, string value, Color valueColor)
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.Width(70));
            GUI.color = valueColor;
            GUILayout.Label(value, EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
    }

    void DrawDistanceBar(float dist, Color barColor)
    {
        Rect barRect = EditorGUILayout.GetControlRect(false, 10);
        EditorGUI.DrawRect(barRect, new Color(0.2f, 0.2f, 0.2f));
        Rect fillRect = new Rect(barRect.x, barRect.y,
                                 barRect.width * Mathf.Clamp01(dist / 2f), barRect.height);
        EditorGUI.DrawRect(fillRect, barColor);
    }

    // ══════════════════════════════════════════════════════════════════
    //  재생 버튼
    // ══════════════════════════════════════════════════════════════════
    void DrawPlayButtons()
    {
        EditorGUILayout.Space(4);

        bool canPlay    = Application.isPlaying && cutSceneManager != null;
        bool isPlaying  = canPlay && cutSceneManager.IsPlaying;
        bool hasSelection = selectedStepIndex >= 0 &&
                            targetCutScene.steps != null &&
                            selectedStepIndex < targetCutScene.steps.Count;

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUI.enabled = canPlay && !isPlaying;
            if (GUILayout.Button("▶ 전체 재생", EditorStyles.toolbarButton, GUILayout.Height(26)))
                cutSceneManager.PlayComicCutSceneAsync(targetCutScene).Forget();

            GUI.enabled = canPlay && isPlaying;
            if (GUILayout.Button("■ 중지", EditorStyles.toolbarButton, GUILayout.Width(60), GUILayout.Height(26)))
                cutSceneManager.StopPlayback();

            GUI.enabled = canPlay;
            if (GUILayout.Button("↺ 초기화", EditorStyles.toolbarButton, GUILayout.Width(70), GUILayout.Height(26)))
                cutSceneManager.ClearCutScene();

            GUI.enabled = true;
        }

        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUI.enabled = canPlay && hasSelection && !isPlaying;

            if (GUILayout.Button("▶ 이 Step만", EditorStyles.toolbarButton, GUILayout.Height(26)))
            {
                var step = targetCutScene.steps[selectedStepIndex];
                cutSceneManager.PlaySingleStepAsync(step).Forget();
            }

            if (GUILayout.Button("▶ 여기서부터 재생", EditorStyles.toolbarButton, GUILayout.Height(26)))
                cutSceneManager.PlayFromStepAsync(targetCutScene, selectedStepIndex).Forget();

            GUI.enabled = true;
        }

        if (!Application.isPlaying)
            EditorGUILayout.HelpBox("플레이 모드에서만 실행 가능", MessageType.None);
        else if (cutSceneManager == null)
            EditorGUILayout.HelpBox("Manager를 할당하세요", MessageType.Warning);
        else if (!hasSelection)
            EditorGUILayout.HelpBox("Step을 선택하면 개별/부분 재생 가능", MessageType.Info);
    }

    // ══════════════════════════════════════════════════════════════════
    //  유틸리티
    // ══════════════════════════════════════════════════════════════════

    Color ActionColor(ECutSceneAction action) => action switch
    {
        ECutSceneAction.ShowImage  => ColShowImage,
        ECutSceneAction.HideImage  => ColHideImage,
        ECutSceneAction.HideAll    => ColHideAll,
        ECutSceneAction.ShowLayout => ColShowLayout,
        _                          => Color.white,
    };

    string StepSummary(TestCutSceneStep step)
    {
        switch (step.action)
        {
            case ECutSceneAction.ShowImage:
                string enterEase = step.enterEase != Ease.Unset ? $" ({step.enterEase})" : "";
                if (step.exitType != EExitPreset.None)
                {
                    string exitEase = step.exitEase != Ease.Unset ? $" ({step.exitEase})" : "";
                    return $"{step.enterType}{enterEase} {step.enterDuration:F1}s → hold:{step.duration:F1}s → {step.exitType}{exitEase} {step.exitDuration:F1}s";
                }
                return $"{step.enterType}{enterEase} {step.enterDuration:F1}s → hold:{step.duration:F1}s → (HideAll 대기)";

            case ECutSceneAction.HideImage:
            {
                string easeStr = step.exitEase != Ease.Unset ? $" ({step.exitEase})" : "";
                return $"exit:{step.exitType}{easeStr} {step.exitDuration:F1}s";
            }
            case ECutSceneAction.HideAll:
            {
                string easeStr = step.exitEase != Ease.Unset ? $" ({step.exitEase})" : "";
                return $"exit:{step.exitType}{easeStr} {step.exitDuration:F1}s  (전체 퇴장)";
            }
            case ECutSceneAction.ShowLayout:
            {
                string easeStr = step.enterEase != Ease.Unset ? $" ({step.enterEase})" : "";
                return $"enter:{step.enterType}{easeStr} {step.enterDuration:F1}s";
            }
            default:
                return "";
        }
    }

    bool IsSlideEnterType(EEneterPreset type) =>
        type == EEneterPreset.SlideLeft  || type == EEneterPreset.SlideRight ||
        type == EEneterPreset.SlideUp    || type == EEneterPreset.SlideDown;

    bool IsSlideExitType(EExitPreset type) =>
        type == EExitPreset.SlideLeft  || type == EExitPreset.SlideRight ||
        type == EExitPreset.SlideUp    || type == EExitPreset.SlideDown;

    string TruncatePath(string path, int max)
    {
        if (string.IsNullOrEmpty(path)) return "-";
        return path.Length <= max ? path : "…" + path.Substring(path.Length - max + 1);
    }

    void DrawBadge(string label, string value)
    {
        GUILayout.Label($"{label}: {value}", styleTag);
    }

    void DrawHorizontalLine()
    {
        GUILayout.Space(4);
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, ColDivider);
        GUILayout.Space(4);
    }

    void DrawDivider()
    {
        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, ColDivider);
    }
}
