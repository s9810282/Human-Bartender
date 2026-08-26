using System;

namespace ProjectLuna.CutscenePrototype
{
    [Serializable]
    public sealed class PrototypeCutsceneData
    {
        public string schema_version;
        public string cutscene_id;
        public string title_ko;
        public string title_en;
        public bool auto_play;
        public string play_mode = "once";
        public string required_when;
        public PrototypeChoiceDefinition[] choices;
        public PrototypeCutsceneStep[] steps;
        public PrototypeEndState end_state;
    }

    [Serializable]
    public sealed class PrototypeCutsceneStep
    {
        public int seq;
        public string type;
        public string timeline_key;
        public string choice_id;
        public string when;
        public int next_seq;
        public int true_seq;
        public int false_seq;
        public string effects;
        public string event_key;
        public bool fire_on_skip;
        public string action;
        public string target_id;
        public string actor_id;
        public string speaker_ko;
        public string speaker_en;
        public string text_ko;
        public string text_en;
        public string value;
        public float duration;
    }

    [Serializable]
    public sealed class PrototypeChoiceDefinition
    {
        public string id;
        public string prompt_ko;
        public string prompt_en;
        public PrototypeChoiceOption[] options;
    }

    [Serializable]
    public sealed class PrototypeChoiceOption
    {
        public string text_ko;
        public string text_en;
        public string when;
        public string lock_reason_ko;
        public string lock_reason_en;
        public string effects;
        public int goto_seq;
    }

    [Serializable]
    public sealed class PrototypeEndState
    {
        public PrototypeActorEndState[] actor_states;
        public float camera_x;
        public float camera_y;
        public float camera_size = 5.4f;
        public string light_color;
        public string fade_state;
        public string effects;
    }

    [Serializable]
    public sealed class PrototypeActorEndState
    {
        public string target_id;
        public bool active = true;
        public float x;
        public float y;
    }

    public enum PrototypeCutsceneState
    {
        Idle,
        Preparing,
        PlayingTimeline,
        ShowingDialogue,
        ShowingChoice,
        Transitioning,
        Skipping,
        Completing,
        Completed,
        Failed
    }
}
