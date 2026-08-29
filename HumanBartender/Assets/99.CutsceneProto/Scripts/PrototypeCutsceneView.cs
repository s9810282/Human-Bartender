using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype
{
    public sealed class PrototypeCutsceneView : MonoBehaviour
    {
        private const float PixelCompositionScale = 0.25f;
        private const float PixelOrthographicSize = 1.35f;

        private readonly Dictionary<string, GameObject> registry = new(StringComparer.Ordinal);

        private Camera sceneCamera;
        private Transform cameraRig;
        private Transform luna;
        private Transform yuna;
        private Transform intruder;
        private Transform labDoor;
        private Transform warningLightLeft;
        private Transform warningLightRight;

        private Canvas canvas;
        private Image fadeOverlay;
        private Image flashOverlay;
        private GameObject dialoguePanel;
        private Text speakerText;
        private Text bodyText;
        private Text hintText;
        private Text statusText;
        private Text headerText;
        private GameObject choicePanel;
        private Text choicePromptText;
        private Button[] choiceButtons;
        private Text[] choiceButtonTexts;
        private GameObject debugPanel;
        private Text debugText;

        private Sprite whiteSprite;
        private Vector3 cameraHome;
        private Coroutine overlayRoutine;
        private Coroutine shakeRoutine;

        public bool IsDialogueVisible => dialoguePanel != null && dialoguePanel.activeSelf;
        public event Action<int> ChoiceSelected;

        public void Configure(Camera value)
        {
            sceneCamera = value;
        }

        private void Awake()
        {
            if (sceneCamera == null)
                sceneCamera = Camera.main;

            BuildPrototypeStage();
            BuildPrototypeUi();
            ResetStage();
        }

        public void ResetStage()
        {
            if (cameraRig != null)
            {
                cameraRig.localPosition = Vector3.zero;
                cameraRig.localRotation = Quaternion.identity;
            }

            if (sceneCamera != null)
            {
                sceneCamera.orthographic = true;
                sceneCamera.orthographicSize = PixelOrthographicSize;
                sceneCamera.transform.localPosition = cameraHome;
            }

            SetPosition(luna, new Vector3(-5.2f, -1.35f, 0f));
            SetPosition(yuna, new Vector3(-1.7f, -1.35f, 0f));
            SetPosition(intruder, new Vector3(6.2f, -1.35f, 0f));
            SetPosition(labDoor, new Vector3(6.55f, 0.1f, 0f));

            if (luna != null) luna.gameObject.SetActive(true);
            if (yuna != null) yuna.gameObject.SetActive(true);
            if (intruder != null) intruder.gameObject.SetActive(false);

            SetWarningLights(new Color(0.08f, 0.75f, 0.9f, 0.8f));
            SetOverlayAlpha(fadeOverlay, 1f);
            SetOverlayAlpha(flashOverlay, 0f);
            HideDialogue();
            HideChoice();
            SetStatus("IDLE", "재생 준비");
        }

        public void BeginTimelineSegment(PrototypeTimelineSegment segment)
        {
            switch (segment)
            {
                case PrototypeTimelineSegment.LabEntry:
                    intruder.gameObject.SetActive(false);
                    SetStatus("TIMELINE", "LAB ENTRY");
                    break;
                case PrototypeTimelineSegment.LabAttack:
                    intruder.gameObject.SetActive(true);
                    SetWarningLights(new Color(1f, 0.12f, 0.16f, 0.95f));
                    SetStatus("TIMELINE", "LAB ATTACK");
                    break;
                case PrototypeTimelineSegment.LabEscape:
                    intruder.gameObject.SetActive(true);
                    SetStatus("TIMELINE", "LAB ESCAPE");
                    break;
            }
        }

        public void ApplyTimelinePose(PrototypeTimelineSegment segment, float time)
        {
            float smooth = time * time * (3f - 2f * time);
            switch (segment)
            {
                case PrototypeTimelineSegment.LabEntry:
                    luna.localPosition = Vector3.Lerp(
                        new Vector3(-5.2f, -1.35f, 0f),
                        new Vector3(-2.8f, -1.35f, 0f), smooth);
                    cameraRig.localPosition = Vector3.Lerp(Vector3.zero, CameraOffset(-0.45f, 0.15f), smooth);
                    break;

                case PrototypeTimelineSegment.LabAttack:
                    intruder.localPosition = Vector3.Lerp(
                        new Vector3(6.2f, -1.35f, 0f),
                        new Vector3(2.85f, -1.35f, 0f), smooth);
                    yuna.localPosition = Vector3.Lerp(
                        new Vector3(-1.7f, -1.35f, 0f),
                        new Vector3(-1.2f, -1.35f, 0f), Mathf.Sin(smooth * Mathf.PI) * 0.22f + smooth);
                    cameraRig.localPosition = Vector3.Lerp(
                        CameraOffset(-0.45f, 0.15f),
                        CameraOffset(0.45f, 0.05f), smooth);
                    PulseWarningLights(time);
                    break;

                case PrototypeTimelineSegment.LabEscape:
                    luna.localPosition = Vector3.Lerp(
                        new Vector3(-2.8f, -1.35f, 0f),
                        new Vector3(5.2f, -1.35f, 0f), smooth);
                    yuna.localPosition = Vector3.Lerp(
                        new Vector3(-1.2f, -1.35f, 0f),
                        new Vector3(5.75f, -1.35f, 0f), smooth);
                    intruder.localPosition = Vector3.Lerp(
                        new Vector3(2.85f, -1.35f, 0f),
                        new Vector3(4.15f, -1.35f, 0f), smooth);
                    labDoor.localPosition = Vector3.Lerp(
                        new Vector3(6.55f, 0.1f, 0f),
                        new Vector3(6.55f, 3.25f, 0f), Mathf.Clamp01(time * 2.2f));
                    cameraRig.localPosition = Vector3.Lerp(
                        CameraOffset(0.45f, 0.05f),
                        CameraOffset(1.2f, 0.1f), smooth);
                    PulseWarningLights(time);
                    break;
            }
        }

        public void SetStatus(string state, string detail)
        {
            if (statusText != null)
                statusText.text = $"STATE  {state}\n{detail}";
        }

        public void SetHeader(string title, string locale)
        {
            if (headerText != null)
                headerText.text = $"PROJECT L.U.N.A  /  CUTSCENE PROTOTYPE     {title}     [{locale.ToUpperInvariant()}]";
        }

        public void ShowDialogue(string speaker, string body)
        {
            dialoguePanel.SetActive(true);
            speakerText.text = speaker;
            bodyText.text = body;
            hintText.text = "SPACE / CLICK  ▶";
        }

        public void SetDialogueBody(string body)
        {
            if (bodyText != null)
                bodyText.text = body;
        }

        public void HideDialogue()
        {
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        public void ShowChoice(string prompt, string[] labels, bool[] enabled, string[] lockReasons)
        {
            if (choicePanel == null)
                return;

            choicePanel.SetActive(true);
            choicePromptText.text = prompt;
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                bool used = labels != null && index < labels.Length;
                choiceButtons[index].gameObject.SetActive(used);
                if (!used)
                    continue;

                bool canSelect = enabled != null && index < enabled.Length && enabled[index];
                choiceButtons[index].interactable = canSelect;
                string lockText = !canSelect && lockReasons != null && index < lockReasons.Length
                    ? lockReasons[index]
                    : string.Empty;
                choiceButtonTexts[index].text = canSelect
                    ? $"{index + 1}.  {labels[index]}"
                    : $"{index + 1}.  {labels[index]}     [{lockText}]";
                choiceButtonTexts[index].color = canSelect
                    ? Color.white
                    : new Color(0.42f, 0.5f, 0.54f, 1f);
            }
        }

        public void HideChoice()
        {
            if (choicePanel != null)
                choicePanel.SetActive(false);
        }

        public void SetDebugVisible(bool visible)
        {
            if (debugPanel != null)
                debugPanel.SetActive(visible);
        }

        public void SetDebugBody(string body)
        {
            if (debugText != null)
                debugText.text = body;
        }

        public bool HasTarget(string id)
        {
            return !string.IsNullOrWhiteSpace(id) && registry.ContainsKey(id);
        }

        public bool TrySetPosition(string id, Vector3 position)
        {
            if (!registry.TryGetValue(id, out GameObject target))
                return false;
            target.transform.localPosition = position;
            return true;
        }

        public void SetCameraState(float x, float y, float size)
        {
            if (cameraRig != null)
            {
                cameraRig.localPosition = new Vector3(x, y, 0f);
                cameraRig.localRotation = Quaternion.identity;
            }
            if (sceneCamera != null && size > 0f)
                sceneCamera.orthographicSize = size;
        }

        public void SetFadeState(string state)
        {
            bool black = string.Equals(state, "black", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(state, "out", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(state, "visible", StringComparison.OrdinalIgnoreCase);
            SetOverlayAlpha(fadeOverlay, black ? 1f : 0f);
        }

        public void ClearTransientEffects()
        {
            if (overlayRoutine != null)
            {
                StopCoroutine(overlayRoutine);
                overlayRoutine = null;
            }
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeRoutine = null;
            }
            SetOverlayAlpha(flashOverlay, 0f);
            if (cameraRig != null)
                cameraRig.localRotation = Quaternion.identity;
        }

        public IEnumerator Fade(bool fadeIn, float duration)
        {
            if (overlayRoutine != null)
                StopCoroutine(overlayRoutine);

            float from = fadeOverlay.color.a;
            float to = fadeIn ? 0f : 1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOverlayAlpha(fadeOverlay, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
                yield return null;
            }
            SetOverlayAlpha(fadeOverlay, to);
        }

        public IEnumerator Flash(float duration)
        {
            float half = Mathf.Max(0.03f, duration * 0.5f);
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOverlayAlpha(flashOverlay, Mathf.Lerp(0f, 0.88f, elapsed / half));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOverlayAlpha(flashOverlay, Mathf.Lerp(0.88f, 0f, elapsed / half));
                yield return null;
            }
            SetOverlayAlpha(flashOverlay, 0f);
        }

        public IEnumerator Shake(float duration, float strength)
        {
            Vector3 origin = cameraRig.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damping = 1f - Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.01f));
                cameraRig.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * strength * PixelCompositionScale * damping);
                yield return null;
            }
            cameraRig.localPosition = origin;
        }

        public bool TrySetActive(string id, bool active)
        {
            if (!registry.TryGetValue(id, out GameObject target))
                return false;
            target.SetActive(active);
            return true;
        }

        public void SetWarningLights(Color color)
        {
            SetSpriteColor(warningLightLeft, color);
            SetSpriteColor(warningLightRight, color);
        }

        private void PulseWarningLights(float time)
        {
            float pulse = 0.45f + Mathf.PingPong(time * 3f, 0.5f);
            SetWarningLights(new Color(1f, 0.05f, 0.09f, pulse));
        }

        private void BuildPrototypeStage()
        {
            whiteSprite = CreateWhiteSprite();

            cameraRig = new GameObject("CameraRig").transform;
            cameraRig.SetParent(transform, false);
            sceneCamera.transform.SetParent(cameraRig, false);
            cameraHome = new Vector3(0f, 0f, -10f);
            sceneCamera.transform.localPosition = cameraHome;
            sceneCamera.backgroundColor = new Color(0.008f, 0.017f, 0.027f, 1f);
            sceneCamera.orthographic = true;
            sceneCamera.orthographicSize = PixelOrthographicSize;
            registry["test_camera_rig"] = cameraRig.gameObject;

            Transform stage = new GameObject("GeneratedLabStage").transform;
            stage.SetParent(transform, false);
            stage.localScale = Vector3.one * PixelCompositionScale;

            CreateRect(stage, "Background", new Vector3(0f, 0f, 2f), new Vector2(20f, 12f), new Color(0.018f, 0.035f, 0.055f), -20);
            CreateRect(stage, "Floor", new Vector3(0f, -3.35f, 1f), new Vector2(20f, 3.2f), new Color(0.035f, 0.075f, 0.09f), -10);
            CreateRect(stage, "FloorLine", new Vector3(0f, -1.8f, 0f), new Vector2(20f, 0.08f), new Color(0.1f, 0.75f, 0.78f, 0.65f), -8);

            for (int x = -7; x <= 7; x += 2)
                CreateRect(stage, $"WallGrid_{x}", new Vector3(x, 0.2f, 1f), new Vector2(0.025f, 7f), new Color(0.08f, 0.25f, 0.29f, 0.45f), -16);
            for (int y = -1; y <= 3; y += 2)
                CreateRect(stage, $"WallGridH_{y}", new Vector3(0f, y, 1f), new Vector2(16f, 0.025f), new Color(0.08f, 0.25f, 0.29f, 0.45f), -16);

            for (int i = 0; i < 3; i++)
            {
                float x = -4.8f + i * 2.25f;
                Transform tank = CreateRect(stage, $"Containment_{i}", new Vector3(x, 0.15f, 0f), new Vector2(1.2f, 3.45f), new Color(0.03f, 0.18f, 0.22f, 0.82f), -6);
                CreateRect(tank, "Glass", new Vector3(0f, 0f, -0.1f), new Vector2(0.94f, 2.75f), new Color(0.06f, 0.65f, 0.72f, 0.15f), -5);
                CreateRect(tank, "Top", new Vector3(0f, 1.62f, -0.2f), new Vector2(1.35f, 0.18f), new Color(0.18f, 0.42f, 0.48f), -4);
                CreateRect(tank, "Bottom", new Vector3(0f, -1.62f, -0.2f), new Vector2(1.35f, 0.18f), new Color(0.18f, 0.42f, 0.48f), -4);
            }

            labDoor = CreateRect(stage, "test_lab_exit", new Vector3(6.55f, 0.1f, 0f), new Vector2(2.25f, 4.2f), new Color(0.16f, 0.22f, 0.27f), -7);
            CreateRect(labDoor, "DoorStripe", new Vector3(-0.78f, 0f, -0.1f), new Vector2(0.13f, 3.7f), new Color(1f, 0.46f, 0.08f), -5);
            registry["test_lab_exit"] = labDoor.gameObject;

            warningLightLeft = CreateRect(stage, "WarningLightLeft", new Vector3(-6.9f, 3.85f, 0f), new Vector2(1.15f, 0.16f), Color.cyan, -4);
            warningLightRight = CreateRect(stage, "WarningLightRight", new Vector3(6.9f, 3.85f, 0f), new Vector2(1.15f, 0.16f), Color.cyan, -4);

            luna = CreateActor(stage, "test_luna", new Color(0.78f, 0.92f, 1f), new Color(0.17f, 0.78f, 0.92f), false);
            yuna = CreateActor(stage, "test_yuna", new Color(1f, 0.86f, 0.77f), new Color(0.93f, 0.28f, 0.53f), true);
            intruder = CreateActor(stage, "test_intruder", new Color(0.67f, 0.69f, 0.72f), new Color(0.75f, 0.06f, 0.1f), false);

            registry["test_luna"] = luna.gameObject;
            registry["test_yuna"] = yuna.gameObject;
            registry["test_intruder"] = intruder.gameObject;
        }

        private void BuildPrototypeUi()
        {
            if (EventSystem.current == null)
            {
                GameObject eventSystemObject = new("PrototypeEventSystem", typeof(EventSystem));
                eventSystemObject.transform.SetParent(transform, false);
                InputSystemUIInputModule inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }

            GameObject canvasObject = new("PrototypeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = sceneCamera;
            canvas.planeDistance = 0.5f;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;

            headerText = CreateText(canvas.transform, "Header", new Vector2(24f, -18f), new Vector2(820f, 44f), 15, TextAnchor.UpperLeft, new Color(0.55f, 1f, 0.96f));
            statusText = CreateText(canvas.transform, "Status", new Vector2(-24f, -18f), new Vector2(320f, 72f), 13, TextAnchor.UpperRight, new Color(0.72f, 0.85f, 0.9f));
            SetTopRight(statusText.rectTransform);

            dialoguePanel = CreateBottomPanel(canvas.transform, "DialoguePanel", 70f, 70f, 48f, 174f, new Color(0.012f, 0.035f, 0.052f, 0.96f));
            RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();

            speakerText = CreateText(panelRect, "Speaker", new Vector2(30f, -18f), new Vector2(300f, 34f), 19, TextAnchor.UpperLeft, new Color(0.42f, 1f, 0.9f));
            bodyText = CreateText(panelRect, "Body", new Vector2(30f, -58f), new Vector2(-60f, 72f), 25, TextAnchor.UpperLeft, Color.white);
            SetHorizontalStretch(bodyText.rectTransform, 30f, 30f, 58f, 18f);
            hintText = CreateText(panelRect, "Hint", new Vector2(-220f, -112f), new Vector2(195f, 28f), 14, TextAnchor.UpperRight, new Color(0.55f, 1f, 0.96f));
            SetTopRight(hintText.rectTransform);

            Text help = CreateText(canvas.transform, "Help", new Vector2(24f, 190f), new Vector2(850f, 32f), 14, TextAnchor.LowerLeft, new Color(0.6f, 0.72f, 0.78f));
            SetBottomLeft(help.rectTransform);
            help.text = "SPACE / CLICK  대사 진행     1–4  선택     S  스킵     R  초기화     L  언어     F1  디버그";

            choicePanel = CreateBottomPanel(canvas.transform, "ChoicePanel", 180f, 180f, 120f, 600f, new Color(0.008f, 0.027f, 0.043f, 0.98f));
            RectTransform choiceRect = choicePanel.GetComponent<RectTransform>();
            choicePromptText = CreateText(choiceRect, "ChoicePrompt", new Vector2(30f, -24f), new Vector2(-60f, 52f), 22, TextAnchor.UpperLeft, new Color(0.42f, 1f, 0.9f));
            SetHorizontalStretch(choicePromptText.rectTransform, 30f, 30f, 24f, 390f);
            choiceButtons = new Button[4];
            choiceButtonTexts = new Text[4];
            for (int index = 0; index < choiceButtons.Length; index++)
            {
                int capturedIndex = index;
                choiceButtons[index] = CreateChoiceButton(choiceRect, index, out choiceButtonTexts[index]);
                choiceButtons[index].onClick.AddListener(() => ChoiceSelected?.Invoke(capturedIndex));
            }
            choicePanel.SetActive(false);

            debugPanel = CreateBottomPanel(canvas.transform, "DebugPanel", 930f, 18f, 118f, 610f, new Color(0f, 0.02f, 0.03f, 0.9f));
            debugText = CreateText(debugPanel.transform, "DebugText", new Vector2(16f, -16f), new Vector2(-32f, -32f), 14, TextAnchor.UpperLeft, new Color(0.56f, 1f, 0.82f));
            SetHorizontalStretch(debugText.rectTransform, 16f, 16f, 16f, 16f);
            debugPanel.SetActive(false);

            fadeOverlay = CreateOverlay(canvas.transform, "FadeOverlay", Color.black);
            flashOverlay = CreateOverlay(canvas.transform, "FlashOverlay", Color.white);
            fadeOverlay.transform.SetAsLastSibling();
            flashOverlay.transform.SetAsLastSibling();
            dialoguePanel.transform.SetAsLastSibling();
            headerText.transform.SetAsLastSibling();
            statusText.transform.SetAsLastSibling();
            help.transform.SetAsLastSibling();
            choicePanel.transform.SetAsLastSibling();
            debugPanel.transform.SetAsLastSibling();
        }

        private Transform CreateActor(Transform parent, string id, Color skin, Color accent, bool longHair)
        {
            Transform root = new GameObject(id).transform;
            root.SetParent(parent, false);
            CreateRect(root, "Shadow", new Vector3(0f, -1.28f, 0.5f), new Vector2(1.2f, 0.18f), new Color(0f, 0f, 0f, 0.55f), 2);
            CreateRect(root, "Body", new Vector3(0f, -0.47f, 0f), new Vector2(0.95f, 1.55f), new Color(0.03f, 0.08f, 0.12f), 3);
            CreateRect(root, "CoatAccent", new Vector3(0f, -0.35f, -0.05f), new Vector2(0.18f, 1.25f), accent, 4);
            CreateRect(root, "Head", new Vector3(0f, 0.75f, 0f), new Vector2(0.88f, 0.92f), skin, 4);
            CreateRect(root, "Hair", new Vector3(longHair ? -0.08f : 0f, 1.07f, -0.1f), new Vector2(longHair ? 1.03f : 0.92f, longHair ? 0.52f : 0.35f), accent, 5);
            if (longHair)
                CreateRect(root, "LongHair", new Vector3(-0.43f, 0.45f, -0.05f), new Vector2(0.18f, 1.45f), accent, 3);
            CreateRect(root, "Eye", new Vector3(0.2f, 0.82f, -0.15f), new Vector2(0.12f, 0.07f), new Color(0.02f, 0.05f, 0.07f), 6);
            return root;
        }

        private Transform CreateRect(Transform parent, string name, Vector3 position, Vector2 size, Color color, int sortingOrder)
        {
            GameObject gameObject = new(name, typeof(SpriteRenderer));
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = position;
            gameObject.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = gameObject.GetComponent<SpriteRenderer>();
            renderer.sprite = whiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return gameObject.transform;
        }

        private static Sprite CreateWhiteSprite()
        {
            Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
            {
                name = "RuntimeWhitePixel",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        }

        private static void SetPosition(Transform target, Vector3 position)
        {
            if (target != null)
                target.localPosition = position;
        }

        private static Vector3 CameraOffset(float x, float y)
        {
            return new Vector3(x * PixelCompositionScale, y * PixelCompositionScale, 0f);
        }

        private static void SetSpriteColor(Transform target, Color color)
        {
            if (target != null && target.TryGetComponent(out SpriteRenderer renderer))
                renderer.color = color;
        }

        private static void SetOverlayAlpha(Image image, float alpha)
        {
            if (image == null) return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            image.raycastTarget = alpha > 0.001f;
        }

        private static Font GetBuiltinFont()
        {
            string[] preferredFonts =
            {
                "Apple SD Gothic Neo",
                "Noto Sans CJK KR",
                "Malgun Gothic",
                "Arial Unicode MS",
                "Arial"
            };
            Font dynamicFont = Font.CreateDynamicFontFromOSFont(preferredFonts, 24);
            return dynamicFont != null
                ? dynamicFont
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Text));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = gameObject.GetComponent<Text>();
            text.font = GetBuiltinFont();
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static GameObject CreateBottomPanel(Transform parent, string name, float left, float right, float bottom, float top, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, top);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return gameObject;
        }

        private static Button CreateChoiceButton(Transform parent, int index, out Text label)
        {
            GameObject gameObject = new($"Choice_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            float top = -(96f + index * 72f);
            rect.offsetMin = new Vector2(28f, top - 56f);
            rect.offsetMax = new Vector2(-28f, top);

            Image image = gameObject.GetComponent<Image>();
            image.color = new Color(0.035f, 0.13f, 0.17f, 0.98f);
            Button button = gameObject.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.45f, 1f, 0.92f, 1f);
            colors.pressedColor = new Color(0.2f, 0.85f, 0.78f, 1f);
            colors.disabledColor = new Color(0.35f, 0.38f, 0.4f, 0.8f);
            button.colors = colors;

            label = CreateText(rect, "Label", new Vector2(18f, -13f), new Vector2(-36f, 36f), 18, TextAnchor.UpperLeft, Color.white);
            SetHorizontalStretch(label.rectTransform, 18f, 18f, 12f, 8f);
            return button;
        }

        private static Image CreateOverlay(Transform parent, string name, Color color)
        {
            GameObject gameObject = new(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static void SetTopRight(RectTransform rect)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
        }

        private static void SetBottomLeft(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
        }

        private static void SetHorizontalStretch(RectTransform rect, float left, float right, float top, float bottom)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
