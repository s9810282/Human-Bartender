using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class ChoiceSelectData
{
    public ChoiceData[] data;
    public Action<ChoiceData> callBackEvent;

    public ChoiceSelectData()
    {
    }

    public ChoiceSelectData(ChoiceData[] data, Action<ChoiceData> callBackEvent)
    {
        this.data = data;
        this.callBackEvent = callBackEvent;
    }
}



public class UIDialogueChoiceView : MonoBehaviour
{
    [SerializeField] PlayerDataSO playerDataAsset;
    [SerializeField] GameObject choicesPanel;
    [SerializeField] List<ChoicePanel> choicePanels = new();

    IPlayerDataReader PlayerData => playerDataAsset;

    ChoiceData[] curChoiceData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowChoice(ChoiceSelectData data)
    {
        Debug.Log("선택지 UI 표시 중...");

        choicesPanel.SetActive(true);

        curChoiceData = data.data;

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].SetActive(false);

        for (int i = 0; i < curChoiceData.Length; i++)
        {
            if (curChoiceData[i].Condition != null)
            {
                var condition = curChoiceData[i].Condition.Value;

                bool isConditionMet = condition.Operator == "and"
                    ? condition.Checks.All(item => CheckCondition(item))
                    : condition.Checks.Any(item => CheckCondition(item));

                if (!isConditionMet) continue;
            }


            int num = i;
            choicePanels[i].SetPanelText(curChoiceData[i].Text);
            choicePanels[i].GetButton().onClick.AddListener
                (() => data.callBackEvent.Invoke(curChoiceData[num]));
            choicePanels[i].GetButton().onClick.AddListener(() => ChoiceSelect(num));
            choicePanels[i].SetActive(true);
        }
    }

    public bool CheckCondition(ChoiceConditionCheck checkType)
    {
        switch (checkType.Type)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                EAffinityTier characterTier = PlayerData.GetCurCharacterTier(checkType.Character);
                EAffinityTier targettier = (EAffinityTier)Enum.Parse(typeof(EAffinityTier), checkType.minTier, true);
                return characterTier >= targettier;

            case EConditionCheckType.Skill:
                ESkillTier targetSkill = (ESkillTier)Enum.Parse(typeof(ESkillTier), checkType.minTier, true);
                return PlayerData.GetSkillTier() >= targetSkill;
            
            case EConditionCheckType.Money:
                return PlayerData.HasEnoughMoney(checkType.minAmount.Value);
                
            case EConditionCheckType.Flag:
                //추후 작업
                break;
        }

        return false;
    }

    public void ChoiceSelect(int num)
    {
        ChoiceData choiceData = curChoiceData[num];

        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
    }
}
