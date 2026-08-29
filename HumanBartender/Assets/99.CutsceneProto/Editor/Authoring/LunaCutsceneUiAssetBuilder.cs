using ProjectLuna.CutscenePrototype.Authoring;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Editor.Authoring
{
    public static class LunaCutsceneUiAssetBuilder
    {
        public const string UiFolder = LunaCutsceneAuthoringBuilder.Root + "/Authoring/UI";
        public const string SpeechBubblePrefabPath = UiFolder + "/CutsceneSpeechBubble.prefab";
        public const string SpeechBubbleStylePath = UiFolder + "/CutsceneSpeechBubbleStyle.asset";
        public const string PresentationPresetPath = UiFolder + "/CinematicPresentationPreset.asset";
        private const string ProjectFontPath = "Assets/06.Fonts/NeoDunggeunmo SDF.asset";

        public readonly struct UiAssets
        {
            public readonly LunaCutsceneSpeechBubbleView prefab;
            public readonly LunaCutsceneSpeechBubbleStyle style;
            public readonly LunaCutscenePresentationPreset presentationPreset;

            public UiAssets(
                LunaCutsceneSpeechBubbleView prefab,
                LunaCutsceneSpeechBubbleStyle style,
                LunaCutscenePresentationPreset presentationPreset)
            {
                this.prefab = prefab;
                this.style = style;
                this.presentationPreset = presentationPreset;
            }
        }

        [MenuItem("Project L.U.N.A/Cutscene Authoring/UI Prefab and Style/Create or Repair")]
        public static void RepairAssetsAndScene()
        {
            UiAssets assets = EnsureAssets(true);
            LunaCutsceneDirector runtime = Object.FindFirstObjectByType<LunaCutsceneDirector>();
            if (runtime != null)
                UpgradeSceneUi(runtime, assets);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LunaCutsceneAuthoring] TMP 말풍선 Prefab·Style·현재 Scene UI 생성/복구 완료.");
        }

        public static UiAssets EnsureAssets(bool rebuildPrefab = false)
        {
            EnsureFolder(LunaCutsceneAuthoringBuilder.Root, "Authoring");
            EnsureFolder(LunaCutsceneAuthoringBuilder.Root + "/Authoring", "UI");

            LunaCutsceneSpeechBubbleStyle style =
                AssetDatabase.LoadAssetAtPath<LunaCutsceneSpeechBubbleStyle>(SpeechBubbleStylePath);
            if (style == null)
            {
                style = ScriptableObject.CreateInstance<LunaCutsceneSpeechBubbleStyle>();
                style.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProjectFontPath)
                             ?? TMP_Settings.defaultFontAsset;
                AssetDatabase.CreateAsset(style, SpeechBubbleStylePath);
            }
            else if (style.font == null)
            {
                style.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProjectFontPath)
                             ?? TMP_Settings.defaultFontAsset;
                EditorUtility.SetDirty(style);
            }

            LunaCutsceneSpeechBubbleView prefab =
                AssetDatabase.LoadAssetAtPath<LunaCutsceneSpeechBubbleView>(SpeechBubblePrefabPath);
            if (prefab == null || rebuildPrefab)
                prefab = BuildPrefab(style);

            LunaCutscenePresentationPreset presentationPreset = EnsurePresentationPreset();

            AssetDatabase.SaveAssets();
            return new UiAssets(prefab, style, presentationPreset);
        }

        public static LunaCutscenePresentationPreset EnsurePresentationPreset()
        {
            LunaCutscenePresentationPreset preset =
                AssetDatabase.LoadAssetAtPath<LunaCutscenePresentationPreset>(PresentationPresetPath);
            if (preset != null)
                return preset;

            preset = ScriptableObject.CreateInstance<LunaCutscenePresentationPreset>();
            AssetDatabase.CreateAsset(preset, PresentationPresetPath);
            EditorUtility.SetDirty(preset);
            return preset;
        }

        public static void UpgradeSceneUi(LunaCutsceneDirector runtime)
        {
            UpgradeSceneUi(runtime, EnsureAssets());
        }

        public static void UpgradeSceneUi(LunaCutsceneDirector runtime, UiAssets assets)
        {
            if (runtime == null)
                return;
            LunaCutsceneDialogueUI ui = runtime.DialogueUI;
            if (ui == null)
                ui = runtime.GetComponentInChildren<LunaCutsceneDialogueUI>(true);
            if (ui == null)
                return;

            Canvas canvas = ui.GetComponent<Canvas>();
            if (canvas == null)
                return;
            RectTransform layer = EnsureBubbleLayer(canvas.transform);
            RemoveLegacyDialoguePanel(canvas.transform);
            TMP_Text status = EnsureStatus(canvas.transform, assets.style);
            Image fade = FindImage(canvas.transform, "Fade");
            Image flash = FindImage(canvas.transform, "Flash");
            RectTransform letterboxTop = EnsureLetterboxBar(canvas.transform, "LetterboxTop", true);
            RectTransform letterboxBottom = EnsureLetterboxBar(canvas.transform, "LetterboxBottom", false);

            Undo.RecordObject(ui, "Upgrade L.U.N.A Cutscene TMP UI");
            ui.Configure(layer, assets.prefab, assets.style, fade, flash, status);
            layer.SetAsLastSibling();
            ui.ConfigurePresentation(letterboxTop, letterboxBottom);
            if (runtime.Definition != null && runtime.Definition.presentationPreset == null)
            {
                Undo.RecordObject(runtime.Definition, "Assign L.U.N.A Cutscene Presentation Preset");
                runtime.Definition.presentationPreset = assets.presentationPreset;
                EditorUtility.SetDirty(runtime.Definition);
            }
            EditorUtility.SetDirty(ui);
            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
        }

        public static RectTransform EnsureBubbleLayer(Transform canvasTransform)
        {
            Transform existing = canvasTransform.Find("SpeechBubbleLayer");
            RectTransform rect;
            if (existing != null)
            {
                rect = existing as RectTransform;
                if (rect == null)
                    rect = existing.gameObject.AddComponent<RectTransform>();
            }
            else
            {
                GameObject layer = new("SpeechBubbleLayer", typeof(RectTransform));
                if (!Application.isBatchMode)
                    Undo.RegisterCreatedObjectUndo(layer, "Create L.U.N.A Speech Bubble Layer");
                layer.transform.SetParent(canvasTransform, false);
                rect = layer.GetComponent<RectTransform>();
            }
            Stretch(rect);
            return rect;
        }

        public static TMP_Text EnsureStatus(Transform canvasTransform, LunaCutsceneSpeechBubbleStyle style)
        {
            Transform statusTransform = canvasTransform.Find("Status");
            GameObject target;
            if (statusTransform == null)
            {
                target = new GameObject("Status", typeof(RectTransform), typeof(CanvasRenderer));
                target.transform.SetParent(canvasTransform, false);
            }
            else
            {
                target = statusTransform.gameObject;
            }

            UnityEngine.UI.Text legacy = target.GetComponent<UnityEngine.UI.Text>();
            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy);
            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>()
                                      ?? Undo.AddComponent<TextMeshProUGUI>(target);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.018f, 0.84f);
            rect.anchorMax = new Vector2(0.42f, 0.98f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            text.font = style != null && style.font != null ? style.font : TMP_Settings.defaultFontAsset;
            text.fontSize = 16f;
            text.color = new Color(0.55f, 0.95f, 0.92f);
            text.alignment = TextAlignmentOptions.TopLeft;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        public static RectTransform EnsureLetterboxBar(Transform canvasTransform, string name, bool top)
        {
            Transform existing = canvasTransform.Find(name);
            GameObject target;
            if (existing == null)
            {
                target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                if (!Application.isBatchMode)
                    Undo.RegisterCreatedObjectUndo(target, "Create L.U.N.A Letterbox Bar");
                target.transform.SetParent(canvasTransform, false);
            }
            else
            {
                target = existing.gameObject;
            }

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = top ? new Vector2(0f, 0.88f) : Vector2.zero;
            rect.anchorMax = top ? Vector2.one : new Vector2(1f, 0.12f);
            rect.pivot = top ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
            Image image = target.GetComponent<Image>() ?? Undo.AddComponent<Image>(target);
            image.color = Color.black;
            image.raycastTarget = false;
            target.SetActive(false);
            return rect;
        }

        private static LunaCutsceneSpeechBubbleView BuildPrefab(LunaCutsceneSpeechBubbleStyle style)
        {
            GameObject root = new(
                "CutsceneSpeechBubble",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LunaCutsceneSpeechBubbleView));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = new Vector2(style.minWidth, style.minHeight);
            Image background = root.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;
            background.color = style.backgroundColor;
            background.raycastTarget = false;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;

            Image tail = CreateTail(root.transform, style);
            TextMeshProUGUI speaker = CreateText(root.transform, "Speaker", style.font, style.speakerFontSize);
            TextMeshProUGUI body = CreateText(root.transform, "Body", style.font, style.bodyFontSize);
            TextMeshProUGUI hint = CreateText(root.transform, "ContinueHint", style.font, style.hintFontSize);

            LunaCutsceneSpeechBubbleView view = root.GetComponent<LunaCutsceneSpeechBubbleView>();
            view.Configure(rootRect, background, speaker, body, hint, tail.rectTransform, tail, group);
            GameObject prefabObject = PrefabUtility.SaveAsPrefabAsset(root, SpeechBubblePrefabPath);
            Object.DestroyImmediate(root);
            if (prefabObject == null)
                throw new System.InvalidOperationException($"말풍선 Prefab 생성 실패: {SpeechBubblePrefabPath}");
            return prefabObject.GetComponent<LunaCutsceneSpeechBubbleView>();
        }

        private static Image CreateTail(Transform parent, LunaCutsceneSpeechBubbleStyle style)
        {
            GameObject target = new("Tail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = style.tailSize;
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image image = target.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Simple;
            image.color = style.tailColor;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            TMP_FontAsset font,
            float fontSize)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            target.transform.SetParent(parent, false);
            Stretch(target.GetComponent<RectTransform>());
            TextMeshProUGUI text = target.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Image FindImage(Transform root, string name)
        {
            Transform target = root.Find(name);
            return target != null ? target.GetComponent<Image>() : null;
        }

        private static void RemoveLegacyDialoguePanel(Transform canvasTransform)
        {
            Transform legacy = canvasTransform.Find("DialoguePanel");
            if (legacy != null)
                Undo.DestroyObjectImmediate(legacy.gameObject);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
