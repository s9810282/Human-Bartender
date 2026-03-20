using System;
using UnityEngine;
using Newtonsoft.Json.Linq;

[Serializable]
public struct SceneData
{
    public string scene_id;
    public string customer;
    public string customer_display_name;
    public string customer_name_color;
    public DialogueData[] dialogues;
}

[Serializable]
public struct DialogueData
{
    public string id;
    public string speaker;
    public string type;
    public string text;
    public string expression;
    public string next;
    public ChoiceData[] choices;
    public TriggerData trigger;
}

[Serializable]
public struct ChoiceData
{
    public string text;
    public string next;
    public string choice_effect;
}

[Serializable]
public struct TriggerData
{
    public string type;
    public TriggerDetailData data;
}


[Serializable]
public struct TriggerDetailData
{
    public string character_id;
    public string sfx;
    public string bgm;
    public string animation;
    public string craft_event_id;
}

[Serializable]
public class DayDatabBase
{
    public int day;
    public SceneData[] scenes;
}

[CreateAssetMenu(fileName = "DayDatabBase", menuName = "Data/DayDatabBase")]
public class DayDataSO : ScriptableObject
{
    public DayDatabBase dayData;
}

