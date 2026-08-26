// 98.Claude 컷씬 프로토타입 — 대사·말풍선·선택지 UI (전부 런타임 생성, TMP·외부 폰트 미사용)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Claude98
{
    public static class C98_Input
    {
        public static bool AdvancePressed =>
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.E) ||
            Input.GetMouseButtonDown(0);
    }

    public class C98_DialogueUI : MonoBehaviour
    {
        const float TypingInterval = 0.04f; // 문서 기준 40ms
        const int MaxBarks = 2;             // 동시 bark 수 — 문서 결정 대기 항목, 프로토타입은 2로 가정

        C98_Bundle bundle;
        C98_BindingRegistry reg;
        Camera cam;
        Font font;
        Canvas canvas;

        // 메인 말풍선
        RectTransform bubbleRoot;
        Text nameText, bodyText, advanceIcon;
        Image bubblePanel;
        C98_Actor trackedActor;
        bool trackWorld;
        bool anchorWarned;

        // 내레이션(화면 하단 고정)
        RectTransform narrationRoot;
        Text narrationText;

        // 선택지
        RectTransform choiceRoot;
        readonly List<GameObject> choiceButtons = new List<GameObject>();
        public int SelectedOption { get; private set; } = -1;

        // 바크
        readonly List<RectTransform> barks = new List<RectTransform>();

        // 안내(타이틀/토스트/오류)
        RectTransform idleRoot;
        Text idleText;
        Text toastText;
        Coroutine toastCo;

        public static C98_DialogueUI Create(Transform parent, C98_Bundle bundle, C98_BindingRegistry reg, Camera cam)
        {
            var go = new GameObject("C98_DialogueUI");
            go.transform.SetParent(parent, false);
            var ui = go.AddComponent<C98_DialogueUI>();
            ui.bundle = bundle; ui.reg = reg; ui.cam = cam;
            ui.font = LoadKoreanFont();
            ui.BuildCanvas();
            return ui;
        }

        // 폴더 자급자족을 위해 OS 폰트를 동적 로드 — 실전은 06.Fonts의 프로젝트 폰트 사용
        static Font LoadKoreanFont()
        {
            string[] candidates = { "Apple SD Gothic Neo", "AppleGothic", "Malgun Gothic", "NanumGothic", "Arial Unicode MS" };
            foreach (var name in candidates)
            {
                var f = Font.CreateDynamicFontFromOSFont(name, 20);
                if (f != null) return f;
            }
            Debug.LogWarning("[C98] 한글 OS 폰트를 찾지 못함 — 기본 폰트로 진행(한글이 깨질 수 있음)");
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Text MakeText(Transform parent, string name, int size, Color color, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font; t.fontSize = size; t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        static RectTransform Stretch(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
            return rt;
        }

        void BuildCanvas()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            gameObject.AddComponent<GraphicRaycaster>();

            // ── 메인 말풍선 ──
            var bGo = new GameObject("main_bubble");
            bGo.transform.SetParent(transform, false);
            bubbleRoot = bGo.AddComponent<RectTransform>();
            bubbleRoot.sizeDelta = new Vector2(480, 120);
            bubblePanel = bGo.AddComponent<Image>();
            bubblePanel.color = new Color(0.06f, 0.07f, 0.1f, 0.92f);
            bubblePanel.raycastTarget = false;

            nameText = MakeText(bGo.transform, "name", 19, Color.white, TextAnchor.UpperLeft);
            Stretch(nameText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(16, -34), new Vector2(-16, -8));
            nameText.fontStyle = FontStyle.Bold;

            bodyText = MakeText(bGo.transform, "body", 20, new Color(0.92f, 0.94f, 0.96f), TextAnchor.UpperLeft);
            Stretch(bodyText.rectTransform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(16, 10), new Vector2(-16, -38));

            advanceIcon = MakeText(bGo.transform, "advance", 18, new Color(0.8f, 0.85f, 0.9f), TextAnchor.LowerRight);
            Stretch(advanceIcon.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 6), new Vector2(-10, 30));
            bubbleRoot.gameObject.SetActive(false);

            // ── 내레이션 패널(화면 하단 고정) ──
            var nGo = new GameObject("narration");
            nGo.transform.SetParent(transform, false);
            narrationRoot = nGo.AddComponent<RectTransform>();
            Stretch(narrationRoot, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, Vector2.zero);
            narrationRoot.sizeDelta = new Vector2(860, 96);
            narrationRoot.anchoredPosition = new Vector2(0, 150); // 레터박스(10%) 위에 오도록
            var nImg = nGo.AddComponent<Image>();
            nImg.color = new Color(0, 0, 0, 0.75f);
            nImg.raycastTarget = false;
            narrationText = MakeText(nGo.transform, "text", 21, new Color(0.9f, 0.9f, 0.95f), TextAnchor.MiddleCenter);
            Stretch(narrationText.rectTransform, Vector2.zero, Vector2.one, new Vector2(20, 10), new Vector2(-20, -10));
            narrationRoot.gameObject.SetActive(false);

            // ── 선택지 ──
            var cGo = new GameObject("choices");
            cGo.transform.SetParent(transform, false);
            choiceRoot = cGo.AddComponent<RectTransform>();
            Stretch(choiceRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            choiceRoot.sizeDelta = new Vector2(560, 300);
            choiceRoot.gameObject.SetActive(false);

            // ── 대기 화면 안내 ──
            var iGo = new GameObject("idle_overlay");
            iGo.transform.SetParent(transform, false);
            idleRoot = iGo.AddComponent<RectTransform>();
            Stretch(idleRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var iImg = iGo.AddComponent<Image>();
            iImg.color = new Color(0, 0, 0, 0.45f);
            iImg.raycastTarget = false;
            idleText = MakeText(iGo.transform, "text", 22, Color.white, TextAnchor.MiddleCenter);
            Stretch(idleText.rectTransform, Vector2.zero, Vector2.one, new Vector2(40, 40), new Vector2(-40, -40));

            // ── 토스트 ──
            toastText = MakeText(transform, "toast", 20, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            Stretch(toastText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            toastText.rectTransform.sizeDelta = new Vector2(700, 40);
            toastText.rectTransform.anchoredPosition = new Vector2(0, -60);
            toastText.text = "";
        }

        // ───────────────────────── 대기 화면 / 토스트 / 오류 ─────────────────────────

        public void ShowIdle(string text)
        {
            idleText.text = text;
            idleRoot.gameObject.SetActive(true);
        }

        public void HideIdle() => idleRoot.gameObject.SetActive(false);

        public void Toast(string msg)
        {
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo(msg));
        }

        IEnumerator ToastCo(string msg)
        {
            toastText.text = msg;
            yield return new WaitForSeconds(1.6f);
            toastText.text = "";
            toastCo = null;
        }

        // ───────────────────────── 대사 재생 ─────────────────────────

        // 대사 씬 실행 — main/narration은 블로킹, bark는 넘기고 진행. skip 요청 시 즉시 정리.
        public IEnumerator RunDialogueScene(C98_DialogueScene scene, System.Func<bool> skipRequested)
        {
            bool usedMain = false; // bark 전용 씬이 진행 중인 메인 말풍선을 닫지 않도록
            foreach (var step in scene.steps)
            {
                if (skipRequested()) break;
                if (step.channel == "bark")
                {
                    StartCoroutine(BarkCo(step));
                    continue;
                }
                usedMain = true;
                yield return RunSay(step, skipRequested);
            }
            if (usedMain) HideMainBubble();
        }

        IEnumerator RunSay(C98_SayStep step, System.Func<bool> skipRequested)
        {
            string text = C98_Loc.Pick(step.text_ko, step.text_en);
            var ch = string.IsNullOrEmpty(step.actor) ? null : bundle.Character(step.actor);
            bool narration = step.channel == "narration";

            Text target;
            if (narration)
            {
                narrationRoot.gameObject.SetActive(true);
                bubbleRoot.gameObject.SetActive(false);
                target = narrationText;
                trackedActor = null;
            }
            else
            {
                bubbleRoot.gameObject.SetActive(true);
                narrationRoot.gameObject.SetActive(false);
                target = bodyText;
                // 이름형/무명형 — 화자(actor) 유무 기준
                if (ch != null)
                {
                    nameText.text = C98_Loc.Pick(ch.name_ko, ch.name_en);
                    nameText.color = C98_Sprites.Hex(ch.name_color, Color.white);
                }
                else nameText.text = "";

                var actorComp = string.IsNullOrEmpty(step.actor) ? null : reg.Get<C98_Actor>(step.actor);
                trackWorld = step.bubble == "world" && actorComp != null && actorComp.Visible;
                if (step.bubble == "world" && (actorComp == null || !actorComp.Visible))
                {
                    // 앵커 부재 → screen 위치 대체 + 경고 (문서 규칙)
                    if (!anchorWarned)
                    {
                        Debug.LogWarning($"[C98] 화자 '{step.actor}' 말풍선 앵커 없음/비표시 — screen 위치로 대체");
                        anchorWarned = true;
                    }
                }
                trackedActor = trackWorld ? actorComp : null;
                if (!trackWorld)
                {
                    bubbleRoot.anchorMin = bubbleRoot.anchorMax = new Vector2(0.5f, 0f);
                    bubbleRoot.anchoredPosition = new Vector2(0, 160);
                }
            }

            // 타이핑 — 입력 시 현재 문장 전체 표시
            advanceIcon.text = "";
            target.text = "";
            int i = 0;
            float timer = 0;
            bool instant = false;
            yield return null; // 직전 스텝의 진행 입력이 같은 프레임에 소비되는 것 방지
            while (i < text.Length)
            {
                if (skipRequested()) { target.text = text; break; }
                if (C98_Input.AdvancePressed) instant = true;
                if (instant) { target.text = text; break; }
                timer += Time.deltaTime;
                while (timer >= TypingInterval && i < text.Length)
                {
                    timer -= TypingInterval;
                    i++;
                    target.text = text.Substring(0, i);
                }
                yield return null;
            }
            target.text = text;

            // 진행 대기 — input: ▼ 표시 후 입력, auto: 지연 후 자동
            if (step.advance == "auto")
            {
                advanceIcon.text = "…";
                float t = 0;
                while (t < step.auto_delay && !skipRequested()) { t += Time.deltaTime; yield return null; }
            }
            else
            {
                advanceIcon.text = "▼";
                yield return null;
                while (!C98_Input.AdvancePressed && !skipRequested()) yield return null;
            }
            advanceIcon.text = "";
            if (narration) narrationRoot.gameObject.SetActive(false);
        }

        public void HideMainBubble()
        {
            bubbleRoot.gameObject.SetActive(false);
            narrationRoot.gameObject.SetActive(false);
            trackedActor = null;
        }

        // 스킵 시 떠 있는 bark 즉시 정리
        public void ClearBarks()
        {
            foreach (var b in barks)
                if (b != null) Destroy(b.gameObject);
            barks.Clear();
        }

        IEnumerator BarkCo(C98_SayStep step)
        {
            var ch = string.IsNullOrEmpty(step.actor) ? null : bundle.Character(step.actor);
            var actorComp = string.IsNullOrEmpty(step.actor) ? null : reg.Get<C98_Actor>(step.actor);

            var go = new GameObject("bark");
            go.transform.SetParent(transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340, 64);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.08f, 0.09f, 0.13f, 0.85f);
            img.raycastTarget = false;
            var txt = MakeText(go.transform, "text", 18, new Color(0.9f, 0.92f, 0.95f), TextAnchor.MiddleCenter);
            Stretch(txt.rectTransform, Vector2.zero, Vector2.one, new Vector2(10, 6), new Vector2(-10, -6));
            string name = ch != null ? C98_Loc.Pick(ch.name_ko, ch.name_en) : null;
            string body = C98_Loc.Pick(step.text_ko, step.text_en);
            txt.text = string.IsNullOrEmpty(name) ? body : $"<b>{name}</b>  {body}";

            barks.Add(rt);
            while (barks.Count > MaxBarks)
            {
                var oldest = barks[0]; barks.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            float life = 2.2f, t = 0;
            while (t < life && rt != null)
            {
                t += Time.deltaTime;
                Vector2 pos;
                if (actorComp != null && actorComp.Visible)
                    pos = WorldToUi(actorComp.SpeechAnchor) + new Vector2(0, 46);
                else
                    pos = new Vector2(0, 250); // 화면 상단 — 방송/화면 밖 화자
                int stack = barks.IndexOf(rt);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = ClampToScreen(pos + new Vector2(0, stack * 70), rt.sizeDelta);
                yield return null;
            }
            barks.Remove(rt);
            if (rt != null) Destroy(rt.gameObject);
        }

        // ───────────────────────── 선택지 ─────────────────────────

        public IEnumerator ShowChoice(C98_Choice choice)
        {
            SelectedOption = -1;
            choiceRoot.gameObject.SetActive(true);
            foreach (var b in choiceButtons) Destroy(b);
            choiceButtons.Clear();

            for (int i = 0; i < choice.options.Count; i++)
            {
                var op = choice.options[i];
                bool unlocked = C98_Flags.Eval(op.when);

                var bGo = new GameObject($"opt_{i}");
                bGo.transform.SetParent(choiceRoot, false);
                var rt = bGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(540, 62);
                rt.anchoredPosition = new Vector2(0, 100 - i * 74);
                var img = bGo.AddComponent<Image>();
                img.color = unlocked ? new Color(0.13f, 0.16f, 0.24f, 0.95f) : new Color(0.10f, 0.10f, 0.12f, 0.9f);
                var btn = bGo.AddComponent<Button>();
                btn.interactable = unlocked;
                int idx = i;
                btn.onClick.AddListener(() => SelectedOption = idx);

                var label = MakeText(bGo.transform, "label", 20,
                    unlocked ? Color.white : new Color(0.5f, 0.5f, 0.55f), TextAnchor.MiddleCenter);
                Stretch(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(14, 4), new Vector2(-14, -4));
                label.raycastTarget = false;
                string body = $"{i + 1}. {C98_Loc.Pick(op.text_ko, op.text_en)}";
                if (!unlocked)
                    // 잠금 = 숨기지 않고 회색 + 부족 사유 (프로젝트 선택지 규칙)
                    body += $"\n<size=15><color=#997755>{C98_Loc.Pick(op.lock_reason_ko, op.lock_reason_en)}</color></size>";
                label.text = body;
                choiceButtons.Add(bGo);
            }

            yield return null;
            while (SelectedOption < 0)
            {
                for (int i = 0; i < choice.options.Count && i < 4; i++)
                    if (Input.GetKeyDown(KeyCode.Alpha1 + i) && C98_Flags.Eval(choice.options[i].when))
                        SelectedOption = i;
                yield return null;
            }

            choiceRoot.gameObject.SetActive(false);
            foreach (var b in choiceButtons) Destroy(b);
            choiceButtons.Clear();
        }

        // ───────────────────────── 월드 추적 ─────────────────────────

        void LateUpdate()
        {
            if (trackedActor == null || !bubbleRoot.gameObject.activeSelf) return;
            if (!trackedActor.Visible)
            {
                // 재생 중 화자가 사라지면 screen 위치로 폴백
                bubbleRoot.anchorMin = bubbleRoot.anchorMax = new Vector2(0.5f, 0f);
                bubbleRoot.anchoredPosition = new Vector2(0, 160);
                return;
            }
            bubbleRoot.anchorMin = bubbleRoot.anchorMax = new Vector2(0.5f, 0.5f);
            var pos = WorldToUi(trackedActor.SpeechAnchor) + new Vector2(0, 66);
            bubbleRoot.anchoredPosition = ClampToScreen(pos, bubbleRoot.sizeDelta);
        }

        Vector2 WorldToUi(Vector3 world)
        {
            var sp = cam.WorldToScreenPoint(world);
            var canvasRt = (RectTransform)canvas.transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, sp, null, out var local);
            return local;
        }

        Vector2 ClampToScreen(Vector2 pos, Vector2 size)
        {
            var canvasRt = (RectTransform)canvas.transform;
            float hw = canvasRt.rect.width * 0.5f, hh = canvasRt.rect.height * 0.5f;
            // 레터박스가 차 있는 동안에는 바 안쪽 영역으로 보정
            float bar = canvasRt.rect.height * C98_ScreenFx.LetterboxAmount;
            pos.x = Mathf.Clamp(pos.x, -hw + size.x * 0.5f + 8, hw - size.x * 0.5f - 8);
            pos.y = Mathf.Clamp(pos.y, -hh + size.y * 0.5f + 8 + bar, hh - size.y * 0.5f - 8 - bar);
            return pos;
        }
    }
}
