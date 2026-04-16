// Editor/SpriteSheetAnimationCreator.cs
// 메뉴: Tools > Sprite Sheet Animation Creator

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

public class SpriteSheetAnimationCreator : EditorWindow
{
    // ─── 공통 설정 ──────────────────────────────────────
    private string _characterName = "luna";
    private string _expression    = "joy";
    private string _clipState     = "Loop";
    private int _frameRate        = 12;
    private bool _isLoop          = true;
    private string _addressableGroup = "Default Local Group";

    // ─── 파츠 엔트리 리스트 ─────────────────────────────
    [System.Serializable]
    private class PartEntry
    {
        public string partName    = "";
        public Texture2D texture  = null;
        public int columns        = 4;
        public int rows           = 1;
        public bool enabled       = true;

        public string GetAddress(string character, string expression, string state)
            => $"{character}_{partName}_{expression}_{state}";

        public string GetClipName(string character, string expression, string state)
            => $"{character}_{partName}_{expression}_{state}";
    }

    private static readonly string[] DEFAULT_PARTS = { "eyes", "eyebrows", "upper_face", "lower_face", "body", "extra" };

    private List<PartEntry> _entries = new();
    private Vector2 _entryScroll;

    // ─── 로그 ───────────────────────────────────────────
    private readonly List<string> _logs = new();
    private Vector2 _logScroll;

    // ─── 스타일 ─────────────────────────────────────────
    private GUIStyle _headerStyle;
    private GUIStyle _successStyle;
    private GUIStyle _warningStyle;
    private GUIStyle _errorStyle;
    private bool _stylesInit;

    [MenuItem("Tools/Sprite Sheet Animation Creator")]
    public static void Open()
    {
        var window = GetWindow<SpriteSheetAnimationCreator>("Batch Anim Creator");
        window.minSize = new Vector2(500, 700);
    }

    private void OnEnable()
    {
        if (_entries.Count == 0)
            ResetToDefaultParts();
    }

    private void OnGUI()
    {
        InitStyles();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("🎞 Batch Sprite Sheet → Animation Clips", _headerStyle);
        EditorGUILayout.Space(6);

        DrawCommonSection();
        EditorGUILayout.Space(8);
        DrawPartsSection();
        EditorGUILayout.Space(8);
        DrawAddressPreview();
        EditorGUILayout.Space(8);
        DrawActionButtons();
        EditorGUILayout.Space(8);
        DrawLogSection();
    }

    // ── 공통 설정 (캐릭터명, 감정, 상태) ──────────────────
    private void DrawCommonSection()
    {
        EditorGUILayout.LabelField("Common Settings", EditorStyles.boldLabel);

        _characterName = EditorGUILayout.TextField("Character", _characterName);
        _expression    = EditorGUILayout.TextField("Expression", _expression);

        using (new EditorGUILayout.HorizontalScope())
        {
            _clipState = EditorGUILayout.TextField("State", _clipState);

            if (GUILayout.Button("Intro", GUILayout.Width(55)))
            { _clipState = "Intro"; _isLoop = false; }
            if (GUILayout.Button("Loop", GUILayout.Width(55)))
            { _clipState = "Loop"; _isLoop = true; }
            if (GUILayout.Button("Dialogue", GUILayout.Width(65)))
            { _clipState = "Dialogue"; _isLoop = false; }
        }

        _frameRate = EditorGUILayout.IntField("Frame Rate", Mathf.Max(1, _frameRate));
        _isLoop    = EditorGUILayout.Toggle("Loop Clip", _isLoop);

        // Addressable 그룹
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings != null)
        {
            var groupNames = settings.groups
                .Where(g => g != null)
                .Select(g => g.Name)
                .ToArray();

            int idx = System.Array.IndexOf(groupNames, _addressableGroup);
            if (idx < 0) idx = 0;
            idx = EditorGUILayout.Popup("Addressable Group", idx, groupNames);
            if (idx >= 0 && idx < groupNames.Length)
                _addressableGroup = groupNames[idx];
        }
    }

    // ── 파츠 리스트 ───────────────────────────────────────
    private void DrawPartsSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Parts", EditorStyles.boldLabel);

            if (GUILayout.Button("Reset Default", GUILayout.Width(100)))
                ResetToDefaultParts();

            if (GUILayout.Button("+ Add", GUILayout.Width(60)))
                _entries.Add(new PartEntry());
        }

        EditorGUILayout.Space(2);

        float listHeight = Mathf.Min(_entries.Count * 52 + 10, 320);
        using var scroll = new EditorGUILayout.ScrollViewScope(_entryScroll, GUILayout.Height(listHeight));
        _entryScroll = scroll.scrollPosition;

        int removeIndex = -1;

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    entry.enabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(16));

                    GUI.enabled = entry.enabled;
                    entry.partName = EditorGUILayout.TextField(entry.partName, GUILayout.Width(90));

                    entry.texture = (Texture2D)EditorGUILayout.ObjectField(
                        entry.texture, typeof(Texture2D), false, GUILayout.Width(160));

                    EditorGUILayout.LabelField("C", GUILayout.Width(12));
                    entry.columns = EditorGUILayout.IntField(entry.columns, GUILayout.Width(30));
                    EditorGUILayout.LabelField("R", GUILayout.Width(12));
                    entry.rows = EditorGUILayout.IntField(entry.rows, GUILayout.Width(30));

                    GUI.enabled = true;

                    if (GUILayout.Button("×", GUILayout.Width(22)))
                        removeIndex = i;
                }

                if (entry.enabled && entry.texture != null)
                {
                    int total = entry.columns * entry.rows;
                    int cellW = entry.texture.width / Mathf.Max(1, entry.columns);
                    int cellH = entry.texture.height / Mathf.Max(1, entry.rows);
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(
                        $"{cellW}×{cellH}px  |  {total}f  |  {total / (float)_frameRate:F2}s",
                        EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }
        }

        if (removeIndex >= 0)
            _entries.RemoveAt(removeIndex);
    }

    // ── 주소 미리보기 ─────────────────────────────────────
    private void DrawAddressPreview()
    {
        EditorGUILayout.LabelField("Address Preview", EditorStyles.boldLabel);

        using var box = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

        foreach (var entry in _entries)
        {
            if (!entry.enabled || string.IsNullOrEmpty(entry.partName)) continue;

            string addr = entry.GetAddress(_characterName, _expression, _clipState);
            string status = entry.texture != null ? "✓" : "✕ no texture";

            EditorGUILayout.SelectableLabel(
                $"{status}  {addr}",
                EditorStyles.miniLabel,
                GUILayout.Height(16));
        }
    }

    // ── 실행 버튼 ─────────────────────────────────────────
    private void DrawActionButtons()
    {
        int activeCount = _entries.Count(e => e.enabled && e.texture != null && !string.IsNullOrEmpty(e.partName));

        using var horizontal = new EditorGUILayout.HorizontalScope();

        GUI.enabled = activeCount > 0;

        if (GUILayout.Button("1. Slice All", GUILayout.Height(32)))
            BatchSlice();

        if (GUILayout.Button("2. Create Clips", GUILayout.Height(32)))
            BatchCreateClips();

        GUI.backgroundColor = new Color(0.6f, 1f, 0.7f);
        if (GUILayout.Button($"▶ All In One ({activeCount})", GUILayout.Height(32)))
        {
            BatchSlice();
            AssetDatabase.Refresh();
            BatchCreateClips();
        }
        GUI.backgroundColor = Color.white;

        GUI.enabled = true;
    }

    // ── 로그 ──────────────────────────────────────────────
    private void DrawLogSection()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Log", EditorStyles.boldLabel);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                _logs.Clear();
        }

        float logHeight = Mathf.Max(60, position.height - 620);
        using var scroll = new EditorGUILayout.ScrollViewScope(_logScroll, GUILayout.Height(logHeight));
        _logScroll = scroll.scrollPosition;

        foreach (var log in _logs)
        {
            GUIStyle style = EditorStyles.miniLabel;
            if (log.Contains("✓")) style = _successStyle;
            else if (log.Contains("⚠")) style = _warningStyle;
            else if (log.Contains("✕")) style = _errorStyle;
            EditorGUILayout.LabelField(log, style);
        }
    }

    // ═══════════════════════════════════════════════════════
    // 배치 처리
    // ═══════════════════════════════════════════════════════

    private void BatchSlice()
    {
        AddLog($"── Slice 시작: {_characterName} / {_expression} / {_clipState} ──");

        foreach (var entry in _entries)
        {
            if (!entry.enabled || entry.texture == null || string.IsNullOrEmpty(entry.partName))
                continue;

            SliceSpriteSheet(entry);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddLog("── Slice 완료 ──");
    }

    private void BatchCreateClips()
    {
        AddLog($"── Clip 생성 시작 ──");

        int successCount = 0;
        int failCount = 0;

        foreach (var entry in _entries)
        {
            if (!entry.enabled || entry.texture == null || string.IsNullOrEmpty(entry.partName))
                continue;

            if (CreateAnimationClip(entry))
                successCount++;
            else
                failCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddLog($"── 완료: 성공 {successCount} / 실패 {failCount} ──");
    }

    // ═══════════════════════════════════════════════════════
    // 개별 처리
    // ═══════════════════════════════════════════════════════

    private void SliceSpriteSheet(PartEntry entry)
    {
        string path = AssetDatabase.GetAssetPath(entry.texture);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AddLog($"  ✕ [{entry.partName}] TextureImporter 없음");
            return;
        }

        importer.textureType        = TextureImporterType.Sprite;
        importer.spriteImportMode   = SpriteImportMode.Multiple;
        importer.isReadable         = true;
        importer.filterMode         = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        int cellW = entry.texture.width  / Mathf.Max(1, entry.columns);
        int cellH = entry.texture.height / Mathf.Max(1, entry.rows);
        int total = entry.rows * entry.columns;

        var metaData = new SpriteMetaData[total];
        int idx = 0;

        for (int row = 0; row < entry.rows; row++)
        {
            for (int col = 0; col < entry.columns; col++)
            {
                metaData[idx] = new SpriteMetaData
                {
                    name      = $"{entry.texture.name}_{idx:D3}",
                    rect      = new Rect(
                        col * cellW,
                        (entry.rows - 1 - row) * cellH,
                        cellW,
                        cellH),
                    alignment = (int)SpriteAlignment.Center,
                    pivot     = new Vector2(0.5f, 0.5f)
                };
                idx++;
            }
        }

        importer.spritesheet = metaData;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();

        AddLog($"  ✓ [{entry.partName}] {total}프레임 ({cellW}×{cellH}px)");
    }

    private bool CreateAnimationClip(PartEntry entry)
    {
        Sprite[] sprites = LoadSpritesFromTexture(entry.texture);
        if (sprites == null || sprites.Length == 0)
        {
            AddLog($"  ✕ [{entry.partName}] 스프라이트 없음 — Slice 먼저 실행");
            return false;
        }

        // 출력 경로 = 텍스처 위치
        string texPath = AssetDatabase.GetAssetPath(entry.texture);
        string outputFolder = Path.GetDirectoryName(texPath);

        // 클립 생성
        var clip = new AnimationClip();
        clip.frameRate = _frameRate;

        var keyframes = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keyframes[i] = new ObjectReferenceKeyframe
            {
                time  = i / (float)_frameRate,
                value = sprites[i]
            };
        }

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = _isLoop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        // 저장
        string clipName = entry.GetClipName(_characterName, _expression, _clipState);
        string clipPath = $"{outputFolder}/{clipName}.anim";

        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            clip = existing;
            AddLog($"  [{entry.partName}] 기존 클립 덮어쓰기");
        }
        else
        {
            AssetDatabase.CreateAsset(clip, clipPath);
            AddLog($"  [{entry.partName}] 새 클립 생성: {clipPath}");
        }

        // Addressable 등록
        string address = entry.GetAddress(_characterName, _expression, _clipState);
        if (!SetAddressableAddress(clipPath, address))
        {
            AddLog($"  ⚠ [{entry.partName}] Addressable 등록 실패");
            return false;
        }

        AddLog($"  ✓ [{entry.partName}] → {address}");
        return true;
    }

    // ═══════════════════════════════════════════════════════
    // 유틸
    // ═══════════════════════════════════════════════════════

    private Sprite[] LoadSpritesFromTexture(Texture2D texture)
    {
        if (texture == null) return null;

        string path = AssetDatabase.GetAssetPath(texture);
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .OrderBy(s => s.name)
            .ToArray();
    }

    private bool SetAddressableAddress(string assetPath, string address)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return false;

        var group = settings.groups.FirstOrDefault(g => g != null && g.Name == _addressableGroup);
        if (group == null)
        {
            AddLog($"  ✕ 그룹 '{_addressableGroup}' 없음");
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(assetPath);
        if (string.IsNullOrEmpty(guid)) return false;

        var entry = settings.FindAssetEntry(guid);
        if (entry != null)
        {
            entry.address = address;
        }
        else
        {
            entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
        return true;
    }

    private void ResetToDefaultParts()
    {
        _entries.Clear();
        foreach (var name in DEFAULT_PARTS)
            _entries.Add(new PartEntry { partName = name });
    }

    private void AddLog(string message)
    {
        _logs.Add($"{System.DateTime.Now:HH:mm:ss}  {message}");
        Repaint();
    }

    private void InitStyles()
    {
        if (_stylesInit) return;
        _stylesInit = true;

        _headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            normal = { textColor = new Color(0.9f, 0.85f, 1f) }
        };

        _successStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.4f, 0.9f, 0.5f) } };
        _warningStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.85f, 0.3f) } };
        _errorStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(1f, 0.4f, 0.4f) } };
    }
}
