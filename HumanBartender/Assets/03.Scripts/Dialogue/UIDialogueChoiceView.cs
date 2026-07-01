using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>선택지 표시에 필요한 데이터와 선택 시 호출할 콜백을 묶은 컨테이너.</summary>
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



/// <summary>
/// 선택지 UI 패널들을 관리한다. 각 선택지의 조건(호감도/스킬/재화/플래그)을 검사해 조건 미충족 시 숨긴다.
/// </summary>
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

    /// <summary>
    /// 선택지 패널들을 초기화하고, 조건을 만족하는 선택지만 텍스트를 채워 활성화한 뒤 클릭 콜백을 등록한다.
    /// </summary>
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

    /// <summary>선택지 노출 조건 하나를 검사한다 (호감도 등급/스킬 등급/보유 재화/플래그).</summary>
    public bool CheckCondition(ChoiceConditionCheck checkType)
    {
        switch (checkType.Type)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                EAffinityTier characterTier = PlayerData.GetCurCharacterAffinityTier(checkType.Character);
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

    /// <summary>선택된 항목의 콜백은 이미 리스너에서 처리되었으므로, 여기서는 패널을 닫고 초기화만 한다.</summary>
    public void ChoiceSelect(int num)
    {
        ChoiceData choiceData = curChoiceData[num];

        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
    }
}
