using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectLuna.WebAnimatic
{
    /// <summary>
    /// 웹 애니매틱 씬 데이터를 유니티에서 그대로 재생하는 플레이어.
    /// 웹 engine.js와 동일하게 "화면 상태 = 시간 t의 순수 함수"로 평가하므로
    /// 아무 시점으로 Seek해도 결과가 같다(에디터 캡처·검증에 그대로 쓴다).
    ///
    /// 웹과의 차이(v1에서 생략, 애니매틱 검토에는 지장 없음):
    /// tvnoise·slowmo 그레이드·speedline·crack 시각 효과, 사운드(웹도 플레이스홀더),
    /// 말풍선 점선 테두리·글자 떨림([shake]는 색만 유지).
    /// </summary>
    public class WebAnimaticPlayer : MonoBehaviour
    {
        public WebAnimaticLibrary library;
        public int sceneIndex;
        public float time;
        public bool playing;
        public float speed = 1f;

        const float VW = 480f, VH = 270f;
        const float PPU = 100f;

        SceneData _scene;
        string _loadedSceneId;

        Camera _cam;
        SpriteRenderer _stage;
        Transform _actorRoot, _fxRoot;
        readonly Dictionary<string, SpriteRenderer> _actors = new();
        readonly List<SpriteRenderer> _fxPool = new();
        readonly List<LineRenderer> _linePool = new();
        int _fxUsed, _lineUsed;
        Sprite _white;

        public Camera CaptureCamera => _cam;

        Canvas _canvas;
        Image _fadeImg, _barTop, _barBottom, _bubbleBg;
        TextMeshProUGUI _cutLabel, _cueLabel, _bubbleWho, _bubbleTxt, _bubbleTail;
        RectTransform _bubbleRoot;
        CanvasGroup _bubbleGroup;

        // ── 초기화 ──────────────────────────────────────────────

        public void EnsureInit()
        {
            if (_cam != null && _canvas != null) return;
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), PPU);

            _cam = GetComponentInChildren<Camera>();
            if (_cam == null)
            {
                GameObject go = new("AnimaticCamera");
                go.transform.SetParent(transform, false);
                _cam = go.AddComponent<Camera>();
            }
            _cam.orthographic = true;
            _cam.backgroundColor = Color.black;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.nearClipPlane = -50f; _cam.farClipPlane = 50f;

            _stage = Find<SpriteRenderer>("Stage");
            _stage.sortingOrder = -100;
            _actorRoot = FindT("Actors");
            _fxRoot = FindT("Fx");

            BuildCanvas();
        }

        T Find<T>(string name) where T : Component
        {
            Transform t = FindT(name);
            return t.TryGetComponent(out T c) ? c : t.gameObject.AddComponent<T>();
        }

        Transform FindT(string name)
        {
            Transform t = transform.Find(name);
            if (t == null)
            {
                GameObject go = new(name);
                go.transform.SetParent(transform, false);
                t = go.transform;
            }
            return t;
        }

        void BuildCanvas()
        {
            Transform ct = transform.Find("Overlay");
            if (ct == null)
            {
                GameObject go = new("Overlay");
                go.transform.SetParent(transform, false);
                ct = go.transform;
            }
            if (!ct.TryGetComponent(out _canvas)) _canvas = ct.gameObject.AddComponent<Canvas>();
            ct = _canvas.transform;   // Canvas 추가 시 Transform이 RectTransform으로 교체된다
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;   // RT 캡처에도 UI가 포함되도록
            _canvas.worldCamera = _cam;
            _canvas.planeDistance = 10f;
            _canvas.sortingOrder = 500;
            if (!ct.TryGetComponent(out CanvasScaler sc)) sc = ct.gameObject.AddComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(VW, VH);
            sc.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            _fadeImg = UiImage(ct, "Fade", new Color(0, 0, 0, 0), stretch: true);
            _barTop = UiImage(ct, "BarTop", Color.black);
            AnchorTop(_barTop.rectTransform);
            _barBottom = UiImage(ct, "BarBottom", Color.black);
            AnchorBottom(_barBottom.rectTransform);

            _cutLabel = UiText(ct, "CutLabel", 7f, new Color(0.81f, 0.88f, 0.92f));
            RectTransform cl = _cutLabel.rectTransform;
            cl.anchorMin = cl.anchorMax = new Vector2(0, 1); cl.pivot = new Vector2(0, 1);
            cl.anchoredPosition = new Vector2(4, -4); cl.sizeDelta = new Vector2(300, 12);
            _cutLabel.alignment = TextAlignmentOptions.TopLeft;

            _cueLabel = UiText(ct, "CueLabel", 6f, new Color(1f, 0.84f, 0.6f));
            RectTransform ql = _cueLabel.rectTransform;
            ql.anchorMin = ql.anchorMax = new Vector2(1, 1); ql.pivot = new Vector2(1, 1);
            ql.anchoredPosition = new Vector2(-4, -40); ql.sizeDelta = new Vector2(260, 10);
            _cueLabel.alignment = TextAlignmentOptions.TopRight;

            // 말풍선
            GameObject bub = new("Bubble");
            bub.transform.SetParent(ct, false);
            _bubbleRoot = bub.AddComponent<RectTransform>();
            _bubbleRoot.anchorMin = _bubbleRoot.anchorMax = new Vector2(0, 1);
            _bubbleGroup = bub.AddComponent<CanvasGroup>();
            _bubbleBg = bub.AddComponent<Image>();
            _bubbleBg.color = new Color(0.05f, 0.08f, 0.10f, 0.94f);
            Outline ol = bub.AddComponent<Outline>();
            ol.effectColor = new Color(0.87f, 0.94f, 0.96f);
            ol.effectDistance = new Vector2(1f, -1f);

            _bubbleWho = UiText(bub.transform, "Who", 6f, new Color(0.31f, 0.84f, 0.88f));
            RectTransform wr = _bubbleWho.rectTransform;
            wr.anchorMin = new Vector2(0, 1); wr.anchorMax = new Vector2(1, 1); wr.pivot = new Vector2(0, 1);
            wr.offsetMin = new Vector2(6, -14); wr.offsetMax = new Vector2(-6, -3);
            _bubbleWho.alignment = TextAlignmentOptions.TopLeft;
            _bubbleWho.enableWordWrapping = false;

            _bubbleTxt = UiText(bub.transform, "Txt", 7f, new Color(0.95f, 0.98f, 1f));
            RectTransform tr = _bubbleTxt.rectTransform;
            tr.anchorMin = new Vector2(0, 0); tr.anchorMax = new Vector2(1, 1); tr.pivot = new Vector2(0, 1);
            tr.offsetMin = new Vector2(6, 5); tr.offsetMax = new Vector2(-6, -14);
            _bubbleTxt.alignment = TextAlignmentOptions.TopLeft;
            _bubbleTxt.enableWordWrapping = true;

            _bubbleTail = UiText(bub.transform, "Tail", 7f, new Color(0.87f, 0.94f, 0.96f));
            RectTransform tl = _bubbleTail.rectTransform;
            tl.anchorMin = tl.anchorMax = new Vector2(0.5f, 0); tl.pivot = new Vector2(0.5f, 1);
            tl.anchoredPosition = new Vector2(0, 1.5f); tl.sizeDelta = new Vector2(12, 8);
            _bubbleTail.text = "▼";
            _bubbleTail.alignment = TextAlignmentOptions.Top;
        }

        Image UiImage(Transform parent, string name, Color c, bool stretch = false)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            Image img = go.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            if (stretch)
            {
                RectTransform r = img.rectTransform;
                r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
            }
            return img;
        }

        TextMeshProUGUI UiText(Transform parent, string name, float size, Color c)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent, false);
            TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
            if (library != null && library.font != null) t.font = library.font;
            t.fontSize = size;
            t.color = c;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        static void AnchorTop(RectTransform r)
        {
            r.anchorMin = new Vector2(0, 1); r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(0, -0.01f); r.offsetMax = Vector2.zero;
        }

        static void AnchorBottom(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0);
            r.offsetMin = Vector2.zero; r.offsetMax = new Vector2(0, 0.01f);
        }

        // ── 씬 로드 ─────────────────────────────────────────────

        public void LoadScene(int index)
        {
            sceneIndex = Mathf.Clamp(index, 0, library.scenes.Length - 1);
            var entry = library.scenes[sceneIndex];
            _scene = SceneData.Parse(entry.json.text);
            _loadedSceneId = entry.id;
            foreach (var kv in _actors) if (kv.Value != null) DestroyObj(kv.Value.gameObject);
            _actors.Clear();
            time = 0f;
            Seek(0f);
        }

        static void DestroyObj(UnityEngine.Object o)
        {
            if (Application.isPlaying) Destroy(o); else DestroyImmediate(o);
        }

        void Start()
        {
            EnsureInit();
            if (_scene == null && library != null && library.scenes.Length > 0)
                LoadScene(sceneIndex);
            playing = Application.isPlaying;
        }

        void Update()
        {
            if (_scene == null) return;
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (Application.isPlaying && kb != null)
            {
                if (kb.spaceKey.wasPressedThisFrame) { if (time >= _scene.end) time = 0f; playing = !playing; }
                if (kb.rKey.wasPressedThisFrame) { time = 0f; playing = true; }
                if (kb.rightArrowKey.wasPressedThisFrame) time = Mathf.Min(_scene.end, time + 1f);
                if (kb.leftArrowKey.wasPressedThisFrame) time = Mathf.Max(0f, time - 1f);
                for (int i = 0; i < Mathf.Min(9, library.scenes.Length); i++)
                {
                    var key = (UnityEngine.InputSystem.Key)((int)UnityEngine.InputSystem.Key.Digit1 + i);
                    if (kb[key].wasPressedThisFrame) LoadScene(i);
                }
            }
#endif
            if (playing && Application.isPlaying)
            {
                time += Time.deltaTime * speed;
                if (time >= _scene.end) { time = _scene.end; playing = false; }
            }
            Seek(time);
        }

        void OnGUI()
        {
            if (!Application.isPlaying || _scene == null || library == null) return;
            GUI.Label(new Rect(8, Screen.height - 24, 700, 20),
                $"[{library.scenes[sceneIndex].label}]  {time:F2}/{_scene.end:F2}s   Space 재생/정지 · ←→ 1초 · R 처음 · 1~{library.scenes.Length} 씬");
        }

        // ── 평가 ────────────────────────────────────────────────

        struct CamState { public float x, y, z; }

        CamState EvalCamera(float t)
        {
            var st = ActiveStage(t);
            float z = WebEval.Num(_scene.camZ, t, 1f); if (z <= 0f) z = 1f;
            float x = WebEval.Num(_scene.camX, t, st.width / 2f);
            float y = WebEval.Num(_scene.camY, t, st.floor - 90f);
            foreach (FxData f in _scene.fx)
            {
                if (f.type != "shake" || t < f.t || t > f.t + f.dur) continue;
                float u = 1f - (t - f.t) / f.dur;
                double k = Math.Floor(t * (f.hz > 0 ? f.hz : 34f));
                x += WebEval.Srnd(k * 3.1 + f.t) * f.amp * u;
                y += WebEval.Srnd(k * 7.7 + f.t) * f.amp * u * 0.7f;
            }
            float hw = VW / (2f * z), hh = VH / (2f * z);
            x = Mathf.Clamp(x, hw, st.width - hw);
            y = Mathf.Clamp(y, hh, st.height - hh);
            return new CamState { x = x, y = y, z = z };
        }

        WebAnimaticLibrary.StageEntry ActiveStage(float t)
        {
            string id = _scene.stages[0].id;
            foreach (var e in _scene.stages) if (t >= e.t) id = e.id;
            return library.Stage(id);
        }

        float Track(string name, float t)
        {
            return _scene.stageTracks.TryGetValue(name, out var keys) ? WebEval.Num(keys, t, 0f) : 0f;
        }

        static Vector3 W2U(float wx, float wy, float depth = 0f) => new(wx / PPU, -wy / PPU, depth);

        public void Seek(float t)
        {
            if (_scene == null || library == null) return;
            EnsureInit();
            time = Mathf.Clamp(t, 0f, _scene.end);
            t = time;

            CamState cam = EvalCamera(t);
            _cam.transform.position = new Vector3(cam.x / PPU, -cam.y / PPU, -10f);
            _cam.orthographicSize = (VH / (2f * cam.z)) / PPU;

            // 무대 배경(상태 조합에 따라 변형 스프라이트 교체)
            var stage = ActiveStage(t);
            _stage.sprite = library.StageSprite(StageVariantId(stage.id, t));
            _stage.transform.position = Vector3.zero;

            _fxUsed = 0; _lineUsed = 0;
            DrawActors(t);
            DrawWorldFx(t);
            TrimPools();
            DrawOverlay(t, cam);
            DrawBubble(t, cam);
        }

        string StageVariantId(string id, float t)
        {
            switch (id)
            {
                case "lobby":
                    if (Track("door", t) > 0.5f) return "lobby_door1";
                    return Track("alarm", t) > 0.3f ? "lobby_door0_alarm" : "lobby_door0";
                case "disposal":
                    return $"disposal_d{(Track("door", t) > 0.5f ? 1 : 0)}c{(Track("chute", t) > 0.5f ? 1 : 0)}";
                case "corridor": return "corridor";
                default: return "shaft";
            }
        }

        // ── 배우 ────────────────────────────────────────────────

        void DrawActors(float t)
        {
            foreach (ActorData a in _scene.actors)
            {
                if (!_actors.TryGetValue(a.id, out SpriteRenderer sr) || sr == null)
                {
                    GameObject go = new("actor_" + a.id);
                    go.transform.SetParent(_actorRoot, false);
                    sr = go.AddComponent<SpriteRenderer>();
                    _actors[a.id] = sr;
                }
                bool vis = WebEval.Num(a.vis, t, 1f) > 0.5f;
                float alpha = WebEval.Num(a.alpha, t, 1f);
                if (!vis || alpha <= 0.01f) { sr.enabled = false; continue; }
                sr.enabled = true;

                var animKey = WebEval.Step(a.anim, t);
                var anim = library.Anim(animKey.v);
                if (anim == null) { sr.enabled = false; continue; }
                int n = anim.to - anim.from + 1;
                int k = Mathf.FloorToInt((t - animKey.t) * anim.fps);
                if (k < 0) k = 0;
                k = anim.loop ? ((k % n) + n) % n : Mathf.Min(k, n - 1);
                int frame = anim.from + k;

                float ax, ay;
                var sheet = library.Sheet(anim.sheet);
                var fm = sheet.frames[frame];
                if (anim.hasAnchor) { ax = anim.ax; ay = anim.ay; }
                else { ax = fm.ox + fm.sw / 2f; ay = fm.oy + fm.sh; }
                sr.sprite = library.Frame(anim.sheet, frame, ax, ay);

                float x = WebEval.Num(a.x, t, 0f);
                float y = WebEval.Num(a.y, t, 0f);
                if (a.bob > 0f) y += -Mathf.Abs(Mathf.Sin(t * a.bobHz * Mathf.PI)) * a.bob;
                float rot = WebEval.Num(a.rot, t, 0f);
                float scale = WebEval.Num(a.scale, t, 1f);
                bool flip = WebEval.Num(a.flip, t, 0f) > 0.5f;

                sr.transform.position = W2U(x, y);
                sr.transform.localRotation = Quaternion.Euler(0, 0, -rot);
                sr.transform.localScale = new Vector3(scale, scale, 1f);
                sr.flipX = flip;
                sr.sortingOrder = a.z * 10;

                Color c = Color.white;
                if (!string.IsNullOrEmpty(a.tint) && ColorUtility.TryParseHtmlString(a.tint, out Color tc))
                    c = Color.Lerp(Color.white, tc, a.tintAmount);
                c.a = alpha;
                sr.color = c;
            }
        }

        // ── 월드 이펙트 ─────────────────────────────────────────

        SpriteRenderer FxQuad()
        {
            SpriteRenderer sr;
            if (_fxUsed < _fxPool.Count) sr = _fxPool[_fxUsed];
            else
            {
                GameObject go = new("fx" + _fxUsed);
                go.transform.SetParent(_fxRoot, false);
                sr = go.AddComponent<SpriteRenderer>();
                _fxPool.Add(sr);
            }
            _fxUsed++;
            sr.enabled = true;
            sr.flipX = false;
            sr.sortingOrder = 100;
            sr.transform.localRotation = Quaternion.identity;
            return sr;
        }

        void Quad(float wx, float wy, float w, float h, Color c, float rotDeg = 0f)
        {
            SpriteRenderer sr = FxQuad();
            sr.sprite = _white;
            sr.color = c;
            sr.transform.position = W2U(wx + w / 2f, wy + h / 2f);
            sr.transform.localScale = new Vector3(w / 4f, h / 4f, 1f); // 화이트 스프라이트 4px 기준
            sr.transform.localRotation = Quaternion.Euler(0, 0, -rotDeg);
        }

        LineRenderer FxLine()
        {
            LineRenderer lr;
            if (_lineUsed < _linePool.Count) lr = _linePool[_lineUsed];
            else
            {
                GameObject go = new("line" + _lineUsed);
                go.transform.SetParent(_fxRoot, false);
                lr = go.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.sortingOrder = 100;
                lr.useWorldSpace = true;
                _linePool.Add(lr);
            }
            _lineUsed++;
            lr.enabled = true;
            return lr;
        }

        void TrimPools()
        {
            for (int i = _fxUsed; i < _fxPool.Count; i++) _fxPool[i].enabled = false;
            for (int i = _lineUsed; i < _linePool.Count; i++) _linePool[i].enabled = false;
        }

        void DrawWorldFx(float t)
        {
            foreach (FxData f in _scene.fx)
            {
                if (t < f.t || t > f.t + f.dur) continue;
                float u = f.dur > 0f ? (t - f.t) / f.dur : 0f;
                switch (f.type)
                {
                    case "dust":
                    {
                        var sheet = library.Sheet("lab_effect-Sheet");
                        int n = sheet.frames.Length;
                        int i = Mathf.Min(n - 1, Mathf.FloorToInt(u * n));
                        var fm = sheet.frames[i];
                        SpriteRenderer sr = FxQuad();
                        sr.sprite = library.Frame("lab_effect-Sheet", i, fm.ox + fm.sw / 2f, fm.oy + fm.sh);
                        float alpha = (f.alpha < 0f ? 0.62f : f.alpha) * (1f - u * 0.45f);
                        Color dc = Color.Lerp(Color.white, new Color(0.36f, 0.39f, 0.44f), 0.62f);
                        dc.a = alpha;
                        sr.color = dc;
                        sr.transform.position = W2U(f.x, f.y);
                        sr.transform.localScale = new Vector3(f.scale, f.scale, 1f);
                        sr.flipX = f.flip;
                        break;
                    }
                    case "debris":
                    {
                        for (int i = 0; i < f.count; i++)
                        {
                            float s = WebEval.Rnd(f.seed + i * 4.3);
                            float s2 = WebEval.Rnd(f.seed + i * 9.1);
                            float vx = (0.5f + s * 1.6f) * f.power;
                            float vy = -(0.15f + s2 * 1.1f) * f.power * 0.55f;
                            float tt = u * f.dur;
                            float px = f.x + vx * tt;
                            float py = Mathf.Min(f.groundY, f.y + vy * tt + 470f * tt * tt);
                            float w = 3f + s * 9f, h = 2f + s2 * 7f;
                            Color c = s > 0.66f ? Hex("#4a5b6b") : s > 0.33f ? Hex("#3a4653") : Hex("#8a6a24");
                            Quad(px - w / 2f, py - h / 2f, w, h, c, (s - 0.5f) * 14f * tt * Mathf.Rad2Deg);
                        }
                        break;
                    }
                    case "muzzle":
                    {
                        float a = 1f - u;
                        Quad(f.x - 5, f.y - 2, 11, 4, new Color(1f, 0.91f, 0.67f, 0.9f * a));
                        Quad(f.x - 2, f.y - 5, 4, 11, new Color(1f, 0.91f, 0.67f, 0.9f * a));
                        Quad(f.x - 2, f.y - 2, 4, 4, new Color(1f, 1f, 1f, 0.7f * a));
                        break;
                    }
                    case "tracer":
                    {
                        LineRenderer lr = FxLine();
                        lr.positionCount = 2;
                        lr.SetPosition(0, W2U(f.x, f.y));
                        lr.SetPosition(1, W2U(f.x2, f.y2));
                        lr.startWidth = lr.endWidth = 1f / PPU;
                        Color c = Hex("#ffeab4"); c.a = (1f - u) * 0.85f;
                        lr.startColor = lr.endColor = c;
                        break;
                    }
                    case "spark":
                    {
                        float a = 1f - u;
                        for (int i = 0; i < 5; i++)
                        {
                            float s = WebEval.Rnd(f.t * 31 + i);
                            Quad(f.x + WebEval.Srnd(s * 3) * 7f - u * WebEval.Srnd(s) * 10f,
                                 f.y + WebEval.Srnd(s * 5) * 9f - u * 8f, 2, 2,
                                 new Color(1f, 0.86f, 0.71f, 0.8f * a));
                        }
                        break;
                    }
                    case "link":
                    {
                        ActorData A = _scene.actors.Find(x => x.id == f.a);
                        ActorData B = _scene.actors.Find(x => x.id == f.b);
                        if (A == null || B == null) break;
                        if (WebEval.Num(A.vis, t, 1f) < 0.5f || WebEval.Num(B.vis, t, 1f) < 0.5f) break;
                        float ax = WebEval.Num(A.x, t, 0) + f.ax;
                        float ayW = WebEval.Num(A.y, t, 0) + (A.bob > 0 ? -Mathf.Abs(Mathf.Sin(t * A.bobHz * Mathf.PI)) * A.bob : 0f) + f.ay;
                        float bx = WebEval.Num(B.x, t, 0) + f.bx;
                        float byW = WebEval.Num(B.y, t, 0) + (B.bob > 0 ? -Mathf.Abs(Mathf.Sin(t * B.bobHz * Mathf.PI)) * B.bob : 0f) + f.by;
                        LineRenderer lr = FxLine();
                        const int SEG = 8;
                        lr.positionCount = SEG + 1;
                        float mx = (ax + bx) / 2f, my = Mathf.Max(ayW, byW) + f.sag;
                        for (int i = 0; i <= SEG; i++)
                        {
                            float q = i / (float)SEG;
                            float px = (1 - q) * (1 - q) * ax + 2 * (1 - q) * q * mx + q * q * bx;
                            float py = (1 - q) * (1 - q) * ayW + 2 * (1 - q) * q * my + q * q * byW;
                            lr.SetPosition(i, W2U(px, py));
                        }
                        lr.startWidth = lr.endWidth = f.w / PPU;
                        Color lc = string.IsNullOrEmpty(f.color) ? Color.white : Hex(f.color);
                        lr.startColor = lr.endColor = lc;
                        break;
                    }
                }
            }
        }

        static Color Hex(string h)
        {
            return ColorUtility.TryParseHtmlString(h, out Color c) ? c : Color.white;
        }

        // ── 화면 오버레이 ───────────────────────────────────────

        float OverlayDim(float t)
        {
            float dim = 0f;
            foreach (FxData f in _scene.fx)
            {
                if (t < f.t || t > f.t + f.dur) continue;
                if (f.type == "fade")
                {
                    float u = (t - f.t) / f.dur;
                    dim = Mathf.Max(dim, f.dir == "in" ? 1f - u : u);
                }
                else if (f.type == "black" || f.type == "whitehold") dim = 1f;
            }
            return dim;
        }

        void DrawOverlay(float t, CamState cam)
        {
            // 페이드·암전·화이트홀드·플래시를 fx 순서대로 하나의 색으로 합성
            Color acc = new(0, 0, 0, 0);
            foreach (FxData f in _scene.fx)
            {
                if (t < f.t || t > f.t + f.dur) continue;
                float u = f.dur > 0 ? (t - f.t) / f.dur : 0f;
                Color layer;
                float a;
                switch (f.type)
                {
                    case "fade":
                        a = f.dir == "in" ? 1f - u : u;
                        layer = string.IsNullOrEmpty(f.color) ? Color.black : Hex(f.color);
                        break;
                    case "black": layer = Color.black; a = 1f; break;
                    case "whitehold": layer = Color.white; a = 1f; break;
                    case "flash":
                        a = f.peak * Mathf.Pow(1f - u, f.falloff);
                        layer = string.IsNullOrEmpty(f.color) ? Color.white : Hex(f.color);
                        break;
                    default: continue;
                }
                a = Mathf.Clamp01(a);
                float outA = a + acc.a * (1f - a);
                if (outA > 0.0001f)
                    acc = new Color(
                        (layer.r * a + acc.r * acc.a * (1f - a)) / outA,
                        (layer.g * a + acc.g * acc.a * (1f - a)) / outA,
                        (layer.b * a + acc.b * acc.a * (1f - a)) / outA, outA);
            }
            _fadeImg.color = acc;

            // 레터박스
            float barH = 0f;
            foreach (FxData f in _scene.fx)
            {
                if (f.type != "lbox") continue;
                float u = Mathf.Clamp01((t - f.t) / f.dur);
                barH = Mathf.Round(VH * 0.12f * WebEval.Ease("ec", u));
            }
            _barTop.rectTransform.sizeDelta = new Vector2(0, barH);
            _barBottom.rectTransform.sizeDelta = new Vector2(0, barH);

            // 컷 라벨 · 사운드 큐 칩
            string cut = null;
            foreach (CutData c in _scene.cuts) if (t >= c.t) cut = c.label;
            _cutLabel.text = cut ?? "";
            string cue = null;
            foreach (CueData c in _scene.sfx) if (t >= c.t && t - c.t < 1.7f) cue = c.label;
            _cueLabel.text = cue != null ? "♪ " + cue : "";
        }

        // ── 말풍선 ──────────────────────────────────────────────

        const string NZ_FULL_WANT = "＃＠％＆￦＄？！";
        const string NZ_FULL_FALLBACK = "▓▒#@$%&?!";
        const string NZ_HALF = "#@$%&!?~^=/";
        string _nzFull;

        string NzFull()
        {
            if (_nzFull != null) return _nzFull;
            var sb = new StringBuilder();
            foreach (char c in NZ_FULL_WANT)
                if (library.font != null && library.font.HasCharacter(c)) sb.Append(c);
            _nzFull = sb.Length >= 4 ? sb.ToString() : NZ_FULL_FALLBACK;
            return _nzFull;
        }

        LineData LineAt(float t)
        {
            foreach (LineData l in _scene.lines)
                if (t >= l.t && t < l.t + l.dur) return l;
            return null;
        }

        void DrawBubble(float t, CamState cam)
        {
            LineData l = LineAt(t);
            if (l == null) { _bubbleRoot.gameObject.SetActive(false); return; }
            _bubbleRoot.gameObject.SetActive(true);
            _bubbleGroup.alpha = 1f - OverlayDim(t);

            bool isRadio = l.kind == "radio", isPa = l.kind == "pa";
            _bubbleWho.text = isRadio ? "● 무전" : isPa ? "● 방송" : l.speaker;
            _bubbleWho.color = isRadio ? Hex("#6fd6e0") : isPa ? Hex("#e0a03a") : Hex("#4fd6e0");

            int n = WebEval.TypedCount(l, t - l.t - l.lead);
            int nk = Mathf.FloorToInt(t * 15f);
            _bubbleTxt.text = BuildRich(l, n, nk);

            // 크기 — 웹과 같은 규칙: 원문 전체 폭으로 확정, 최소 한글 5자, 화자명 한 줄
            string full = ProcessedFull(l);
            float maxW = VW * 0.62f;
            Vector2 pref = _bubbleTxt.GetPreferredValues(full, maxW - 12f, 0f);
            float min5 = _bubbleTxt.GetPreferredValues("가나다라마").x;
            float whoW = _bubbleWho.GetPreferredValues(_bubbleWho.text).x;
            float w = Mathf.Min(Mathf.Max(pref.x, min5, whoW) + 13f, maxW);
            Vector2 wrapped = _bubbleTxt.GetPreferredValues(full, w - 12f, 0f);
            float h = wrapped.y + 20f;
            _bubbleRoot.sizeDelta = new Vector2(w, h);

            // 위치 — 화자 머리 위 또는 월드 좌표(at)
            float px, py;
            bool anchored = false, below = l.below;
            if (l.at != null)
            {
                px = (l.at[0] - cam.x) * cam.z + VW / 2f;
                py = (l.at[1] - cam.y) * cam.z + VH / 2f;
                anchored = true;
            }
            else
            {
                string id = l.actor ?? ((isRadio || isPa) ? _scene.radioActor : null);
                ActorData a = id != null ? _scene.actors.Find(x => x.id == id) : null;
                if (a != null && WebEval.Num(a.vis, t, 1f) > 0.5f)
                {
                    var animKey = WebEval.Step(a.anim, t);
                    var anim = library.Anim(animKey.v);
                    float contentH = anim != null ? library.Sheet(anim.sheet).contentH : 40f;
                    float scale = WebEval.Num(a.scale, t, 1f);
                    float wx = WebEval.Num(a.x, t, 0f);
                    float wy = WebEval.Num(a.y, t, 0f) - contentH * scale - 8f;
                    px = (wx - cam.x) * cam.z + VW / 2f;
                    py = (wy - cam.y) * cam.z + VH / 2f;
                    anchored = true;
                }
                else { px = VW / 2f; py = VH * 0.6f; }
            }

            float left = Mathf.Clamp(px - w / 2f, 6f, VW - w - 6f);
            float top = below ? py + 7f : py - h - 6f;
            top = Mathf.Clamp(top, 16f, VH - h - 16f);
            _bubbleRoot.pivot = new Vector2(0, 1);
            _bubbleRoot.anchoredPosition = new Vector2(left, -top);

            _bubbleTail.gameObject.SetActive(anchored);
            _bubbleTail.text = below ? "▲" : "▼";
            RectTransform tl = _bubbleTail.rectTransform;
            tl.anchorMin = tl.anchorMax = new Vector2(0, below ? 1 : 0);
            tl.pivot = new Vector2(0.5f, below ? 0f : 1f);
            float tailX = Mathf.Clamp(px - left, 8f, w - 8f);
            tl.anchoredPosition = new Vector2(tailX, below ? 6f : -6f + 7.5f);
            _bubbleTail.color = isPa ? Hex("#e0a03a") : isRadio ? Hex("#6fd6e0") : Hex("#dfeff5");

            Color border = isPa ? Hex("#e0a03a") : isRadio ? Hex("#6fd6e0") : Hex("#dfeff5");
            _bubbleBg.GetComponent<Outline>().effectColor = border;
        }

        string ProcessedFull(LineData l)
        {
            StringBuilder sb = new();
            for (int i = 0; i < l.text.Length; i++)
            {
                char ch = l.text[i];
                if (IsCensored(l, i) && !char.IsWhiteSpace(ch)) ch = "▓▒█▒"[i % 4];
                sb.Append(ch);
            }
            return sb.ToString();
        }

        bool IsCensored(LineData l, int i)
        {
            foreach (SpanData s in l.spans)
                if (s.type == "censor" && i >= s.i0 && i < s.i1) return true;
            return false;
        }

        SpanData SpanAt(LineData l, int i, string type)
        {
            foreach (SpanData s in l.spans)
                if (s.type == type && i >= s.i0 && i < s.i1) return s;
            return null;
        }

        string BuildRich(LineData l, int visible, int nk)
        {
            StringBuilder sb = new();
            string openColor = null;
            for (int i = 0; i < visible && i < l.text.Length; i++)
            {
                char ch = l.text[i];
                bool space = char.IsWhiteSpace(ch);
                SpanData cSpan = SpanAt(l, i, "c");
                SpanData nSpan = SpanAt(l, i, "noise");
                bool censor = IsCensored(l, i);

                string want = cSpan?.param;
                if (censor) want = "#66757f";
                else if (nSpan != null && want == null) want = "#bfeef6";
                if (want != openColor)
                {
                    if (openColor != null) sb.Append("</color>");
                    if (want != null) sb.Append("<color=").Append(want).Append('>');
                    openColor = want;
                }

                if (censor && !space) sb.Append("▓▒█▒"[i % 4]);
                else if (nSpan != null && !space) sb.Append(Scramble(ch, i, nk));
                else if (ch == '<') sb.Append("<​");   // TMP 태그 오인 방지
                else sb.Append(ch);
            }
            if (openColor != null) sb.Append("</color>");
            return sb.ToString();
        }

        char Scramble(char orig, int i, int k)
        {
            float r = WebEval.Rnd(k * 31.7 + i * 7.3);
            if (r < 0.30f) return orig;                    // 원문 스침 (웹과 동일 확률·시드)
            bool hang = orig >= '가' && orig <= '힣';
            float pick = WebEval.Rnd(k * 13.1 + i * 3.7);
            string set = hang ? NzFull() : NZ_HALF;
            return set[Mathf.FloorToInt(pick * set.Length) % set.Length];
        }
    }
}
