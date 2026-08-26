using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public sealed class LunaCutsceneDialogueUI : MonoBehaviour
    {
        [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private Text speakerText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Text continueHint;
        [SerializeField] private Image fadeOverlay;
        [SerializeField] private Image flashOverlay;
        [SerializeField] private Text statusText;

        private Coroutine typingRoutine;
        private string fullText = string.Empty;

        public bool IsTyping { get; private set; }
        public bool IsVisible => dialoguePanel != null && dialoguePanel.activeSelf;

        public void Configure(
            GameObject panel,
            Text speaker,
            Text body,
            Text hint,
            Image fade,
            Image flash,
            Text status)
        {
            dialoguePanel = panel;
            speakerText = speaker;
            bodyText = body;
            continueHint = hint;
            fadeOverlay = fade;
            flashOverlay = flash;
            statusText = status;
        }

        private void Awake()
        {
            HideDialogue();
            SetOverlayAlpha(flashOverlay, 0f);
        }

        public void ShowDialogue(LunaLocalizedLine line, bool english, float typeInterval)
        {
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);

            fullText = english ? line.textEn : line.textKo;
            speakerText.text = english ? line.speakerEn : line.speakerKo;
            bodyText.text = string.Empty;
            continueHint.text = english ? "SPACE / CLICK  NEXT" : "SPACE / 클릭  다음";
            dialoguePanel.SetActive(true);
            typingRoutine = StartCoroutine(TypeText(Mathf.Max(0.005f, typeInterval)));
        }

        public void RevealImmediately()
        {
            if (!IsTyping)
                return;
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            typingRoutine = null;
            bodyText.text = fullText;
            IsTyping = false;
        }

        public void HideDialogue()
        {
            if (typingRoutine != null)
                StopCoroutine(typingRoutine);
            typingRoutine = null;
            IsTyping = false;
            if (dialoguePanel != null)
                dialoguePanel.SetActive(false);
        }

        public void SetStatus(string value)
        {
            if (statusText != null)
                statusText.text = value;
        }

        public IEnumerator Fade(bool toBlack, float duration)
        {
            float from = fadeOverlay != null ? fadeOverlay.color.a : 0f;
            float to = toBlack ? 1f : 0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetOverlayAlpha(fadeOverlay, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
                yield return null;
            }
            SetOverlayAlpha(fadeOverlay, to);
        }

        public IEnumerator Flash(Color color, float duration)
        {
            if (flashOverlay == null)
                yield break;

            Color baseColor = color;
            float half = Mathf.Max(0.03f, duration * 0.5f);
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                baseColor.a = Mathf.Lerp(0f, 0.9f, Mathf.Clamp01(elapsed / half));
                flashOverlay.color = baseColor;
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                baseColor.a = Mathf.Lerp(0.9f, 0f, Mathf.Clamp01(elapsed / half));
                flashOverlay.color = baseColor;
                yield return null;
            }
            SetOverlayAlpha(flashOverlay, 0f);
        }

        public void SetFadeImmediate(bool black)
        {
            SetOverlayAlpha(fadeOverlay, black ? 1f : 0f);
        }

        public void ClearTransientEffects()
        {
            StopAllCoroutines();
            typingRoutine = null;
            IsTyping = false;
            SetOverlayAlpha(flashOverlay, 0f);
        }

        private IEnumerator TypeText(float interval)
        {
            IsTyping = true;
            for (int index = 0; index < fullText.Length; index++)
            {
                bodyText.text = fullText.Substring(0, index + 1);
                yield return new WaitForSecondsRealtime(interval);
            }
            bodyText.text = fullText;
            IsTyping = false;
            typingRoutine = null;
        }

        private static void SetOverlayAlpha(Image image, float alpha)
        {
            if (image == null)
                return;
            Color color = image.color;
            color.a = alpha;
            image.color = color;
            image.raycastTarget = alpha > 0.001f;
        }
    }
}
