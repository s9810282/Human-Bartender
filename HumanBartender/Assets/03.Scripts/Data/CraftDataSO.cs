using System;
using UnityEngine;

[Serializable]
public struct CraftEventData
{
    public string id;
    public bool auto_open_recipe_ui;
    public TutorialData tutorial;
    public EvaluationData evaluation;
    public ReactionsData reactions;
    public EffectsData effects;
}

[Serializable]
public struct TutorialData
{
    public bool enabled;
    public TutorialStepData[] steps;
}

[Serializable]
public struct TutorialStepData
{
    public string type;
    public string target;
    public string text;
}

[Serializable]
public struct EvaluationData
{
    public RuleData[] rules;
    public string default_grade;
    public bool craft_penalty;
}

[Serializable]
public struct RuleData
{
    public string grade;
    public string match_type;
    public string[] match_values;
    public string description;
}

[Serializable]
public struct ReactionsData
{
    public string S;
    public string A;
    public string B;
    public string C;
}

[Serializable]
public struct EffectsData
{
    public EffectDetailData S;
    public EffectDetailData A;
    public EffectDetailData B;
    public EffectDetailData C;
}

[Serializable]
public struct EffectDetailData
{
    public int affinity;
    public int karma;
}

[CreateAssetMenu(fileName = "CraftDataBase", menuName = "Data/CraftDataBase")]
public class CraftDataSO : ScriptableObject
{
    public CraftDataBase craftData;
}

[Serializable]
public class CraftDataBase
{
    public CraftEventData[] craft_events;
}



