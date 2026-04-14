// Editor/DialogueAnimationTestWindow.cs
// 메뉴: Tools > Dialogue > Animation Test

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DialogueAnimationTestWindow : EditorWindow
{
    // ─── 입력 ───────────────────────────────────────────
    private string _speaker    = "Miku";
    private string _expression = "Happy";

    // ─── 파트 개별 테스트 ───────────────────────────────
    private static readonly string[] PART_NAMES = { "body", "eyes", "eyebrows", "upper_face", "lower_face" };
    private bool[] _partEnabled = { true, true, true, true, true };

    // ─── 타겟 ────────────────────────────────────────────
    private DialogueCharacterManager _targetManager;

    // ─── 주소 미리보기 ───────────────────────────────────
    private bool _showAddressPreview = true;

    // ─── 로드 결과 로그 ──────────────────────────────────
    private readonly List<LogEntry> _logs = new();
    private Vector2 _logScroll;

    // ─── 빠른 테스트 프리셋 ──────────────────────────────
    private string _presetSpeaker    = "Miku";
    private string _newPresetExpression = "";
    private readonly List<string> _presetExpressions = new() { "Normal", "Happy", "Sad", "Angry", "Surprised" };

    // ─── 스타일 캐시 ─────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _logSuccessStyle;
    private GUIStyle _logWarningStyle;
    private GUIStyle _logErrorStyle;
    private bool _stylesInitialized;

    [MenuItem("Tools/Dialogue/Animation Test")]
    public static void Open()
    {
        var window = GetWindow<DialogueAnimationTestWindow>("Animation Test");
        window.minSize = new Vector2(380, 580);
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🎭 Dialogue Animation Test", _headerStyle);
        EditorGUILayout.Space(4);

        DrawTargetSection();
        EditorGUILayout.Space(8);
        DrawInputSection();
        EditorGUILayout.Space(8);
        DrawPartToggleSection();
        EditorGUILayout.Space(8);
        DrawAddressPreviewSection();
        EditorGUILayout.Space(8);
        DrawQuickPresetSection();
        EditorGUILayout.Space(8);
        DrawLogSection();
    }

    // ── 타겟 매니저 선택 ──────────────────────────────────
    private void DrawTargetSection()
    {
        EditorGUILayout.LabelField("Target", EditorStyles.boldLabel);
        using var horizontal = new EditorGUILayout.HorizontalScope();

        _targetManager = (DialogueCharacterManager)EditorGUILayout.ObjectField(
            _targetManager, typeof(DialogueCharacterManager), true);

        GUI.enabled = _targetManager == null;
        if (GUILayout.Button("Auto Find", GUILayout.Width(80)))
        {
            _targetManager = FindFirstObjectByType<DialogueCharacterManager>();
            if (_targetManager == null)
                AddLog(LogLevel.Warning, "씬에서 DialogueCharacterManager를 찾지 못했습니다.");
        }
        GUI.enabled = true;
    }

    // ── Speaker / Expression 입력 ─────────────────────────
    private void DrawInputSection()
    {
        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

        _speaker    = EditorGUILayout.TextField("Speaker",    _speaker);
        _expression = EditorGUILayout.TextField("Expression", _expression);

        EditorGUILayout.Space(4);

        using var horizontal = new EditorGUILayout.HorizontalScope();

        GUI.enabled = _targetManager != null && Application.isPlaying;
        if (GUILayout.Button("▶  Play", GUILayout.Height(30)))
            PlayAsync(_speaker, _expression).Forget();

        if (GUILayout.Button("⏹  Off", GUILayout.Height(30)))
        {
            _targetManager.OffCharacter();
            AddLog(LogLevel.Info, "OffCharacter 호출");
        }

        if (GUILayout.Button("🗑  Release", GUILayout.Height(30)))
        {
            _targetManager.ReleaseAll();
            AddLog(LogLevel.Info, "ReleaseAll 호출");
        }
        GUI.enabled = true;

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Play Mode에서만 실행 가능합니다.", MessageType.Info);
        }
    }

    // ── 파트 개별 활성화 토글 ─────────────────────────────
    private void DrawPartToggleSection()
    {
        EditorGUILayout.LabelField("Parts", EditorStyles.boldLabel);

        using var horizontal = new EditorGUILayout.HorizontalScope();
        for (int i = 0; i < PART_NAMES.Length; i++)
        {
            _partEnabled[i] = GUILayout.Toggle(
                _partEnabled[i],
                PART_NAMES[i],
                GUI.skin.button,
                GUILayout.Height(24));
        }
    }

    // ── 주소 미리보기 ─────────────────────────────────────
    private void DrawAddressPreviewSection()
    {
        _showAddressPreview = EditorGUILayout.Foldout(_showAddressPreview, "Address Preview", true);
        if (!_showAddressPreview) return;

        using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

        foreach (var part in PART_NAMES)
        {
            string baseAddr  = $"{_speaker}_{part}_{_expression}";
            EditorGUILayout.LabelField($"{part}", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.SelectableLabel($"{baseAddr}_Intro", EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.SelectableLabel($"{baseAddr}_Loop",  EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUI.indentLevel--;
        }
    }

    // ── 빠른 프리셋 ───────────────────────────────────────
    private void DrawQuickPresetSection()
    {
        EditorGUILayout.LabelField("Quick Presets", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            _presetSpeaker = EditorGUILayout.TextField(_presetSpeaker, GUILayout.Width(80));
            _newPresetExpression = EditorGUILayout.TextField(_newPresetExpression);
            if (GUILayout.Button("+", GUILayout.Width(24)) && !string.IsNullOrEmpty(_newPresetExpression))
            {
                if (!_presetExpressions.Contains(_newPresetExpression))
                    _presetExpressions.Add(_newPresetExpression);
                _newPresetExpression = "";
            }
        }

        EditorGUILayout.Space(2);

        // 버튼 그리드
        int columns = 3;
        for (int i = 0; i < _presetExpressions.Count; i += columns)
        {
            using var row = new EditorGUILayout.HorizontalScope();
            for (int j = i; j < Mathf.Min(i + columns, _presetExpressions.Count); j++)
            {
                string exp = _presetExpressions[j];
                GUI.enabled = _targetManager != null && Application.isPlaying;
                if (GUILayout.Button(exp, GUILayout.Height(22)))
                {
                    _speaker    = _presetSpeaker;
                    _expression = exp;
                    PlayAsync(_speaker, _expression).Forget();
                }
                GUI.enabled = true;
            }
        }
    }

    // ── 로그 섹션 ─────────────────────────────────────────
    private void DrawLogSection()
    {
        using var horizontal = new EditorGUILayout.HorizontalScope();
        EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
            _logs.Clear();

        float logHeight = Mathf.Max(80, position.height - 480);
        using var scrollView = new EditorGUILayout.ScrollViewScope(_logScroll, GUILayout.Height(logHeight));
        _logScroll = scrollView.scrollPosition;

        foreach (var entry in _logs)
        {
            var style = entry.Level switch
            {
                LogLevel.Success => _logSuccessStyle,
                LogLevel.Warning => _logWarningStyle,
                LogLevel.Error   => _logErrorStyle,
                _                => EditorStyles.miniLabel
            };
            EditorGUILayout.LabelField(entry.Message, style);
        }
    }

    // ── 재생 로직 ─────────────────────────────────────────
    private async UniTaskVoid PlayAsync(string speaker, string expression)
    {
        if (_targetManager == null)
        {
            AddLog(LogLevel.Error, "DialogueCharacterManager가 없습니다.");
            return;
        }

        AddLog(LogLevel.Info, $"[{speaker} / {expression}] 로드 시작...");

        // 파트 토글 반영: 비활성 파트 Animator를 임시로 끔
        ApplyPartToggles(false);

        // 주소별 사전 검증 (에디터에서 존재 여부 표시)
        await ValidateAddressesAsync(speaker, expression);

        await _targetManager.SetCharacterAsync(speaker, expression);

        AddLog(LogLevel.Success, $"[{speaker} / {expression}] SetCharacterAsync 완료");

        ApplyPartToggles(true);
        Repaint();
    }

    private void ApplyPartToggles(bool restore)
    {
        // DialogueCharacterManager의 parts 필드에 직접 접근
        // (SerializeField이므로 SerializedObject 사용)
        if (_targetManager == null) return;

        var so = new SerializedObject(_targetManager);
        var partsProp = so.FindProperty("parts");
        if (partsProp == null) return;

        Logger.Log(partsProp.arraySize);
        Logger.Log(PART_NAMES.Length);

        for (int i = 0; i < Mathf.Min(partsProp.arraySize, PART_NAMES.Length); i++)
        {
            var partProp    = partsProp.GetArrayElementAtIndex(i);
            var animProp    = partProp.FindPropertyRelative("Animator");
            var animatorObj = animProp?.objectReferenceValue as Animator;
            if (animatorObj == null) continue;

            if (!restore)
                animatorObj.enabled = _partEnabled[i];
        }
    }

    private async UniTask ValidateAddressesAsync(string speaker, string expression)
    {
        foreach (var part in PART_NAMES)
        {
            string loopAddr  = $"{speaker}_{part}_{expression}_Loop";
            string introAddr = $"{speaker}_{part}_{expression}_Intro";

            bool loopExists  = await CheckAddressExistsAsync(loopAddr);
            bool introExists = await CheckAddressExistsAsync(introAddr);

            if (loopExists)
            {
                string detail = introExists ? "(Intro+Loop)" : "(Loop only)";
                AddLog(LogLevel.Success, $"  {part}: 애니메이션 {detail}");
            }
            else
            {
                string spriteAddr = $"{speaker}_{part}_{expression}";
                bool spriteExists = await CheckAddressExistsAsync(spriteAddr);
                if (spriteExists)
                    AddLog(LogLevel.Info, $"  {part}: Sprite");
                else
                    AddLog(LogLevel.Warning, $"  {part}: 없음 (비활성 처리)");
            }
        }
    }

    private async UniTask<bool> CheckAddressExistsAsync(string address)
    {
        var handle = Addressables.LoadResourceLocationsAsync(address);
        await handle.ToUniTask();

        bool exists = handle.Status == AsyncOperationStatus.Succeeded && handle.Result.Count > 0;
        Addressables.Release(handle);
        return exists;
    }

    // ── 로그 유틸 ─────────────────────────────────────────
    private void AddLog(LogLevel level, string message)
    {
        string prefix = level switch
        {
            LogLevel.Success => "✓ ",
            LogLevel.Warning => "⚠ ",
            LogLevel.Error   => "✕ ",
            _                => "  "
        };
        _logs.Add(new LogEntry(level, $"{System.DateTime.Now:HH:mm:ss}  {prefix}{message}"));
        Repaint();
    }

    // ── 스타일 초기화 ─────────────────────────────────────
    private void InitStyles()
    {
        if (_stylesInitialized) return;
        _stylesInitialized = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.9f, 0.85f, 1f) }
        };

        _logSuccessStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.4f, 0.9f, 0.5f) } };
        _logWarningStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.85f, 0.3f) } };
        _logErrorStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
    }

    // ── 내부 타입 ─────────────────────────────────────────
    private enum LogLevel { Info, Success, Warning, Error }

    private class LogEntry
    {
        public LogLevel Level;
        public string Message;
        public LogEntry(LogLevel level, string message) { Level = level; Message = message; }
    }
}
