using System;
using UnityEngine;
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

// 트리거 타입에 따라 필요한 데이터가 다르므로, 가능한 모든 필드를 선언해 둡니다.
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

