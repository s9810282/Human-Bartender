// 98.Claude 컷씬 프로토타입 — 카메라 연출 + 화면 효과(페이드·플래시·조명 프리셋) + 사운드 스텁
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Claude98
{
    // ───────────────────────── 카메라 ─────────────────────────

    // 실전은 Cinemachine Track — 프로토타입은 같은 계약(이동·줌·흔들림·시작 상태 복원)만 구현.
    public class C98_CameraDirector : MonoBehaviour
    {
        Camera cam;
        Vector3 basePos;          // 흔들림 제외 기준 위치
        float shakeAmp, shakeUntil;
        Vector3 savedPos; float savedSize; bool hasSaved;
        Coroutine moving, zooming;

        public static C98_CameraDirector Create(Transform parent)
        {
            var go = new GameObject("C98_Camera");
            go.transform.SetParent(parent, false);
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 3.2f;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            go.transform.position = new Vector3(0, 0.6f, -10);
            go.AddComponent<AudioListener>();
            var d = go.AddComponent<C98_CameraDirector>();
            d.cam = cam;
            d.basePos = go.transform.position;
            return d;
        }

        // 씬에 편집용 무대와 함께 배치된 카메라를 그대로 채택한다
        public static C98_CameraDirector Adopt(Camera cam)
        {
            var go = cam.gameObject;
            if (go.GetComponent<AudioListener>() == null) go.AddComponent<AudioListener>();
            var d = go.GetComponent<C98_CameraDirector>();
            if (d == null) d = go.AddComponent<C98_CameraDirector>();
            d.cam = cam;
            d.basePos = go.transform.position;
            return d;
        }

        public Camera Cam => cam;

        // 컷씬 시작 전 상태 저장(temporary_state) → 종료 시 복원
        public void SaveState() { savedPos = basePos; savedSize = cam.orthographicSize; hasSaved = true; }
        public void RestoreState()
        {
            if (!hasSaved) return;
            StopAll();
            basePos = savedPos; cam.orthographicSize = savedSize;
            transform.position = basePos;
            hasSaved = false;
        }

        // 종료 연출용 — 저장 상태로 부드럽게 복귀(최종 확정은 RestoreState가 담당)
        public void RestoreSmooth(float duration)
        {
            if (!hasSaved) return;
            MoveTo(savedPos, duration);
            ZoomTo(savedSize, duration);
        }

        // 컷씬 진입 연출용 — 현재 크기 기준 비율 줌
        public void ZoomByFactor(float factor, float duration)
            => ZoomTo(cam.orthographicSize * factor, duration);

        public void MoveTo(Vector3 worldPos, float duration)
        {
            if (moving != null) StopCoroutine(moving);
            var dest = new Vector3(worldPos.x, worldPos.y, -10);
            moving = StartCoroutine(MoveCo(dest, Mathf.Max(0.01f, duration)));
        }

        IEnumerator MoveCo(Vector3 dest, float duration)
        {
            Vector3 from = basePos; float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration));
                basePos = Vector3.Lerp(from, dest, k);
                yield return null;
            }
            basePos = dest; moving = null;
        }

        public void ZoomTo(float size, float duration)
        {
            if (zooming != null) StopCoroutine(zooming);
            zooming = StartCoroutine(ZoomCo(Mathf.Max(0.5f, size), Mathf.Max(0.01f, duration)));
        }

        IEnumerator ZoomCo(float size, float duration)
        {
            float from = cam.orthographicSize, t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                cam.orthographicSize = Mathf.Lerp(from, size, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            cam.orthographicSize = size; zooming = null;
        }

        public void Shake(float amplitude, float duration)
        {
            shakeAmp = amplitude;
            shakeUntil = Time.time + duration;
        }

        public void StopAll()
        {
            if (moving != null) { StopCoroutine(moving); moving = null; }
            if (zooming != null) { StopCoroutine(zooming); zooming = null; }
            shakeUntil = 0;
        }

        void LateUpdate()
        {
            Vector3 offset = Vector3.zero;
            if (Time.time < shakeUntil)
            {
                float k = (shakeUntil - Time.time); // 잔여 시간 비례 감쇠
                offset = new Vector3(
                    (Mathf.PerlinNoise(Time.time * 30f, 0.3f) - 0.5f),
                    (Mathf.PerlinNoise(0.7f, Time.time * 30f) - 0.5f), 0) * shakeAmp * Mathf.Min(1f, k);
            }
            transform.position = basePos + offset;
        }
    }

    // ───────────────────────── 화면 효과 + 조명 프리셋 ─────────────────────────

    public class C98_ScreenFx : MonoBehaviour
    {
        public const float LetterboxMax = 0.10f; // 화면 위아래 각 10%

        Image fade;   // 검은 페이드
        Image flash;  // 흰 플래시
        Image barTop, barBottom; // 시네마틱 레터박스
        Coroutine fadeCo, flashCo, letterboxCo;
        public string CurrentLightPreset { get; private set; } = "light_default";

        // 현재 레터박스 비율(0~LetterboxMax) — 말풍선 화면 보정이 참조한다
        public static float LetterboxAmount { get; private set; }

        public static C98_ScreenFx Create(Transform parent)
        {
            // 조명 tint는 대사 UI(order 400) 아래, 페이드·플래시는 그 위 — 캔버스 2개로 분리
            var go = new GameObject("C98_ScreenFx");
            go.transform.SetParent(parent, false);
            var tintCanvas = go.AddComponent<Canvas>();
            tintCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            tintCanvas.sortingOrder = 300;
            var fx = go.AddComponent<C98_ScreenFx>();

            var topGo = new GameObject("C98_ScreenFx_Top");
            topGo.transform.SetParent(go.transform, false);
            var topCanvas = topGo.AddComponent<Canvas>();
            topCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            topCanvas.sortingOrder = 600;

            Image Layer(Transform host, string name)
            {
                var lg = new GameObject(name);
                lg.transform.SetParent(host, false);
                var img = lg.AddComponent<Image>();
                var rt = img.rectTransform;
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                img.raycastTarget = false;
                img.color = Color.clear;
                return img;
            }

            // 레터박스 — 대사 UI 위, 플래시·페이드 아래 (형제 순서로 결정)
            Image Bar(string name)
            {
                var bg = new GameObject(name);
                bg.transform.SetParent(topGo.transform, false);
                var img = bg.AddComponent<Image>();
                img.color = Color.black;
                img.raycastTarget = false;
                return img;
            }
            fx.barTop = Bar("letterbox_top");
            fx.barBottom = Bar("letterbox_bottom");
            fx.ApplyLetterbox(0f);

            fx.flash = Layer(topGo.transform, "flash");
            fx.fade = Layer(topGo.transform, "fade");
            LetterboxAmount = 0f;
            return fx;
        }

        // ───────────────────────── 레터박스 ─────────────────────────

        void ApplyLetterbox(float k)
        {
            LetterboxAmount = k;
            var rtTop = barTop.rectTransform;
            rtTop.anchorMin = new Vector2(0, 1f - k);
            rtTop.anchorMax = Vector2.one;
            rtTop.offsetMin = Vector2.zero; rtTop.offsetMax = Vector2.zero;
            var rtBottom = barBottom.rectTransform;
            rtBottom.anchorMin = Vector2.zero;
            rtBottom.anchorMax = new Vector2(1, k);
            rtBottom.offsetMin = Vector2.zero; rtBottom.offsetMax = Vector2.zero;
        }

        // 위 바는 위→아래, 아래 바는 아래→위로 동시에 차오른다
        public void ShowLetterbox(float duration) => RunLetterbox(LetterboxMax, duration);
        public void HideLetterbox(float duration) => RunLetterbox(0f, duration);

        void RunLetterbox(float target, float duration)
        {
            if (letterboxCo != null) StopCoroutine(letterboxCo);
            letterboxCo = StartCoroutine(LetterboxCo(target, Mathf.Max(0.01f, duration)));
        }

        IEnumerator LetterboxCo(float target, float duration)
        {
            float from = LetterboxAmount, t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                ApplyLetterbox(Mathf.Lerp(from, target, Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / duration))));
                yield return null;
            }
            ApplyLetterbox(target);
            letterboxCo = null;
        }

        public void FadeFromBlack(float duration) => RunFade(1f, 0f, duration);
        public void FadeToBlack(float duration) => RunFade(fade.color.a, 1f, duration);

        void RunFade(float from, float to, float duration)
        {
            if (fadeCo != null) StopCoroutine(fadeCo);
            fadeCo = StartCoroutine(FadeCo(from, to, Mathf.Max(0.01f, duration)));
        }

        IEnumerator FadeCo(float from, float to, float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, to, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, to);
            fadeCo = null;
        }

        public void Flash(float duration)
        {
            if (flashCo != null) StopCoroutine(flashCo);
            flashCo = StartCoroutine(FlashCo(Mathf.Max(0.05f, duration)));
        }

        IEnumerator FlashCo(float duration)
        {
            float t = 0;
            while (t < duration)
            {
                t += Time.deltaTime;
                flash.color = new Color(1, 1, 1, Mathf.Lerp(0.9f, 0f, Mathf.Clamp01(t / duration)));
                yield return null;
            }
            flash.color = Color.clear;
            flashCo = null;
        }

        // 조명 프리셋 — 값 하드코딩 대신 프리셋 이름으로만 전환 (문서 규칙).
        // 실체는 무대의 C98_LightQuad(월드 틴트) — Timeline 조명 클립이 미리보기로 쓰는 것과 동일 대상.
        public void SetLightPreset(string preset)
        {
            CurrentLightPreset = preset;
            var quad = C98_LightQuad.Instance;
            if (quad == null) quad = FindFirstObjectByType<C98_LightQuad>();
            if (quad != null) quad.Apply(preset);
            else Debug.LogWarning("[C98] C98_LightQuad 없음 — 조명 프리셋 무시");
        }

        // 스킵·종료 시 잔여 효과 정리 — 페이드는 end_state 쪽에서 명시적으로 결정
        public void ClearTransients()
        {
            if (flashCo != null) { StopCoroutine(flashCo); flashCo = null; }
            flash.color = Color.clear;
            if (fadeCo != null) { StopCoroutine(fadeCo); fadeCo = null; }
            fade.color = Color.clear;
        }
    }

    // ───────────────────────── 사운드 스텁 (절차 생성) ─────────────────────────

    // 발주 전 음원이 없으므로 짧은 톤을 코드로 만든다. 실전은 AudioManager + Timeline Audio Track.
    public class C98_AudioStub : MonoBehaviour
    {
        AudioSource src;
        AudioClip alarm, thud, ping, whoosh;
        public string CurrentBgm { get; private set; } = "";

        public static C98_AudioStub Create(Transform parent)
        {
            var go = new GameObject("C98_Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<C98_AudioStub>();
            a.src = go.AddComponent<AudioSource>();
            a.src.volume = 0.35f;
            a.alarm = Tone("c98_alarm", 1.2f, t => Mathf.Sin(2 * Mathf.PI * (t % 0.6f < 0.3f ? 880 : 660) * t) * Env(t % 0.3f, 0.3f));
            a.thud = Tone("c98_thud", 0.25f, t => Mathf.Sin(2 * Mathf.PI * 70 * t) * Env(t, 0.25f));
            a.ping = Tone("c98_ping", 0.2f, t => Mathf.Sin(2 * Mathf.PI * 1320 * t) * Env(t, 0.2f));
            a.whoosh = Tone("c98_whoosh", 0.35f, t => (Mathf.PerlinNoise(t * 90f, 0.5f) - 0.5f) * 2f * Env(t, 0.35f));
            return a;
        }

        static float Env(float t, float len) => Mathf.Clamp01(1f - t / len);

        static AudioClip Tone(string name, float length, System.Func<float, float> wave)
        {
            const int rate = 22050;
            int n = Mathf.CeilToInt(length * rate);
            var clip = AudioClip.Create(name, n, 1, rate, false);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = wave(i / (float)rate) * 0.5f;
            clip.SetData(data, 0);
            return clip;
        }

        public void PlaySfx(string id)
        {
            AudioClip c = id switch
            {
                "alarm" => alarm,
                "thud" => thud,
                "ping" => ping,
                "whoosh" => whoosh,
                _ => null
            };
            if (c == null) { Debug.LogWarning($"[C98] 알 수 없는 sfx: {id} — 무음 진행"); return; }
            src.PlayOneShot(c);
        }

        // BGM은 스텁 — id 기록만 하고 실제 재생은 없다 (end_state.bgm 계약 확인용)
        public void SetBgm(string id)
        {
            CurrentBgm = id ?? "";
            Debug.Log($"[C98] BGM → {(string.IsNullOrEmpty(id) ? "(이전 BGM 복구)" : id)}");
        }
    }
}
