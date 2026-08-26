using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

namespace ProjectLuna.CutscenePrototype
{
    [Serializable]
    public sealed class PrototypeTimelineEntry
    {
        public string key;
        public PrototypeTimelineAsset asset;
    }

    [RequireComponent(typeof(PlayableDirector))]
    [RequireComponent(typeof(PrototypeCutsceneView))]
    public sealed class PrototypeCutsceneRunner : MonoBehaviour
    {
        [SerializeField] private TextAsset cutsceneJson;
        [SerializeField] private PrototypeTimelineEntry[] timelines;
        [SerializeField] private string locale = "ko";
        [SerializeField, Min(0.005f)] private float typeInterval = 0.025f;
        [SerializeField] private bool autoPlay = true;

        private readonly Dictionary<string, PrototypeTimelineAsset> timelineByKey = new(StringComparer.Ordinal);
        private readonly Dictionary<int, int> stepIndexBySequence = new();
        private readonly Dictionary<string, PrototypeChoiceDefinition> choiceById = new(StringComparer.Ordinal);
        private readonly HashSet<string> flags = new(StringComparer.Ordinal);
        private readonly HashSet<string> firedEventKeys = new(StringComparer.Ordinal);
        private readonly HashSet<string> completedCutscenes = new(StringComparer.Ordinal);

        private PlayableDirector director;
        private PrototypeCutsceneView view;
        private PrototypeCutsceneData data;
        private Coroutine playbackRoutine;
        private bool advancePressed;
        private bool revealPressed;
        private bool dialogueTyping;
        private bool skipRequested;
        private bool debugVisible;
        private int selectedChoice = -1;
        private int firstEnabledChoice = -1;
        private int nextSequenceOverride;
        private int currentStepIndex = -1;
        private string currentFullDialogue;
        private PrototypeCutsceneStep currentDialogueStep;
        private PrototypeChoiceDefinition currentChoice;
        private bool[] currentChoiceEnabled;

        public PrototypeCutsceneState State { get; private set; } = PrototypeCutsceneState.Idle;
        public int CurrentSequence { get; private set; }
        public string Locale => locale;
        public bool IsRunning => playbackRoutine != null;
        public bool InputLocked => IsRunning && State != PrototypeCutsceneState.Completed && State != PrototypeCutsceneState.Failed;
        public bool LastCompletionWasSkipped { get; private set; }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrWhiteSpace(flag) && flags.Contains(flag);
        }

        public void AdvanceForAutomation()
        {
            if (State == PrototypeCutsceneState.ShowingDialogue)
            {
                if (dialogueTyping)
                    revealPressed = true;
                else
                    advancePressed = true;
                return;
            }

            if (State == PrototypeCutsceneState.ShowingChoice && firstEnabledChoice >= 0)
                selectedChoice = firstEnabledChoice;
        }

        public void SelectChoiceForAutomation(int index)
        {
            if (State == PrototypeCutsceneState.ShowingChoice)
                SelectChoice(index);
        }

        public void SkipForAutomation()
        {
            RequestSkip();
        }

        public void Configure(TextAsset json, PrototypeTimelineEntry[] entries, bool shouldAutoPlay)
        {
            cutsceneJson = json;
            timelines = entries;
            autoPlay = shouldAutoPlay;
        }

        private void Awake()
        {
            director = GetComponent<PlayableDirector>();
            view = GetComponent<PrototypeCutsceneView>();
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            view.ChoiceSelected += OnChoiceSelected;
            BuildTimelineLookup();
        }

        private void OnDestroy()
        {
            if (view != null)
                view.ChoiceSelected -= OnChoiceSelected;
        }

        private void Start()
        {
            if (autoPlay)
                PlayFromStart();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool next = (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                        || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (next && State == PrototypeCutsceneState.ShowingDialogue)
            {
                if (dialogueTyping)
                    revealPressed = true;
                else
                    advancePressed = true;
            }

            if (State == PrototypeCutsceneState.ShowingChoice && keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) SelectChoice(0);
                if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) SelectChoice(1);
                if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) SelectChoice(2);
                if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame) SelectChoice(3);
            }

            if (keyboard != null && keyboard.sKey.wasPressedThisFrame)
                RequestSkip();

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                ResetAndReplay();

            if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
                ToggleLocale();

            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame)
            {
                debugVisible = !debugVisible;
                view.SetDebugVisible(debugVisible);
            }

            if (debugVisible)
                view.SetDebugBody(BuildDebugText());
        }

        public void PlayFromStart()
        {
            StopPlayback();
            view.ResetStage();
            CurrentSequence = 0;
            currentStepIndex = -1;
            firedEventKeys.Clear();
            skipRequested = false;
            LastCompletionWasSkipped = false;
            playbackRoutine = StartCoroutine(RunCutscene());
        }

        public void ResetAndReplay()
        {
            completedCutscenes.Clear();
            flags.Clear();
            PlayFromStart();
        }

        public void StopPlayback()
        {
            if (playbackRoutine != null)
                StopCoroutine(playbackRoutine);
            playbackRoutine = null;
            if (director != null)
            {
                director.Stop();
                director.playableAsset = null;
            }
            advancePressed = false;
            revealPressed = false;
            dialogueTyping = false;
            skipRequested = false;
            selectedChoice = -1;
            currentChoice = null;
            currentDialogueStep = null;
            view?.HideDialogue();
            view?.HideChoice();
            view?.ClearTransientEffects();
            SetState(PrototypeCutsceneState.Idle, "재생 중지");
        }

        private IEnumerator RunCutscene()
        {
            SetState(PrototypeCutsceneState.Preparing, "JSON·바인딩 검증");
            yield return null;

            if (!CutscenePrototypeLoader.TryLoad(cutsceneJson, out data, out string error))
            {
                Fail(error);
                yield break;
            }

            BuildRuntimeLookups();
            if (!ValidateRuntimeBindings(out error))
            {
                Fail(error);
                yield break;
            }

            if (!EvaluateCondition(data.required_when, out bool canPlay, out error))
            {
                Fail(error);
                yield break;
            }

            if (!canPlay)
            {
                CompleteWithoutPlayback("재생 조건 불충족");
                yield break;
            }

            if (data.play_mode == "once" && completedCutscenes.Contains(data.cutscene_id))
            {
                CompleteWithoutPlayback("이미 재생된 once 컷씬 · R로 초기화");
                yield break;
            }

            RefreshHeader();
            int index = 0;
            while (index >= 0 && index < data.steps.Length)
            {
                currentStepIndex = index;
                PrototypeCutsceneStep step = data.steps[index];
                CurrentSequence = step.seq;
                nextSequenceOverride = 0;

                if (skipRequested)
                {
                    yield return SkipAndComplete();
                    yield break;
                }

                bool stepEnabled = true;
                if (step.type != "branch" && !EvaluateCondition(step.when, out stepEnabled, out error))
                {
                    Fail($"seq {step.seq}: {error}");
                    yield break;
                }

                if (stepEnabled)
                {
                    view.SetStatus(State.ToString().ToUpperInvariant(), $"SEQ {step.seq:000}  /  {step.type.ToUpperInvariant()}");
                    IEnumerator execution = ExecuteStep(step);
                    while (true)
                    {
                        bool moved;
                        try
                        {
                            moved = execution.MoveNext();
                        }
                        catch (Exception exception)
                        {
                            Fail($"seq {step.seq} 실행 오류: {exception.Message}");
                            yield break;
                        }

                        if (!moved)
                            break;
                        yield return execution.Current;
                    }

                    if (State == PrototypeCutsceneState.Failed)
                        yield break;

                    if (skipRequested)
                    {
                        yield return SkipAndComplete();
                        yield break;
                    }

                    if (!ApplyEffects(step.effects, out error))
                    {
                        Fail($"seq {step.seq} effects 오류: {error}");
                        yield break;
                    }
                }

                int requestedSequence = nextSequenceOverride != 0 ? nextSequenceOverride : step.next_seq;
                if (requestedSequence != 0)
                    index = stepIndexBySequence[requestedSequence];
                else
                    index++;
            }

            yield return CompleteCutscene(false);
        }

        private IEnumerator ExecuteStep(PrototypeCutsceneStep step)
        {
            switch (step.type)
            {
                case "timeline":
                    yield return PlayTimeline(step.timeline_key);
                    break;
                case "dialogue":
                    yield return PlayDialogue(step);
                    break;
                case "choice":
                    yield return PlayChoice(step);
                    break;
                case "branch":
                    if (!EvaluateCondition(step.when, out bool branchResult, out string branchError))
                    {
                        Fail($"seq {step.seq} branch 오류: {branchError}");
                        yield break;
                    }
                    nextSequenceOverride = branchResult ? step.true_seq : step.false_seq;
                    break;
                case "command":
                    yield return ExecuteCommand(step, false);
                    break;
                case "wait":
                    yield return WaitSkippable(Mathf.Max(0f, step.duration));
                    break;
            }
        }

        private IEnumerator PlayTimeline(string timelineKey)
        {
            if (!timelineByKey.TryGetValue(timelineKey, out PrototypeTimelineAsset asset) || asset == null)
            {
                Fail($"timeline_key를 찾을 수 없습니다: {timelineKey}");
                yield break;
            }

            SetState(PrototypeCutsceneState.PlayingTimeline, $"TIMELINE  {timelineKey}");
            director.playableAsset = asset;
            director.time = 0d;
            director.Play();

            while (director.state == PlayState.Playing && !skipRequested)
                yield return null;

            if (skipRequested)
                director.Stop();
            director.playableAsset = null;
        }

        private IEnumerator PlayDialogue(PrototypeCutsceneStep step)
        {
            SetState(PrototypeCutsceneState.ShowingDialogue, $"DIALOGUE  {step.actor_id}");
            advancePressed = false;
            revealPressed = false;
            currentDialogueStep = step;

            string speaker = IsEnglish() ? step.speaker_en : step.speaker_ko;
            currentFullDialogue = IsEnglish() ? step.text_en : step.text_ko;
            view.ShowDialogue(speaker, string.Empty);
            dialogueTyping = true;

            string visible = string.Empty;
            for (int index = 0; index < currentFullDialogue.Length && !skipRequested; index++)
            {
                if (revealPressed)
                {
                    visible = currentFullDialogue;
                    view.SetDialogueBody(visible);
                    break;
                }

                visible += currentFullDialogue[index];
                view.SetDialogueBody(visible);
                yield return new WaitForSecondsRealtime(typeInterval);
            }

            if (skipRequested)
                yield break;

            view.SetDialogueBody(currentFullDialogue);
            dialogueTyping = false;
            revealPressed = false;

            while (!advancePressed && !skipRequested)
                yield return null;

            advancePressed = false;
            currentDialogueStep = null;
            view.HideDialogue();
        }

        private IEnumerator PlayChoice(PrototypeCutsceneStep step)
        {
            if (!choiceById.TryGetValue(step.choice_id, out PrototypeChoiceDefinition choice))
            {
                Fail($"choice_id를 찾을 수 없습니다: {step.choice_id}");
                yield break;
            }

            SetState(PrototypeCutsceneState.ShowingChoice, $"CHOICE  {step.choice_id}");
            currentChoice = choice;
            selectedChoice = -1;
            if (!RefreshChoiceView(out string error))
            {
                Fail(error);
                yield break;
            }

            while (selectedChoice < 0)
                yield return null;

            PrototypeChoiceOption option = choice.options[selectedChoice];
            if (!ApplyEffects(option.effects, out error))
            {
                Fail($"choice '{choice.id}' effects 오류: {error}");
                yield break;
            }

            nextSequenceOverride = option.goto_seq;
            view.HideChoice();
            currentChoice = null;
            currentChoiceEnabled = null;
            selectedChoice = -1;
        }

        private IEnumerator ExecuteCommand(PrototypeCutsceneStep step, bool immediate)
        {
            if (!string.IsNullOrWhiteSpace(step.event_key) && firedEventKeys.Contains(step.event_key))
                yield break;

            if (immediate)
            {
                if (!ApplyCommandImmediate(step, out string immediateError))
                    Fail(immediateError);
            }
            else
            {
                switch (step.action)
                {
                    case "screen_fade":
                        yield return view.Fade(string.Equals(step.value, "in", StringComparison.OrdinalIgnoreCase), Mathf.Max(0.01f, step.duration));
                        break;

                    case "screen_flash":
                        yield return view.Flash(Mathf.Max(0.06f, step.duration));
                        break;

                    case "camera_shake":
                        float strength = string.Equals(step.value, "strong", StringComparison.OrdinalIgnoreCase) ? 0.28f : 0.16f;
                        yield return view.Shake(Mathf.Max(0.05f, step.duration), strength);
                        break;

                    case "set_active":
                        bool active = ParseActive(step.value);
                        if (!view.TrySetActive(step.target_id, active))
                            Fail($"set_active 대상이 없습니다: {step.target_id}");
                        break;

                    case "set_light":
                        view.SetWarningLights(ParseColor(step.value));
                        break;

                    case "play_sfx":
                        Debug.Log($"[CutscenePrototype] SFX placeholder: {step.value}");
                        break;

                    default:
                        Fail($"지원하지 않는 command action입니다: {step.action}");
                        break;
                }
            }

            if (State != PrototypeCutsceneState.Failed && !string.IsNullOrWhiteSpace(step.event_key))
                firedEventKeys.Add(step.event_key);
        }

        private IEnumerator SkipAndComplete()
        {
            SetState(PrototypeCutsceneState.Skipping, "필수 이벤트와 종료 상태 적용");
            director.Stop();
            director.playableAsset = null;
            view.HideDialogue();
            view.HideChoice();
            view.ClearTransientEffects();

            for (int index = Mathf.Max(0, currentStepIndex); index < data.steps.Length; index++)
            {
                PrototypeCutsceneStep step = data.steps[index];
                if (!step.fire_on_skip || firedEventKeys.Contains(step.event_key))
                    continue;

                IEnumerator execution = ExecuteCommand(step, true);
                while (execution.MoveNext())
                    yield return execution.Current;
                if (State == PrototypeCutsceneState.Failed)
                    yield break;
            }

            yield return CompleteCutscene(true);
        }

        private IEnumerator CompleteCutscene(bool skipped)
        {
            SetState(PrototypeCutsceneState.Transitioning, skipped ? "스킵 종료 상태 적용" : "최종 상태 적용");
            if (!ApplyEndState(out string error))
            {
                Fail(error);
                yield break;
            }

            SetState(PrototypeCutsceneState.Completing, "컷씬 종료 처리");
            yield return new WaitForSecondsRealtime(0.15f);
            view.HideDialogue();
            view.HideChoice();
            completedCutscenes.Add(data.cutscene_id);
            LastCompletionWasSkipped = skipped;
            SetState(PrototypeCutsceneState.Completed, skipped ? "스킵 완료 · R로 초기화" : "완료 · R로 초기화");
            playbackRoutine = null;
            Debug.Log($"[CutscenePrototype] COMPLETE: {data.cutscene_id}, skipped={skipped}");
        }

        private bool ApplyEndState(out string error)
        {
            error = null;
            if (data.end_state == null)
                return true;

            PrototypeEndState endState = data.end_state;
            if (endState.actor_states != null)
            {
                foreach (PrototypeActorEndState actorState in endState.actor_states)
                {
                    if (!view.TrySetPosition(actorState.target_id, new Vector3(actorState.x, actorState.y, 0f)))
                    {
                        error = $"end_state 대상을 찾을 수 없습니다: {actorState.target_id}";
                        return false;
                    }
                    view.TrySetActive(actorState.target_id, actorState.active);
                }
            }

            if (!string.IsNullOrWhiteSpace(endState.light_color))
                view.SetWarningLights(ParseColor(endState.light_color));

            view.SetCameraState(endState.camera_x, endState.camera_y, endState.camera_size);

            if (!string.IsNullOrWhiteSpace(endState.fade_state))
                view.SetFadeState(endState.fade_state);

            return ApplyEffects(endState.effects, out error);
        }

        private bool ApplyCommandImmediate(PrototypeCutsceneStep step, out string error)
        {
            error = null;
            switch (step.action)
            {
                case "screen_fade":
                    view.SetFadeState(string.Equals(step.value, "in", StringComparison.OrdinalIgnoreCase) ? "clear" : "black");
                    return true;
                case "set_active":
                    if (!view.TrySetActive(step.target_id, ParseActive(step.value)))
                    {
                        error = $"set_active 대상을 찾을 수 없습니다: {step.target_id}";
                        return false;
                    }
                    return true;
                case "set_light":
                    view.SetWarningLights(ParseColor(step.value));
                    return true;
                case "play_sfx":
                    Debug.Log($"[CutscenePrototype] SFX placeholder (skip): {step.value}");
                    return true;
                default:
                    error = $"fire_on_skip에서 즉시 적용할 수 없는 action입니다: {step.action}";
                    return false;
            }
        }

        private IEnumerator WaitSkippable(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds && !skipRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void RequestSkip()
        {
            if (!IsRunning)
                return;

            if (State == PrototypeCutsceneState.ShowingChoice)
            {
                view.SetStatus("SHOWINGCHOICE", "선택 중에는 스킵할 수 없습니다");
                return;
            }

            skipRequested = true;
        }

        private void ToggleLocale()
        {
            locale = IsEnglish() ? "ko" : "en";
            RefreshHeader();

            if (State == PrototypeCutsceneState.ShowingDialogue && currentDialogueStep != null)
            {
                currentFullDialogue = IsEnglish() ? currentDialogueStep.text_en : currentDialogueStep.text_ko;
                string speaker = IsEnglish() ? currentDialogueStep.speaker_en : currentDialogueStep.speaker_ko;
                dialogueTyping = false;
                revealPressed = false;
                view.ShowDialogue(speaker, currentFullDialogue);
            }
            else if (State == PrototypeCutsceneState.ShowingChoice && currentChoice != null)
            {
                RefreshChoiceView(out _);
            }
        }

        private bool RefreshChoiceView(out string error)
        {
            error = null;
            PrototypeChoiceOption[] options = currentChoice.options;
            currentChoiceEnabled = new bool[options.Length];
            var labels = new string[options.Length];
            var locks = new string[options.Length];
            firstEnabledChoice = -1;

            for (int index = 0; index < options.Length; index++)
            {
                if (!EvaluateCondition(options[index].when, out bool enabled, out error))
                    return false;
                currentChoiceEnabled[index] = enabled;
                labels[index] = IsEnglish() ? options[index].text_en : options[index].text_ko;
                locks[index] = IsEnglish() ? options[index].lock_reason_en : options[index].lock_reason_ko;
                if (enabled && firstEnabledChoice < 0)
                    firstEnabledChoice = index;
            }

            if (firstEnabledChoice < 0)
            {
                error = $"choice '{currentChoice.id}'에 선택 가능한 항목이 없습니다.";
                return false;
            }

            string prompt = IsEnglish() ? currentChoice.prompt_en : currentChoice.prompt_ko;
            view.ShowChoice(prompt, labels, currentChoiceEnabled, locks);
            return true;
        }

        private void OnChoiceSelected(int index)
        {
            if (State == PrototypeCutsceneState.ShowingChoice)
                SelectChoice(index);
        }

        private void SelectChoice(int index)
        {
            if (currentChoiceEnabled == null || index < 0 || index >= currentChoiceEnabled.Length)
                return;
            if (!currentChoiceEnabled[index])
                return;
            selectedChoice = index;
        }

        private void BuildRuntimeLookups()
        {
            stepIndexBySequence.Clear();
            choiceById.Clear();
            for (int index = 0; index < data.steps.Length; index++)
                stepIndexBySequence[data.steps[index].seq] = index;
            if (data.choices == null)
                return;
            foreach (PrototypeChoiceDefinition choice in data.choices)
                choiceById[choice.id] = choice;
        }

        private bool ValidateRuntimeBindings(out string error)
        {
            error = null;
            foreach (PrototypeCutsceneStep step in data.steps)
            {
                if (step.type == "timeline" && (!timelineByKey.TryGetValue(step.timeline_key, out PrototypeTimelineAsset asset) || asset == null))
                {
                    error = $"timeline_key 바인딩이 없습니다: {step.timeline_key}";
                    return false;
                }
                if (step.type == "dialogue" && !view.HasTarget(step.actor_id))
                {
                    error = $"dialogue actor_id 바인딩이 없습니다: {step.actor_id}";
                    return false;
                }
                if (step.type == "command" && step.action == "set_active" && !view.HasTarget(step.target_id))
                {
                    error = $"command target_id 바인딩이 없습니다: {step.target_id}";
                    return false;
                }
            }

            if (data.end_state != null && data.end_state.actor_states != null)
            {
                foreach (PrototypeActorEndState actorState in data.end_state.actor_states)
                {
                    if (!view.HasTarget(actorState.target_id))
                    {
                        error = $"end_state target_id 바인딩이 없습니다: {actorState.target_id}";
                        return false;
                    }
                }
            }
            return true;
        }

        private bool ApplyEffects(string expression, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            var operations = new List<(string Flag, bool Value)>();
            string[] statements = expression.Split(';');
            foreach (string raw in statements)
            {
                string statement = raw.Trim();
                if (statement.Length == 0)
                    continue;
                string[] pair = statement.Split('=');
                if (pair.Length != 2)
                {
                    error = $"대입문 형식이 아닙니다: {statement}";
                    return false;
                }

                string key = pair[0].Trim();
                string value = pair[1].Trim();
                if (!key.StartsWith("flag.", StringComparison.Ordinal) || key.Length <= 5)
                {
                    error = $"지원하지 않는 상태 키입니다: {key}";
                    return false;
                }
                if (!bool.TryParse(value, out bool boolValue))
                {
                    error = $"플래그 값은 true/false여야 합니다: {value}";
                    return false;
                }
                operations.Add((key.Substring(5), boolValue));
            }

            foreach ((string flag, bool value) in operations)
            {
                if (value) flags.Add(flag);
                else flags.Remove(flag);
            }
            return true;
        }

        private bool EvaluateCondition(string expression, out bool result, out string error)
        {
            result = true;
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            string token = expression.Trim();
            bool negate = token.StartsWith("!", StringComparison.Ordinal);
            if (negate)
                token = token.Substring(1).Trim();
            if (!token.StartsWith("flag.", StringComparison.Ordinal) || token.Length <= 5)
            {
                error = $"지원하지 않는 when 조건입니다: {expression}";
                return false;
            }

            bool value = flags.Contains(token.Substring(5));
            result = negate ? !value : value;
            return true;
        }

        private void BuildTimelineLookup()
        {
            timelineByKey.Clear();
            if (timelines == null)
                return;

            foreach (PrototypeTimelineEntry entry in timelines)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.asset == null)
                    continue;
                timelineByKey[entry.key] = entry.asset;
            }
        }

        private void RefreshHeader()
        {
            if (data == null)
                return;
            string title = IsEnglish() ? data.title_en : data.title_ko;
            view.SetHeader(title, locale);
        }

        private string BuildDebugText()
        {
            var builder = new StringBuilder();
            builder.AppendLine($"state       {State}");
            builder.AppendLine($"cutscene    {data?.cutscene_id ?? "-"}");
            builder.AppendLine($"seq         {CurrentSequence}");
            builder.AppendLine($"director    {(director != null ? director.time.ToString("0.00") : "-")}");
            builder.AppendLine($"skip        {skipRequested}");
            builder.AppendLine($"input lock  {InputLocked}");
            builder.AppendLine($"events      {firedEventKeys.Count}");
            builder.AppendLine("flags");
            if (flags.Count == 0)
                builder.AppendLine("  (none)");
            else
                foreach (string flag in flags)
                    builder.AppendLine($"  {flag}");
            return builder.ToString();
        }

        private void CompleteWithoutPlayback(string detail)
        {
            SetState(PrototypeCutsceneState.Completed, detail);
            playbackRoutine = null;
        }

        private bool IsEnglish()
        {
            return string.Equals(locale, "en", StringComparison.OrdinalIgnoreCase);
        }

        private void SetState(PrototypeCutsceneState value, string detail)
        {
            State = value;
            view?.SetStatus(value.ToString().ToUpperInvariant(), detail);
        }

        private void Fail(string error)
        {
            State = PrototypeCutsceneState.Failed;
            view?.SetStatus("FAILED", error);
            view?.HideDialogue();
            view?.HideChoice();
            playbackRoutine = null;
            Debug.LogError($"[CutscenePrototype] {error}");
        }

        private static bool ParseActive(string value)
        {
            return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(value, "off", StringComparison.OrdinalIgnoreCase);
        }

        private static Color ParseColor(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && ColorUtility.TryParseHtmlString(value, out Color parsed))
                return parsed;
            return new Color(1f, 0.1f, 0.15f, 0.95f);
        }
    }
}
