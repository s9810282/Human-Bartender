// Editor 폴더 안에 넣어야 합니다: Assets/Editor/CutsceneTestTool.cs

using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class CutsceneTestTool : EditorWindow
{
    // ── 데이터 ────────────────────────────────────────────────────────
    private CutSceneDataSO  cutSceneData;
    private CutsceneManager cutsceneManager;

    // ── 필터/선택 ─────────────────────────────────────────────────────
    private string          searchFilter    = "";
    private string          selectedId      = "";
    private Vector2         listScrollPos;
    private Vector2         detailScrollPos;

    // ── 스타일 캐시 ───────────────────────────────────────────────────
    private GUIStyle        styleSelected;
    private GUIStyle        styleHeader;
    private GUIStyle        styleTag;
    private bool            stylesInitialized;

    // ── 상수 ──────────────────────────────────────────────────────────
    private static readonly Color ColorSelected  = new Color(0.22f, 0.47f, 0.75f);
    private static readonly Color ColorTransition = new Color(0.4f, 0.65f, 0.4f);
    private static readonly Color ColorComic     = new Color(0.65f, 0.45f, 0.75f);
    private static readonly Color ColorTag       = new Color(0.25f, 0.25f, 0.25f);

    [MenuItem("Tools/Cutscene Test Tool")]
    public static void Open()
    {
        var window = GetWindow<CutsceneTestTool>("Cutscene Test");
        window.minSize = new Vector2(680, 480);
    }

    // ── 스타일 초기화 ─────────────────────────────────────────────────
    void InitStyles()
    {
        if (stylesInitialized) return;

        styleSelected = new GUIStyle(EditorStyles.helpBox)
        {
            normal  = { background = MakeTex(1, 1, ColorSelected) },
            padding = new RectOffset(8, 8, 6, 6),
            margin  = new RectOffset(0, 0, 2, 2)
        };

        styleHeader = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13
        };

        styleTag = new GUIStyle(EditorStyles.miniLabel)
        {
            normal    = { background = MakeTex(1, 1, ColorTag), textColor = Color.white },
            padding   = new RectOffset(6, 6, 2, 2),
            margin    = new RectOffset(0, 4, 0, 0),
            alignment = TextAnchor.MiddleCenter,
            fixedHeight = 18
        };

        stylesInitialized = true;
    }

    Texture2D MakeTex(int w, int h, Color col)
    {
        var tex = new Texture2D(w, h);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    // ── GUI ───────────────────────────────────────────────────────────
    void OnGUI()
    {
        InitStyles();

        DrawToolbar();

        if (cutSceneData == null)
        {
            EditorGUILayout.HelpBox("CutSceneDataSO를 먼저 할당해주세요.", MessageType.Info);
            return;
        }

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
            GUILayout.Label("CutSceneDataSO", EditorStyles.toolbarButton, GUILayout.Width(120));
            cutSceneData = (CutSceneDataSO)EditorGUILayout.ObjectField(
                cutSceneData, typeof(CutSceneDataSO), false, GUILayout.Width(200));

            GUILayout.Space(12);

            GUILayout.Label("CutsceneManager", EditorStyles.toolbarButton, GUILayout.Width(120));
            cutsceneManager = (CutsceneManager)EditorGUILayout.ObjectField(
                cutsceneManager, typeof(CutsceneManager), true, GUILayout.Width(200));

            GUILayout.FlexibleSpace();

            // 씬에서 자동 탐색
            if (GUILayout.Button("씬에서 찾기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                cutsceneManager = FindObjectOfType<CutsceneManager>();
        }
    }

    // ── 좌측: 컷씬 목록 ──────────────────────────────────────────────
    void DrawLeftPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(260)))
        {
            // 검색창
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("🔍", GUILayout.Width(18));
                searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                    searchFilter = "";
            }

            // 목록
            listScrollPos = EditorGUILayout.BeginScrollView(listScrollPos);

            if (cutSceneData?.cutSceneData?.Cutscenes != null)
            {
                foreach (var cutscene in cutSceneData.cutSceneData.Cutscenes)
                {
                    if (!string.IsNullOrEmpty(searchFilter) &&
                        !cutscene.Id.Contains(searchFilter) &&
                        !(cutscene.Description?.Contains(searchFilter) ?? false))
                        continue;

                    DrawCutsceneListItem(cutscene);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }

    void DrawCutsceneListItem(Cutscene cutscene)
    {
        bool isSelected = cutscene.Id == selectedId;
        var style = isSelected ? styleSelected : EditorStyles.helpBox;

        using (new EditorGUILayout.VerticalScope(style))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                // 타입 태그
                Color tagColor = cutscene.Type == "comic" ? ColorComic : ColorTransition;
                var tagStyle = new GUIStyle(styleTag)
                {
                    normal = { background = MakeTex(1, 1, tagColor), textColor = Color.white }
                };
                GUILayout.Label(cutscene.Type ?? "?", tagStyle, GUILayout.Width(60));

                // ID
                GUILayout.Label(cutscene.Id, EditorStyles.boldLabel);
            }

            if (!string.IsNullOrEmpty(cutscene.Description))
                GUILayout.Label(cutscene.Description, EditorStyles.miniLabel);

            GUILayout.Label($"steps: {cutscene.Steps?.Length ?? 0}  |  blocking: {cutscene.Blocking}",
                EditorStyles.miniLabel);

            if (GUILayout.Button(isSelected ? "▶ 선택됨" : "선택", GUILayout.Height(22)))
                selectedId = cutscene.Id;
        }

        GUILayout.Space(2);
    }

    // ── 구분선 ────────────────────────────────────────────────────────
    void DrawDivider()
    {
        var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1), GUILayout.ExpandHeight(true));
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f));
    }

    // ── 우측: 상세 & 실행 ─────────────────────────────────────────────
    void DrawRightPanel()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            if (string.IsNullOrEmpty(selectedId))
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("좌측에서 컷씬을 선택하세요.", MessageType.None);
                GUILayout.FlexibleSpace();
                return;
            }

            Cutscene? cutscene = cutSceneData.cachedById.GetValueOrDefault(selectedId);
            if (cutscene == null)
            {
                EditorGUILayout.HelpBox("선택된 컷씬을 찾을 수 없습니다.", MessageType.Warning);
                return;
            }

            detailScrollPos = EditorGUILayout.BeginScrollView(detailScrollPos);

            DrawDetailHeader(cutscene.Value);
            GUILayout.Space(8);
            DrawStepList(cutscene.Value);

            EditorGUILayout.EndScrollView();

            DrawPlayButtons(cutscene.Value);
        }
    }

    void DrawDetailHeader(Cutscene cutscene)
    {
        GUILayout.Label(cutscene.Id, styleHeader);
        if (!string.IsNullOrEmpty(cutscene.Description))
            GUILayout.Label(cutscene.Description, EditorStyles.wordWrappedMiniLabel);

        GUILayout.Space(4);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawBadge("type",       cutscene.Type       ?? "-");
            DrawBadge("background", cutscene.Background ?? "-");
            DrawBadge("blocking",   cutscene.Blocking.ToString());
        }
    }

    void DrawBadge(string label, string value)
    {
        GUILayout.Label($"{label}: {value}", styleTag);
    }

    void DrawStepList(Cutscene cutscene)
    {
        GUILayout.Label("Steps", EditorStyles.boldLabel);

        if (cutscene.Steps == null || cutscene.Steps.Length == 0)
        {
            EditorGUILayout.HelpBox("steps가 없습니다.", MessageType.None);
            return;
        }

        foreach (var step in cutscene.Steps)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                // 타임라인 표시
                GUI.color = Color.cyan;
                GUILayout.Label($"{step.Time:F1}s", EditorStyles.boldLabel, GUILayout.Width(36));
                GUI.color = Color.white;

                // action 이름
                GUI.color = ActionColor(step.Action);
                GUILayout.Label(step.Action ?? "?", EditorStyles.boldLabel, GUILayout.Width(90));
                GUI.color = Color.white;

                // 상세 파라미터
                GUILayout.Label(StepSummary(step), EditorStyles.miniLabel);
            }
        }
    }

    Color ActionColor(string action) => action switch
    {
        "show_image"  => new Color(0.5f, 1f, 0.5f),
        "hide_image"  => new Color(1f, 0.6f, 0.4f),
        "hide_all"    => new Color(1f, 0.5f, 0.3f),     // 신규
        "show_layout" => new Color(0.4f, 0.8f, 1f),
        "effect"      => new Color(1f, 1f, 0.4f),
        "play_sfx"    => new Color(1f, 0.8f, 0.4f),
        "wait"        => Color.gray,
        _             => Color.white,
    };

    string StepSummary(CutsceneStep step) => step.Action switch
    {
        "show_image"  => $"{step.Image}  pos:{step.Position}  enter:{step.Enter}  dur:{step.EnterDuration:F1}s",
        "hide_image"  => $"{step.Image}  exit:{step.Exit ?? "fade_out"}  dur:{step.ExitDuration:F1}s",
        "hide_all"    => $"exit:{step.Exit ?? "fade_out"}  dur:{step.ExitDuration:F1}s",
        "show_layout" => FormatShowLayoutSummary(step),
        "effect"      => $"{step.EffectType}  intensity:{step.Intensity}  dur:{step.Duration:F1}s",
        "play_sfx"    => $"{step.Sfx}",
        "wait"        => $"dur:{step.Duration:F1}s",
        _             => "",
    };

    /// <summary>
    /// show_layout의 enters 배열 여부에 따라 요약 포맷 분기
    /// </summary>
    string FormatShowLayoutSummary(CutsceneStep step)
    {
        string imagesStr = step.Images != null ? string.Join(", ", step.Images) : "";

        if (step.Enters != null && step.Enters.Length > 0)
        {
            string entersStr = string.Join(", ", step.Enters);
            return $"layout:{step.Layout}  images:[{imagesStr}]  enters:[{entersStr}]";
        }
        else
        {
            return $"layout:{step.Layout}  images:[{imagesStr}]  enter:{step.Enter ?? "cut"}";
        }
    }

    // ── 재생 버튼 ─────────────────────────────────────────────────────
    void DrawPlayButtons(Cutscene cutscene)
    {
        EditorGUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            bool canPlay = Application.isPlaying && cutsceneManager != null;

            GUI.enabled = canPlay;
            if (GUILayout.Button("▶  재생", EditorStyles.toolbarButton, GUILayout.Height(28)))
                cutsceneManager.PlayCutSceneAsync(selectedId).Forget();

            if (GUILayout.Button("■  초기화", EditorStyles.toolbarButton, GUILayout.Width(80)))
                cutsceneManager.ResetImages();
            GUI.enabled = true;

            if (!Application.isPlaying)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("플레이 모드에서만 실행 가능합니다.", MessageType.None);
            }
            else if (cutsceneManager == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.HelpBox("CutsceneManager를 할당하세요.", MessageType.Warning);
            }
        }
    }
}
