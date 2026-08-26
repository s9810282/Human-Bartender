// 98.Claude 컷씬 프로토타입 — 바인딩 대상(배우/오브젝트/앵커) + 무대 구성
// 아트 리소스를 쓰지 않고 전부 런타임 생성 스프라이트로 대체한다.
//
// v2: 무대를 에디터에서 씬에 영구 배치할 수 있다(Timeline 스크럽 미리보기용).
//     생성 스프라이트는 에셋이 아니라 씬에 저장되지 않으므로, C98_AutoSprite가
//     OnEnable마다 다시 만들어 붙인다. 바인딩은 C98_Id 스캔으로 구성한다.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Claude98
{
    // ───────────────────────── 스프라이트 팩토리 ─────────────────────────

    public static class C98_Sprites
    {
        static Sprite _white;
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    tex.SetPixels(px); tex.Apply();
                    _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                    _white.hideFlags = HideFlags.DontSave;
                }
                return _white;
            }
        }

        static Sprite _circle;
        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                {
                    const int n = 64;
                    var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = x - n / 2f + 0.5f, dy = y - n / 2f + 0.5f;
                            bool inside = dx * dx + dy * dy <= (n / 2f - 1) * (n / 2f - 1);
                            tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                        }
                    tex.Apply();
                    _circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                    _circle.hideFlags = HideFlags.DontSave;
                }
                return _circle;
            }
        }

        public static SpriteRenderer Rect(Transform parent, string name, Color color,
            Vector2 pos, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color; sr.sortingOrder = order;
            go.AddComponent<C98_AutoSprite>().circle = false;
            sr.sprite = White;
            return sr;
        }

        public static SpriteRenderer Dot(Transform parent, string name, Color color,
            Vector2 pos, float diameter, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = Vector3.one * diameter;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = color; sr.sortingOrder = order;
            go.AddComponent<C98_AutoSprite>().circle = true;
            sr.sprite = Circle;
            return sr;
        }

        public static Color Hex(string hex, Color fallback)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : fallback;
        }
    }

    // 생성 스프라이트는 씬에 저장되지 않는다 — 로드·도메인 리로드 때마다 다시 붙인다.
    [ExecuteAlways]
    public class C98_AutoSprite : MonoBehaviour
    {
        public bool circle;

        void OnEnable()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite == null)
                sr.sprite = circle ? C98_Sprites.Circle : C98_Sprites.White;
        }
    }

    // ───────────────────────── 바인딩 ─────────────────────────

    // 씬에 배치된 컷씬 대상물의 ID 표식 — 레지스트리와 Timeline 트랙이 이걸로 찾는다.
    public class C98_Id : MonoBehaviour
    {
        public string id;
    }

    // 편집용으로 씬에 배치된 무대의 루트 표식 — 있으면 Bootstrap이 무대를 새로 짓지 않는다.
    public class C98_StageRoot : MonoBehaviour { }

    // Scene의 컷씬 대상물을 ID로 등록·조회한다. 중복 ID는 등록 시점에 오류.
    public class C98_BindingRegistry
    {
        readonly Dictionary<string, Component> map = new Dictionary<string, Component>();
        public readonly List<string> errors = new List<string>();

        public void Register(string id, Component c)
        {
            if (string.IsNullOrEmpty(id)) { errors.Add("바인딩 ID 공란"); return; }
            if (map.ContainsKey(id)) { errors.Add($"바인딩 ID 중복: {id}"); return; }
            map[id] = c;
        }

        // C98_Id 표식 스캔으로 레지스트리 구성 — 대표 컴포넌트 우선순위: Actor > Object > Anchor > 그 외
        public void BuildFromScene()
        {
            foreach (var tag in Object.FindObjectsByType<C98_Id>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Component c = (Component)tag.GetComponent<C98_Actor>()
                    ?? (Component)tag.GetComponent<C98_SceneObject>()
                    ?? (Component)tag.GetComponent<C98_Anchor>()
                    ?? (Component)tag.GetComponent<SpriteRenderer>()
                    ?? tag.transform;
                Register(tag.id, c);
            }
        }

        public T Get<T>(string id) where T : Component
            => map.TryGetValue(id, out var c) ? c as T : null;

        public bool Has(string id) => map.ContainsKey(id);
        public IEnumerable<string> Ids() => map.Keys;
    }

    // ───────────────────────── 앵커 ─────────────────────────

    // 좌표는 JSON이 아니라 씬(무대)이 소유한다 — 문서 규칙. 씬 뷰에서 기즈모로 보인다.
    public class C98_Anchor : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, 0.15f);
#if UNITY_EDITOR
            var tag = GetComponent<C98_Id>();
            if (tag != null)
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.2f, tag.id);
#endif
        }
    }

    // ───────────────────────── 조명 프리셋 쿼드 ─────────────────────────

    // 조명 프리셋의 실체 — 무대를 덮는 월드 틴트. Timeline 조명 클립이 미리보기로 확인할 수 있다.
    [ExecuteAlways]
    public class C98_LightQuad : MonoBehaviour
    {
        public string preset = "light_default";
        SpriteRenderer sr;

        public static C98_LightQuad Instance { get; private set; }

        void OnEnable()
        {
            Instance = this;
            sr = GetComponent<SpriteRenderer>();
            Apply(preset);
        }

        void OnDisable() { if (Instance == this) Instance = null; }

        public void Apply(string p)
        {
            preset = p;
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            switch (p)
            {
                case "light_lab_alarm": sr.color = new Color(0.9f, 0.06f, 0.05f, 0.20f); break;
                case "light_blackout": sr.color = new Color(0, 0, 0, 0.82f); break;
                default: sr.color = Color.clear; break;
            }
        }

        void Update()
        {
            // 경보 점멸은 런타임 전용 — 에디터 미리보기에선 고정 색만 보여준다
            if (!Application.isPlaying || preset != "light_lab_alarm" || sr == null) return;
            float k = (Mathf.Sin(Time.time * 5.2f) + 1f) * 0.5f;
            var c = sr.color;
            c.a = Mathf.Lerp(0.10f, 0.30f, k);
            sr.color = c;
        }
    }

    // ───────────────────────── 배우 ─────────────────────────

    [ExecuteAlways]
    public class C98_Actor : MonoBehaviour
    {
        public string id;
        public Color bodyColor = Color.gray;

        SpriteRenderer body, head, eye;
        Coroutine moving, pulsing;
        int facing = 1; // 1=right, -1=left

        public string Id => id;
        public Color BodyColor => bodyColor;

        public static C98_Actor Create(Transform parent, C98_Character def)
        {
            var go = new GameObject($"actor_{def.id}");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<C98_Actor>();
            a.id = def.id;
            a.bodyColor = C98_Sprites.Hex(def.body_color, Color.gray);
            a.EnsureVisuals();
            a.ApplyColors();
            return a;
        }

        void OnEnable() { EnsureVisuals(); }

        // 씬에 저장된 배우도 로드 시 시각 요소를 복원한다 (find-by-name → 없으면 생성)
        public void EnsureVisuals()
        {
            if (body != null && head != null && eye != null) return;
            var bodyT = transform.Find("body");
            body = bodyT != null ? bodyT.GetComponent<SpriteRenderer>()
                : C98_Sprites.Rect(transform, "body", bodyColor, new Vector2(0, 0.55f), new Vector2(0.52f, 1.1f), 20);
            var headT = transform.Find("head");
            head = headT != null ? headT.GetComponent<SpriteRenderer>()
                : C98_Sprites.Dot(transform, "head", bodyColor * 1.15f, new Vector2(0, 1.32f), 0.44f, 21);
            var eyeT = transform.Find("eye");
            eye = eyeT != null ? eyeT.GetComponent<SpriteRenderer>()
                : C98_Sprites.Dot(transform, "eye", new Color(0.08f, 0.08f, 0.1f), new Vector2(0.1f, 1.36f), 0.09f, 22);
        }

        void ApplyColors()
        {
            body.color = bodyColor;
            head.color = bodyColor * 1.15f;
        }

        // 말풍선이 따라갈 기준점 (SpeechBubbleAnchor 역할)
        public Vector3 SpeechAnchor => transform.position + new Vector3(0, 1.75f, 0);
        public bool Visible => body != null && body.enabled;

        public void SetVisible(bool v)
        {
            EnsureVisuals();
            body.enabled = head.enabled = eye.enabled = v;
        }

        public void SetFacing(string dir)
        {
            EnsureVisuals();
            facing = dir == "left" ? -1 : 1;
            eye.transform.localPosition = new Vector3(0.1f * facing, 1.36f, 0);
        }

        public void FaceToward(float destX)
        {
            SetFacing(destX < transform.position.x ? "left" : "right");
        }

        public void TeleportTo(C98_Anchor anchor)
        {
            StopMove();
            transform.position = Snap(anchor.transform.position);
        }

        public void MoveTo(C98_Anchor anchor, float duration, MonoBehaviour host)
        {
            StopMove();
            moving = host.StartCoroutine(MoveCo(anchor.transform.position, duration));
        }

        IEnumerator MoveCo(Vector3 dest, float duration)
        {
            FaceToward(dest.x);
            Vector3 from = transform.position;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = duration <= 0 ? 1 : Mathf.Clamp01(t / duration);
                transform.position = Snap(Vector3.Lerp(from, dest, k));
                // 걷는 느낌: 이동 중 몸통 상하 미세 진동
                body.transform.localPosition = new Vector3(0, 0.55f + Mathf.Abs(Mathf.Sin(t * 14f)) * 0.05f, 0);
                yield return null;
            }
            transform.position = Snap(dest);
            body.transform.localPosition = new Vector3(0, 0.55f, 0);
            moving = null;
        }

        public void StopMove()
        {
            if (moving != null) { StopCoroutine(moving); moving = null; }
            if (body != null) body.transform.localPosition = new Vector3(0, 0.55f, 0);
        }

        public void SetExpression(string expr, MonoBehaviour host)
        {
            Color tint;
            switch (expr)
            {
                case "alert": tint = new Color(1f, 0.9f, 0.4f); break;
                case "angry": tint = new Color(1f, 0.35f, 0.3f); break;
                case "fear": tint = new Color(0.55f, 0.6f, 1f); break;
                case "relief": tint = new Color(0.5f, 1f, 0.6f); break;
                default: tint = bodyColor; break;
            }
            if (pulsing != null) host.StopCoroutine(pulsing);
            pulsing = host.StartCoroutine(PulseCo(tint));
        }

        IEnumerator PulseCo(Color tint)
        {
            float t = 0;
            while (t < 0.55f)
            {
                t += Time.deltaTime;
                float k = Mathf.PingPong(t * 4f, 1f);
                head.color = Color.Lerp(bodyColor * 1.15f, tint, k);
                yield return null;
            }
            head.color = Color.Lerp(bodyColor * 1.15f, tint, 0.55f);
            pulsing = null;
        }

        // 픽셀아트 떨림 방지 자리 — 프로토타입은 1/32 단위 스냅으로 대신한다
        public static Vector3 Snap(Vector3 p)
        {
            const float ppu = 32f;
            return new Vector3(Mathf.Round(p.x * ppu) / ppu, Mathf.Round(p.y * ppu) / ppu, p.z);
        }
    }

    // ───────────────────────── 오브젝트 ─────────────────────────

    public class C98_SceneObject : MonoBehaviour
    {
        Coroutine moving;

        public void SetActiveState(bool v) => gameObject.SetActive(v);

        // payload 예: 문 열림 = 위로 슬라이드
        public void Slide(Vector2 offset, float duration, MonoBehaviour host)
        {
            if (moving != null) host.StopCoroutine(moving);
            moving = host.StartCoroutine(SlideCo(offset, duration));
        }

        public void SlideImmediate(Vector2 offset)
        {
            transform.localPosition += (Vector3)offset;
        }

        IEnumerator SlideCo(Vector2 offset, float duration)
        {
            Vector3 from = transform.localPosition, to = from + (Vector3)offset;
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                transform.localPosition = Vector3.Lerp(from, to, Mathf.Clamp01(t / duration));
                yield return null;
            }
            transform.localPosition = to;
            moving = null;
        }
    }

    // ───────────────────────── 무대(연구소) 구성 ─────────────────────────

    public static class C98_StageBuilder
    {
        // 연구소 무대를 코드로 짓는다. 배우·오브젝트·앵커에는 C98_Id 표식을 붙이고,
        // 레지스트리는 나중에 BuildFromScene 스캔으로 구성한다 (런타임·에디터 공용).
        public static Transform BuildLab(Transform parent, C98_Bundle bundle)
        {
            var root = new GameObject("C98_Stage").transform;
            if (parent != null) root.SetParent(parent, false);
            root.gameObject.AddComponent<C98_StageRoot>();

            var wallC = new Color(0.10f, 0.12f, 0.16f);
            var wallPanel = new Color(0.13f, 0.16f, 0.21f);
            var floorC = new Color(0.07f, 0.08f, 0.10f);
            var lineC = new Color(0.2f, 0.55f, 0.55f, 0.5f);

            void Tag(Component c, string id) => c.gameObject.AddComponent<C98_Id>().id = id;

            C98_Sprites.Rect(root, "bg_wall", wallC, new Vector2(0, 1.6f), new Vector2(24, 6.2f), 0);
            C98_Sprites.Rect(root, "bg_floor", floorC, new Vector2(0, -1.9f), new Vector2(24, 1.8f), 1);
            for (int i = -5; i <= 5; i++)
                C98_Sprites.Rect(root, $"wall_panel_{i}", wallPanel, new Vector2(i * 2.2f, 1.7f), new Vector2(1.9f, 5.6f), 2);
            C98_Sprites.Rect(root, "floor_line", lineC, new Vector2(0, -1.02f), new Vector2(24, 0.05f), 3);

            // 실험 장비 실루엣
            C98_Sprites.Rect(root, "tank_1", new Color(0.15f, 0.28f, 0.3f), new Vector2(-2.4f, 0.1f), new Vector2(1.0f, 2.2f), 5);
            C98_Sprites.Rect(root, "tank_2", new Color(0.15f, 0.28f, 0.3f), new Vector2(-3.6f, -0.1f), new Vector2(0.8f, 1.8f), 5);
            C98_Sprites.Rect(root, "console", new Color(0.2f, 0.2f, 0.26f), new Vector2(2.1f, -0.55f), new Vector2(1.6f, 0.9f), 5);

            // 문(왼쪽) — 컷씬에서 위로 열린다
            var doorGo = new GameObject("obj_lab_door");
            doorGo.transform.SetParent(root, false);
            doorGo.transform.localPosition = new Vector3(-6.4f, 0.35f, 0);
            C98_Sprites.Rect(doorGo.transform, "door_frame", new Color(0.28f, 0.3f, 0.36f), Vector2.zero, new Vector2(1.5f, 3.4f), 4);
            C98_Sprites.Rect(doorGo.transform, "door_slab", new Color(0.42f, 0.46f, 0.55f), Vector2.zero, new Vector2(1.2f, 3.1f), 6);
            Tag(doorGo.AddComponent<C98_SceneObject>(), "lab_door");

            // 환풍구(오른쪽 아래) — 숨는 분기에서 덮개가 떨어진다
            var ventGo = new GameObject("obj_vent");
            ventGo.transform.SetParent(root, false);
            ventGo.transform.localPosition = new Vector3(4.9f, -0.62f, 0);
            C98_Sprites.Rect(ventGo.transform, "vent_cover", new Color(0.35f, 0.4f, 0.45f), Vector2.zero, new Vector2(0.9f, 0.7f), 6);
            for (int i = 0; i < 3; i++)
                C98_Sprites.Rect(ventGo.transform, $"vent_slit_{i}", new Color(0.1f, 0.12f, 0.14f),
                    new Vector2(0, -0.18f + i * 0.18f), new Vector2(0.7f, 0.06f), 7);
            Tag(ventGo.AddComponent<C98_SceneObject>(), "vent_cover");

            // 비상구 표지(오른쪽 끝)
            var exitGo = new GameObject("obj_exit_sign");
            exitGo.transform.SetParent(root, false);
            exitGo.transform.localPosition = new Vector3(6.6f, 2.2f, 0);
            C98_Sprites.Rect(exitGo.transform, "sign", new Color(0.15f, 0.65f, 0.3f), Vector2.zero, new Vector2(0.9f, 0.4f), 6);
            Tag(exitGo.AddComponent<C98_SceneObject>(), "exit_sign");

            // 경보등 2개
            var lamp1 = C98_Sprites.Rect(root, "alarm_lamp_1", new Color(0.5f, 0.12f, 0.12f), new Vector2(-4.4f, 3.4f), new Vector2(0.5f, 0.22f), 6);
            var lamp2 = C98_Sprites.Rect(root, "alarm_lamp_2", new Color(0.5f, 0.12f, 0.12f), new Vector2(4.4f, 3.4f), new Vector2(0.5f, 0.22f), 6);
            Tag(lamp1, "alarm_lamp_1");
            Tag(lamp2, "alarm_lamp_2");

            // 조명 프리셋 쿼드 — 무대 전체를 덮는 틴트 (Timeline 조명 클립의 미리보기 대상)
            var quad = C98_Sprites.Rect(root, "light_quad", Color.clear, new Vector2(0, 0.6f), new Vector2(26, 10), 60);
            quad.gameObject.AddComponent<C98_LightQuad>();
            Tag(quad, "light_quad");

            // 앵커 — 좌표의 유일한 소유자
            void Anchor(string id, float x, float y)
            {
                var go = new GameObject($"anchor_{id}");
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3(x, y, 0);
                Tag(go.AddComponent<C98_Anchor>(), id);
            }
            Anchor("a_lab_door", -5.4f, -1.0f);
            Anchor("a_lab_center", -1.2f, -1.0f);
            Anchor("a_luna_start", 1.6f, -1.0f);
            Anchor("a_vent", 4.7f, -1.0f);
            Anchor("a_lab_exit", 6.3f, -1.0f);
            Anchor("a_offscreen_left", -8.5f, -1.0f);
            Anchor("a_offscreen_right", 8.5f, -1.0f);
            Anchor("cam_home", 0f, 0.6f);
            Anchor("cam_door", -3.5f, 0.4f);
            Anchor("cam_luna", 1.6f, 0.1f);
            Anchor("cam_exit", 5.2f, 0.3f);

            // 배우 — characters 데이터 기반 생성, 초기 배치는 무대가 결정
            foreach (var def in bundle.characters)
            {
                var actor = C98_Actor.Create(root, def);
                Tag(actor, def.id);
            }

            ResetToDefaults(root);
            return root;
        }

        // 무대 초기 배치 — 에디터 미리보기로 어질러진 상태 복원에도 쓴다
        public static void ResetToDefaults(Transform root)
        {
            C98_Actor FindActor(string id)
            {
                foreach (var a in root.GetComponentsInChildren<C98_Actor>(true))
                    if (a.id == id) return a;
                return null;
            }
            C98_Anchor FindAnchor(string id)
            {
                foreach (var t in root.GetComponentsInChildren<C98_Id>(true))
                    if (t.id == id && t.GetComponent<C98_Anchor>() != null) return t.GetComponent<C98_Anchor>();
                return null;
            }

            var luna = FindActor("luna");
            if (luna != null)
            {
                var a = FindAnchor("a_luna_start");
                if (a != null) luna.TeleportTo(a);
                luna.SetFacing("left");
                luna.SetVisible(true);
            }
            var hound = FindActor("hound");
            if (hound != null)
            {
                var a = FindAnchor("a_offscreen_left");
                if (a != null) hound.TeleportTo(a);
                hound.SetVisible(false);
            }
            var announcer = FindActor("lab_announcer");
            if (announcer != null) announcer.SetVisible(false); // 방송 전용 — 화면에 없음

            // 오브젝트 원위치
            foreach (var t in root.GetComponentsInChildren<C98_Id>(true))
            {
                if (t.id == "lab_door") t.transform.localPosition = new Vector3(-6.4f, 0.35f, 0);
                if (t.id == "vent_cover") t.transform.localPosition = new Vector3(4.9f, -0.62f, 0);
            }
            var quad = root.GetComponentsInChildren<C98_LightQuad>(true);
            foreach (var q in quad) q.Apply("light_default");
        }
    }
}
