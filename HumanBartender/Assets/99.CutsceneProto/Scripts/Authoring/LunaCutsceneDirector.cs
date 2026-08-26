using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ProjectLuna.CutscenePrototype.Authoring
{
    public enum LunaAuthoringCutsceneState
    {
        Idle,
        Playing,
        WaitingDialogue,
        Skipping,
        Completed,
        Failed
    }

    [RequireComponent(typeof(PlayableDirector))]
    public sealed class LunaCutsceneDirector : MonoBehaviour, INotificationReceiver
    {
        [SerializeField] private LunaCutsceneDefinition definition;
        [SerializeField] private PlayableDirector playableDirector;
        [SerializeField] private LunaCutsceneBindingRegistry bindingRegistry;
        [SerializeField] private LunaCutsceneDialogueUI dialogueUI;
        [SerializeField] private string locale = "ko";
        [SerializeField, Min(0.005f)] private float typeInterval = 0.025f;

        private readonly HashSet<string> flags = new(StringComparer.Ordinal);
        private readonly HashSet<string> firedEventKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> completedCutscenes = new(StringComparer.Ordinal);
        private readonly Dictionary<string, BindingSnapshot> initialState = new(StringComparer.Ordinal);
        private readonly List<IMarker> markerCache = new();
        private readonly HashSet<IMarker> firedMarkers = new();
        private readonly List<Marker> generatedRuntimeMarkers = new();

        private LunaDialogueMarker currentDialogue;
        private Coroutine autoAdvanceRoutine;
        private Coroutine shakeRoutine;
        private bool hasStarted;
        private bool completionHandled;

        public LunaAuthoringCutsceneState State { get; private set; } = LunaAuthoringCutsceneState.Idle;
        public string Locale => locale;
        public LunaCutsceneDefinition Definition => definition;
        public PlayableDirector Director => playableDirector;
        public LunaCutsceneBindingRegistry BindingRegistry => bindingRegistry;
        public LunaCutsceneDialogueUI DialogueUI => dialogueUI;
        public bool InputLocked => State == LunaAuthoringCutsceneState.Playing
                                   || State == LunaAuthoringCutsceneState.WaitingDialogue
                                   || State == LunaAuthoringCutsceneState.Skipping;

        public void Configure(
            LunaCutsceneDefinition cutsceneDefinition,
            PlayableDirector director,
            LunaCutsceneBindingRegistry registry,
            LunaCutsceneDialogueUI ui)
        {
            definition = cutsceneDefinition;
            playableDirector = director;
            bindingRegistry = registry;
            dialogueUI = ui;
        }

        private void Awake()
        {
            if (playableDirector == null)
                playableDirector = GetComponent<PlayableDirector>();
            if (bindingRegistry == null)
                bindingRegistry = GetComponent<LunaCutsceneBindingRegistry>();

            playableDirector.playOnAwake = false;
            playableDirector.extrapolationMode = DirectorWrapMode.Hold;
            bindingRegistry?.RebuildLookup();
            CaptureInitialState();
        }

        private void Start()
        {
            if (definition != null && definition.autoPlay)
                PlayFromStart();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool advance = (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                           || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (advance && State == LunaAuthoringCutsceneState.WaitingDialogue)
            {
                if (dialogueUI.IsTyping)
                    dialogueUI.RevealImmediately();
                else
                    ResumeAfterDialogue();
            }

            if (keyboard != null && keyboard.sKey.wasPressedThisFrame)
                Skip();

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                ResetAndReplay();

            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
                ToggleLocale();

            if (hasStarted && !completionHandled && State == LunaAuthoringCutsceneState.Playing)
                PollTimelineMarkers();

            if (hasStarted
                && !completionHandled
                && State == LunaAuthoringCutsceneState.Playing
                && playableDirector.duration > 0d
                && playableDirector.time >= playableDirector.duration - 0.02d)
            {
                Complete(false);
            }

            UpdateStatus();
        }

        public void PlayFromStart()
        {
            if (!ValidateForRuntime(out string error))
            {
                Fail(error);
                return;
            }

            if (definition.playMode == LunaCutscenePlayMode.Once && completedCutscenes.Contains(definition.cutsceneId))
            {
                State = LunaAuthoringCutsceneState.Completed;
                dialogueUI.SetStatus("이미 재생한 컷씬입니다. R로 테스트 상태를 초기화할 수 있습니다.");
                return;
            }

            if (!EvaluateCondition(definition.requiredWhen))
            {
                State = LunaAuthoringCutsceneState.Completed;
                dialogueUI.SetStatus("재생 조건을 만족하지 않았습니다.");
                return;
            }

            StopRuntimeEffects();
            RestoreInitialState();
            firedEventKeys.Clear();
            firedMarkers.Clear();
            CacheTimelineMarkers();
            completionHandled = false;
            hasStarted = true;
            currentDialogue = null;
            dialogueUI.HideDialogue();
            dialogueUI.SetFadeImmediate(definition.startFromBlack);
            playableDirector.time = 0d;
            playableDirector.Evaluate();
            playableDirector.Play();
            State = LunaAuthoringCutsceneState.Playing;
        }

        public void ResetAndReplay()
        {
            completedCutscenes.Clear();
            flags.Clear();
            PlayFromStart();
        }

        public void AdvanceForAutomation()
        {
            if (State != LunaAuthoringCutsceneState.WaitingDialogue)
                return;
            if (dialogueUI != null && dialogueUI.IsTyping)
                dialogueUI.RevealImmediately();
            else
                ResumeAfterDialogue();
        }

        public void SkipForAutomation()
        {
            Skip();
        }

        public bool HasFlag(string flagName)
        {
            return flags.Contains(NormalizeFlag(flagName));
        }

        public void Skip()
        {
            if (definition == null || !definition.skippable || !InputLocked)
                return;

            State = LunaAuthoringCutsceneState.Skipping;
            playableDirector.Pause();
            dialogueUI.HideDialogue();
            StopRuntimeEffects();
            FireRequiredSkipMarkers();
            ApplyEndState();
            playableDirector.Stop();
            Complete(true, false);
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (!Application.isPlaying || completionHandled)
                return;

            DispatchMarker(notification as IMarker);
        }

        private bool DispatchMarker(IMarker marker)
        {
            if (marker == null || firedMarkers.Contains(marker))
                return false;

            bool handled;
            switch (marker)
            {
                case LunaDialogueMarker dialogueMarker:
                    if (currentDialogue != null)
                        return false;
                    HandleDialogue(dialogueMarker);
                    handled = true;
                    break;
                case LunaEffectMarker effectMarker:
                    ExecuteEffect(effectMarker, false);
                    handled = true;
                    break;
                case LunaSpriteSwapMarker spriteMarker:
                    ExecuteSpriteSwap(spriteMarker);
                    handled = true;
                    break;
                default:
                    return false;
            }

            firedMarkers.Add(marker);
            Debug.Log($"[LunaCutsceneAuthoring] MARKER {marker.GetType().Name} at {marker.time:0.00}", this);
            return handled;
        }

        private void CacheTimelineMarkers()
        {
            CleanupGeneratedMarkers();
            markerCache.Clear();
            if (playableDirector.playableAsset is not TimelineAsset timeline || timeline.markerTrack == null)
                return;
            markerCache.AddRange(timeline.markerTrack.GetMarkers());
            if (markerCache.Count == 0 && definition != null && definition.bakedEvents != null)
            {
                foreach (LunaBakedCutsceneEvent bakedEvent in definition.bakedEvents)
                {
                    Marker marker = CreateRuntimeMarker(bakedEvent);
                    if (marker == null)
                        continue;
                    generatedRuntimeMarkers.Add(marker);
                    markerCache.Add(marker);
                }
            }
            markerCache.Sort((left, right) => left.time.CompareTo(right.time));
            Debug.Log($"[LunaCutsceneAuthoring] MARKER_CACHE count={markerCache.Count}, timeline={timeline.name}", this);
        }

        private Marker CreateRuntimeMarker(LunaBakedCutsceneEvent source)
        {
            if (source == null)
                return null;
            switch (source.kind)
            {
                case LunaBakedEventKind.Dialogue:
                {
                    LunaDialogueMarker marker = ScriptableObject.CreateInstance<LunaDialogueMarker>();
                    marker.hideFlags = HideFlags.HideAndDontSave;
                    marker.time = source.time;
                    marker.line = CloneLine(source.line);
                    marker.pauseTimeline = source.pauseTimeline;
                    marker.advanceMode = source.advanceMode;
                    marker.autoDelay = source.autoDelay;
                    marker.dialogueId = source.dialogueId;
                    return marker;
                }
                case LunaBakedEventKind.Effect:
                {
                    LunaEffectMarker marker = ScriptableObject.CreateInstance<LunaEffectMarker>();
                    marker.hideFlags = HideFlags.HideAndDontSave;
                    marker.time = source.time;
                    marker.effectType = source.effectType;
                    marker.targetId = source.targetId;
                    marker.stringValue = source.stringValue;
                    marker.color = source.color;
                    marker.duration = source.duration;
                    marker.strength = source.strength;
                    marker.audioClip = source.audioClip;
                    marker.fireOnSkip = source.fireOnSkip;
                    marker.eventKey = source.eventKey;
                    return marker;
                }
                case LunaBakedEventKind.SpriteSwap:
                {
                    LunaSpriteSwapMarker marker = ScriptableObject.CreateInstance<LunaSpriteSwapMarker>();
                    marker.hideFlags = HideFlags.HideAndDontSave;
                    marker.time = source.time;
                    marker.targetId = source.targetId;
                    marker.sprite = source.sprite;
                    marker.flipX = source.flipX;
                    marker.fireOnSkip = source.fireOnSkip;
                    marker.eventKey = source.eventKey;
                    return marker;
                }
                default:
                    return null;
            }
        }

        private static LunaLocalizedLine CloneLine(LunaLocalizedLine source)
        {
            if (source == null)
                return new LunaLocalizedLine();
            return new LunaLocalizedLine
            {
                speakerId = source.speakerId,
                speakerKo = source.speakerKo,
                speakerEn = source.speakerEn,
                textKo = source.textKo,
                textEn = source.textEn
            };
        }

        private void CleanupGeneratedMarkers()
        {
            foreach (Marker marker in generatedRuntimeMarkers)
            {
                if (marker != null)
                    Destroy(marker);
            }
            generatedRuntimeMarkers.Clear();
        }

        private void PollTimelineMarkers()
        {
            double currentTime = playableDirector.time + 0.0001d;
            foreach (IMarker marker in markerCache)
            {
                if (marker.time > currentTime)
                    break;
                if (firedMarkers.Contains(marker))
                    continue;
                if (!DispatchMarker(marker))
                    continue;
                if (State == LunaAuthoringCutsceneState.WaitingDialogue)
                    break;
            }
        }

        private void HandleDialogue(LunaDialogueMarker marker)
        {
            if (marker == null || currentDialogue != null)
                return;

            currentDialogue = marker;
            if (marker.pauseTimeline)
                playableDirector.Pause();
            dialogueUI.ShowDialogue(marker.line, IsEnglish(), typeInterval);
            State = LunaAuthoringCutsceneState.WaitingDialogue;

            if (marker.advanceMode == LunaDialogueAdvanceMode.Auto)
            {
                if (autoAdvanceRoutine != null)
                    StopCoroutine(autoAdvanceRoutine);
                autoAdvanceRoutine = StartCoroutine(AutoAdvance(marker.autoDelay));
            }
        }

        private IEnumerator AutoAdvance(float delay)
        {
            while (dialogueUI.IsTyping)
                yield return null;
            yield return new WaitForSecondsRealtime(Mathf.Max(0f, delay));
            ResumeAfterDialogue();
        }

        private void ResumeAfterDialogue()
        {
            if (currentDialogue == null)
                return;
            if (autoAdvanceRoutine != null)
            {
                StopCoroutine(autoAdvanceRoutine);
                autoAdvanceRoutine = null;
            }

            bool shouldResume = currentDialogue.pauseTimeline;
            currentDialogue = null;
            dialogueUI.HideDialogue();
            State = LunaAuthoringCutsceneState.Playing;
            if (shouldResume)
                playableDirector.Resume();
        }

        private void ExecuteEffect(LunaEffectMarker marker, bool immediate)
        {
            if (marker == null || IsEventAlreadyFired(marker.eventKey))
                return;

            switch (marker.effectType)
            {
                case LunaCutsceneEffectType.FadeIn:
                    if (immediate) dialogueUI.SetFadeImmediate(false);
                    else StartCoroutine(dialogueUI.Fade(false, Mathf.Max(0.01f, marker.duration)));
                    break;
                case LunaCutsceneEffectType.FadeOut:
                    if (immediate) dialogueUI.SetFadeImmediate(true);
                    else StartCoroutine(dialogueUI.Fade(true, Mathf.Max(0.01f, marker.duration)));
                    break;
                case LunaCutsceneEffectType.Flash:
                    if (!immediate) StartCoroutine(dialogueUI.Flash(marker.color, Mathf.Max(0.06f, marker.duration)));
                    break;
                case LunaCutsceneEffectType.CameraShake:
                    if (!immediate && Resolve(marker.targetId, out GameObject cameraTarget))
                    {
                        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
                        shakeRoutine = StartCoroutine(Shake(cameraTarget.transform, marker.duration, marker.strength));
                    }
                    break;
                case LunaCutsceneEffectType.SetActive:
                    if (Resolve(marker.targetId, out GameObject activeTarget))
                        activeTarget.SetActive(!string.Equals(marker.stringValue, "false", StringComparison.OrdinalIgnoreCase));
                    break;
                case LunaCutsceneEffectType.SetSpriteColor:
                    if (Resolve(marker.targetId, out GameObject colorTarget)
                        && colorTarget.TryGetComponent(out SpriteRenderer renderer))
                        renderer.color = marker.color;
                    break;
                case LunaCutsceneEffectType.PlaySfx:
                    PlaySfx(marker);
                    break;
                case LunaCutsceneEffectType.SetFlag:
                    ApplyFlag(marker.stringValue);
                    break;
            }

            MarkEventFired(marker.eventKey);
        }

        private void ExecuteSpriteSwap(LunaSpriteSwapMarker marker)
        {
            if (marker == null || IsEventAlreadyFired(marker.eventKey))
                return;
            if (Resolve(marker.targetId, out GameObject target)
                && target.TryGetComponent(out SpriteRenderer renderer))
            {
                renderer.sprite = marker.sprite;
                renderer.flipX = marker.flipX;
            }
            MarkEventFired(marker.eventKey);
        }

        private void FireRequiredSkipMarkers()
        {
            foreach (IMarker marker in markerCache)
            {
                if (marker.time + 0.0001d < playableDirector.time)
                    continue;
                if (marker is LunaEffectMarker effect && effect.fireOnSkip)
                    ExecuteEffect(effect, true);
                else if (marker is LunaSpriteSwapMarker sprite && sprite.fireOnSkip)
                    ExecuteSpriteSwap(sprite);
            }
        }

        private void ApplyEndState()
        {
            if (definition.endBindings != null)
            {
                foreach (LunaCutsceneEndBinding endBinding in definition.endBindings)
                {
                    if (endBinding == null || !Resolve(endBinding.targetId, out GameObject target))
                        continue;
                    if (endBinding.applyActive)
                        target.SetActive(endBinding.active);
                    if (endBinding.applyPosition)
                        target.transform.localPosition = endBinding.localPosition;
                    if (endBinding.applySprite && target.TryGetComponent(out SpriteRenderer renderer))
                        renderer.sprite = endBinding.sprite;
                }
            }

            if (!string.IsNullOrWhiteSpace(definition.completionFlag))
                flags.Add(NormalizeFlag(definition.completionFlag));
            if (definition.fadeToBlackOnEnd)
                dialogueUI.SetFadeImmediate(true);
        }

        private void Complete(bool skipped, bool applyEndState = true)
        {
            if (completionHandled)
                return;
            completionHandled = true;
            if (applyEndState)
                ApplyEndState();
            dialogueUI.HideDialogue();
            completedCutscenes.Add(definition.cutsceneId);
            State = LunaAuthoringCutsceneState.Completed;
            dialogueUI.SetStatus(skipped ? "SKIPPED / END STATE APPLIED" : "COMPLETE");
            Debug.Log($"[LunaCutsceneAuthoring] COMPLETE id={definition.cutsceneId}, skipped={skipped}", this);
        }

        private IEnumerator Shake(Transform target, float duration, float strength)
        {
            Vector3 origin = target.localPosition;
            float elapsed = 0f;
            while (elapsed < duration && State != LunaAuthoringCutsceneState.Skipping)
            {
                elapsed += Time.unscaledDeltaTime;
                float damping = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                target.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * strength * damping);
                yield return null;
            }
            target.localPosition = origin;
            shakeRoutine = null;
        }

        private void PlaySfx(LunaEffectMarker marker)
        {
            if (marker.audioClip == null)
                return;
            if (Resolve(marker.targetId, out GameObject target) && target.TryGetComponent(out AudioSource source))
                source.PlayOneShot(marker.audioClip);
            else
                AudioSource.PlayClipAtPoint(marker.audioClip, Vector3.zero);
        }

        private void CaptureInitialState()
        {
            initialState.Clear();
            if (bindingRegistry == null)
                return;
            foreach (LunaCutsceneBindingEntry entry in bindingRegistry.Bindings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.target == null)
                    continue;
                SpriteRenderer renderer = entry.target.GetComponent<SpriteRenderer>();
                initialState[entry.id] = new BindingSnapshot
                {
                    active = entry.target.activeSelf,
                    localPosition = entry.target.transform.localPosition,
                    localRotation = entry.target.transform.localRotation,
                    localScale = entry.target.transform.localScale,
                    sprite = renderer != null ? renderer.sprite : null,
                    color = renderer != null ? renderer.color : Color.white,
                    flipX = renderer != null && renderer.flipX
                };
            }
        }

        private void RestoreInitialState()
        {
            foreach (KeyValuePair<string, BindingSnapshot> pair in initialState)
            {
                if (!Resolve(pair.Key, out GameObject target))
                    continue;
                BindingSnapshot snapshot = pair.Value;
                target.SetActive(snapshot.active);
                target.transform.localPosition = snapshot.localPosition;
                target.transform.localRotation = snapshot.localRotation;
                target.transform.localScale = snapshot.localScale;
                if (target.TryGetComponent(out SpriteRenderer renderer))
                {
                    renderer.sprite = snapshot.sprite;
                    renderer.color = snapshot.color;
                    renderer.flipX = snapshot.flipX;
                }
            }
        }

        private void StopRuntimeEffects()
        {
            if (autoAdvanceRoutine != null) StopCoroutine(autoAdvanceRoutine);
            if (shakeRoutine != null) StopCoroutine(shakeRoutine);
            autoAdvanceRoutine = null;
            shakeRoutine = null;
            dialogueUI?.ClearTransientEffects();
        }

        private bool ValidateForRuntime(out string error)
        {
            error = null;
            if (definition == null) error = "LunaCutsceneDefinition이 연결되지 않았습니다.";
            else if (playableDirector == null) error = "PlayableDirector가 연결되지 않았습니다.";
            else if (playableDirector.playableAsset is not TimelineAsset) error = "실제 TimelineAsset이 연결되지 않았습니다.";
            else if (bindingRegistry == null) error = "Binding Registry가 연결되지 않았습니다.";
            else if (dialogueUI == null) error = "Dialogue UI가 연결되지 않았습니다.";
            return error == null;
        }

        private void OnDestroy()
        {
            CleanupGeneratedMarkers();
        }

        private void Fail(string error)
        {
            State = LunaAuthoringCutsceneState.Failed;
            completionHandled = true;
            dialogueUI?.SetStatus($"DATA / BINDING ERROR\n{error}");
            Debug.LogError($"[LunaCutsceneAuthoring] {error}", this);
        }

        private void ToggleLocale()
        {
            locale = IsEnglish() ? "ko" : "en";
            if (currentDialogue != null)
                dialogueUI.ShowDialogue(currentDialogue.line, IsEnglish(), typeInterval);
        }

        private void UpdateStatus()
        {
            if (dialogueUI == null || definition == null)
                return;
            if (State == LunaAuthoringCutsceneState.WaitingDialogue)
                return;
            dialogueUI.SetStatus($"{definition.cutsceneId}\n{State}  {playableDirector.time:0.00} / {playableDirector.duration:0.00}\nSPACE 대사 · S 스킵 · R 재시작 · L 언어");
        }

        private bool Resolve(string id, out GameObject target)
        {
            target = null;
            return !string.IsNullOrWhiteSpace(id) && bindingRegistry != null && bindingRegistry.TryGet(id, out target);
        }

        private bool EvaluateCondition(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return true;
            string value = expression.Trim();
            bool negate = value.StartsWith("!", StringComparison.Ordinal);
            if (negate) value = value.Substring(1).Trim();
            string flag = NormalizeFlag(value);
            bool exists = flags.Contains(flag);
            return negate ? !exists : exists;
        }

        private void ApplyFlag(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return;
            string[] pair = expression.Split('=');
            string key = NormalizeFlag(pair[0]);
            bool enabled = pair.Length < 2 || !string.Equals(pair[1].Trim(), "false", StringComparison.OrdinalIgnoreCase);
            if (enabled) flags.Add(key);
            else flags.Remove(key);
        }

        private static string NormalizeFlag(string value)
        {
            string trimmed = value?.Trim() ?? string.Empty;
            return trimmed.StartsWith("flag.", StringComparison.Ordinal) ? trimmed.Substring(5) : trimmed;
        }

        private bool IsEnglish()
        {
            return string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsEventAlreadyFired(string eventKey)
        {
            return !string.IsNullOrWhiteSpace(eventKey) && firedEventKeys.Contains(eventKey);
        }

        private void MarkEventFired(string eventKey)
        {
            if (!string.IsNullOrWhiteSpace(eventKey))
                firedEventKeys.Add(eventKey);
        }

        private sealed class BindingSnapshot
        {
            public bool active;
            public Vector3 localPosition;
            public Quaternion localRotation;
            public Vector3 localScale;
            public Sprite sprite;
            public Color color;
            public bool flipX;
        }
    }
}
