using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectLuna.CutscenePrototype
{
    public static class CutscenePrototypeLoader
    {
        public const string SupportedSchemaVersion = "cutscene_proto_1.1";

        public static bool TryLoad(TextAsset source, out PrototypeCutsceneData data, out string error)
        {
            data = null;
            error = null;

            if (source == null)
            {
                error = "컷씬 JSON TextAsset이 연결되지 않았습니다.";
                return false;
            }

            try
            {
                data = JsonUtility.FromJson<PrototypeCutsceneData>(source.text);
            }
            catch (Exception exception)
            {
                error = $"컷씬 JSON 파싱 실패: {exception.Message}";
                return false;
            }

            if (data == null)
            {
                error = "컷씬 JSON 결과가 비어 있습니다.";
                return false;
            }

            if (!string.Equals(data.schema_version, SupportedSchemaVersion, StringComparison.Ordinal))
            {
                error = $"지원하지 않는 schema_version입니다: {data.schema_version}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.cutscene_id))
            {
                error = "cutscene_id가 비어 있습니다.";
                return false;
            }

            if (data.play_mode != "once" && data.play_mode != "repeatable")
            {
                error = $"지원하지 않는 play_mode입니다: {data.play_mode}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(data.title_ko) || string.IsNullOrWhiteSpace(data.title_en))
            {
                error = "title_ko와 title_en이 모두 필요합니다.";
                return false;
            }

            if (data.steps == null || data.steps.Length == 0)
            {
                error = "재생할 steps가 없습니다.";
                return false;
            }

            if (!ValidateConditionSyntax(data.required_when, out error))
            {
                error = $"required_when 오류: {error}";
                return false;
            }

            Array.Sort(data.steps, (left, right) => left.seq.CompareTo(right.seq));

            var usedSequences = new HashSet<int>();
            var eventKeys = new HashSet<string>(StringComparer.Ordinal);
            var choices = new Dictionary<string, PrototypeChoiceDefinition>(StringComparer.Ordinal);

            if (data.choices != null)
            {
                foreach (PrototypeChoiceDefinition choice in data.choices)
                {
                    if (!ValidateChoice(choice, choices, out error))
                        return false;
                }
            }

            foreach (PrototypeCutsceneStep step in data.steps)
            {
                if (step.seq <= 0)
                {
                    error = $"seq는 1 이상이어야 합니다: {step.seq}";
                    return false;
                }

                if (!usedSequences.Add(step.seq))
                {
                    error = $"중복 seq가 있습니다: {step.seq}";
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(step.event_key) && !eventKeys.Add(step.event_key))
                {
                    error = $"중복 event_key가 있습니다: {step.event_key}";
                    return false;
                }

                if (step.fire_on_skip && string.IsNullOrWhiteSpace(step.event_key))
                {
                    error = $"seq {step.seq}: fire_on_skip 스텝에는 event_key가 필요합니다.";
                    return false;
                }

                if (!ValidateStep(step, choices, out error))
                {
                    error = $"seq {step.seq}: {error}";
                    return false;
                }

                if (!ValidateConditionSyntax(step.when, out error))
                {
                    error = $"seq {step.seq} when 오류: {error}";
                    return false;
                }
                if (!ValidateEffectsSyntax(step.effects, out error))
                {
                    error = $"seq {step.seq} effects 오류: {error}";
                    return false;
                }
            }

            foreach (PrototypeCutsceneStep step in data.steps)
            {
                if (!ValidateSequenceReference(step.next_seq, usedSequences, step.seq, "next_seq", out error)
                    || !ValidateSequenceReference(step.true_seq, usedSequences, step.seq, "true_seq", out error)
                    || !ValidateSequenceReference(step.false_seq, usedSequences, step.seq, "false_seq", out error))
                    return false;
            }

            foreach (PrototypeChoiceDefinition choice in choices.Values)
            {
                foreach (PrototypeChoiceOption option in choice.options)
                {
                    if (!ValidateSequenceReference(option.goto_seq, usedSequences, 0, $"choice '{choice.id}' goto_seq", out error))
                        return false;
                }
            }

            if (data.end_state != null && data.end_state.actor_states != null)
            {
                var endTargets = new HashSet<string>(StringComparer.Ordinal);
                foreach (PrototypeActorEndState actorState in data.end_state.actor_states)
                {
                    if (actorState == null || string.IsNullOrWhiteSpace(actorState.target_id))
                    {
                        error = "end_state.actor_states에 target_id가 필요합니다.";
                        return false;
                    }
                    if (!endTargets.Add(actorState.target_id))
                    {
                        error = $"end_state에 중복 target_id가 있습니다: {actorState.target_id}";
                        return false;
                    }
                }
            }


            if (data.end_state != null && !ValidateEffectsSyntax(data.end_state.effects, out error))
            {
                error = $"end_state.effects 오류: {error}";
                return false;
            }

            return true;
        }

        private static bool ValidateChoice(
            PrototypeChoiceDefinition choice,
            Dictionary<string, PrototypeChoiceDefinition> choices,
            out string error)
        {
            error = null;
            if (choice == null || string.IsNullOrWhiteSpace(choice.id))
            {
                error = "choices에 id가 필요합니다.";
                return false;
            }

            if (choices.ContainsKey(choice.id))
            {
                error = $"중복 choice id가 있습니다: {choice.id}";
                return false;
            }
            choices.Add(choice.id, choice);

            if (string.IsNullOrWhiteSpace(choice.prompt_ko) || string.IsNullOrWhiteSpace(choice.prompt_en))
            {
                error = $"choice '{choice.id}'에 한·영 prompt가 모두 필요합니다.";
                return false;
            }

            if (choice.options == null || choice.options.Length < 2 || choice.options.Length > 4)
            {
                error = $"choice '{choice.id}'의 options는 2~4개여야 합니다.";
                return false;
            }

            foreach (PrototypeChoiceOption option in choice.options)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.text_ko) || string.IsNullOrWhiteSpace(option.text_en))
                {
                    error = $"choice '{choice.id}'의 모든 선택지에 한·영 텍스트가 필요합니다.";
                    return false;
                }

                bool conditional = !string.IsNullOrWhiteSpace(option.when);
                if (conditional && (string.IsNullOrWhiteSpace(option.lock_reason_ko) || string.IsNullOrWhiteSpace(option.lock_reason_en)))
                {
                    error = $"choice '{choice.id}'의 조건부 선택지에는 한·영 lock_reason이 필요합니다.";
                    return false;
                }
                if (!ValidateConditionSyntax(option.when, out error))
                {
                    error = $"choice '{choice.id}' when 오류: {error}";
                    return false;
                }
                if (!ValidateEffectsSyntax(option.effects, out error))
                {
                    error = $"choice '{choice.id}' effects 오류: {error}";
                    return false;
                }
            }

            return true;
        }

        private static bool ValidateStep(
            PrototypeCutsceneStep step,
            IReadOnlyDictionary<string, PrototypeChoiceDefinition> choices,
            out string error)
        {
            error = null;
            if (step.fire_on_skip && step.type != "command")
            {
                error = "fire_on_skip은 command 스텝에만 사용할 수 있습니다.";
                return false;
            }

            if (step.fire_on_skip
                && step.action != "screen_fade"
                && step.action != "set_active"
                && step.action != "set_light"
                && step.action != "play_sfx")
            {
                error = $"fire_on_skip에서 즉시 적용할 수 없는 action입니다: {step.action}";
                return false;
            }

            switch (step.type)
            {
                case "timeline":
                    if (string.IsNullOrWhiteSpace(step.timeline_key))
                    {
                        error = "timeline_key가 필요합니다.";
                        return false;
                    }
                    return true;

                case "dialogue":
                    if (string.IsNullOrWhiteSpace(step.actor_id))
                    {
                        error = "actor_id가 필요합니다.";
                        return false;
                    }
                    if (string.IsNullOrWhiteSpace(step.text_ko) || string.IsNullOrWhiteSpace(step.text_en))
                    {
                        error = "한국어·영어 대사가 모두 필요합니다.";
                        return false;
                    }
                    return true;

                case "choice":
                    if (string.IsNullOrWhiteSpace(step.choice_id) || !choices.ContainsKey(step.choice_id))
                    {
                        error = $"유효한 choice_id가 필요합니다: {step.choice_id}";
                        return false;
                    }
                    return true;

                case "branch":
                    if (string.IsNullOrWhiteSpace(step.when))
                    {
                        error = "branch에는 when이 필요합니다.";
                        return false;
                    }
                    if (step.true_seq <= 0 || step.false_seq <= 0)
                    {
                        error = "branch에는 true_seq와 false_seq가 모두 필요합니다.";
                        return false;
                    }
                    return true;

                case "command":
                    if (string.IsNullOrWhiteSpace(step.action))
                    {
                        error = "action이 필요합니다.";
                        return false;
                    }
                    if (step.action != "screen_fade"
                        && step.action != "screen_flash"
                        && step.action != "camera_shake"
                        && step.action != "set_active"
                        && step.action != "set_light"
                        && step.action != "play_sfx")
                    {
                        error = $"지원하지 않는 command action입니다: {step.action}";
                        return false;
                    }
                    if (step.action == "set_active" && string.IsNullOrWhiteSpace(step.target_id))
                    {
                        error = "set_active에는 target_id가 필요합니다.";
                        return false;
                    }
                    return true;

                case "wait":
                    if (step.duration < 0f)
                    {
                        error = "duration은 0 이상이어야 합니다.";
                        return false;
                    }
                    return true;

                default:
                    error = $"지원하지 않는 step.type입니다: {step.type}";
                    return false;
            }
        }

        private static bool ValidateSequenceReference(
            int target,
            HashSet<int> sequences,
            int owner,
            string field,
            out string error)
        {
            error = null;
            if (target == 0)
                return true;
            if (sequences.Contains(target))
                return true;

            string ownerText = owner > 0 ? $"seq {owner}" : "데이터";
            error = $"{ownerText}: {field}가 존재하지 않는 seq {target}을 참조합니다.";
            return false;
        }

        private static bool ValidateConditionSyntax(string expression, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
                return true;

            string token = expression.Trim();
            if (token.StartsWith("!", StringComparison.Ordinal))
                token = token.Substring(1).Trim();
            if (!token.StartsWith("flag.", StringComparison.Ordinal) || token.Length <= 5)
            {
                error = $"지원하지 않는 조건입니다: {expression}";
                return false;
            }
            return true;
        }

        private static bool ValidateEffectsSyntax(string expression, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(expression))
                return true;

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
                if (!bool.TryParse(value, out _))
                {
                    error = $"플래그 값은 true/false여야 합니다: {value}";
                    return false;
                }
            }
            return true;
        }
    }
}
