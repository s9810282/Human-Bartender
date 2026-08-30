using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] private PlayerDataSO playerDataAsset;
    [SerializeField] private GameObject choicesPanel;
    [SerializeField] private List<ChoicePanel> choicePanels = new();

    private IPlayerDataReader PlayerData => playerDataAsset;

    private ChoiceData[] curChoiceData;
    private NewStreetOptionData[] curOutsideOptions;

    // GameStateManager의 언어 설정을 실시간 참조
    public ELanguage CurrentLanguage => GameStateManager.Instance != null
        ? GameStateManager.Instance.Language
        : ELanguage.Ko;

    void Start()
    {

    }

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
            choicePanels[i].GetButton().onClick.RemoveAllListeners();
            choicePanels[i].GetButton().onClick.AddListener(() => data.callBackEvent.Invoke(curChoiceData[num]));
            choicePanels[i].GetButton().onClick.AddListener(() => ChoiceSelect(num));
            choicePanels[i].SetActive(true);
        }
    }

    /// <summary>
    /// [신규] Step 기반 아웃사이드 선택지(NewStreetOptionData[]) 패널 표시 및 이벤트 등록
    /// </summary>
    public void ShowOutsideChoice(NewStreetOptionData[] options, Action<NewStreetOptionData> onSelected)
    {
        if (options == null || options.Length == 0) return;

        Debug.Log("아웃사이드 선택지 UI 표시 중...");

        choicesPanel.SetActive(true);
        curOutsideOptions = options;

        for (int i = 0; i < choicePanels.Count; i++)
        {
            choicePanels[i].ResetPanel();
            choicePanels[i].SetActive(false);
        }

        int visibleIndex = 0;
        for (int i = 0; i < curOutsideOptions.Length; i++)
        {
            if (visibleIndex >= choicePanels.Count)
            {
                Debug.LogWarning("[UIDialogueChoiceView] 표시할 선택지 수가 choicePanels 슬롯 수를 초과했습니다.");
                break;
            }

            var option = curOutsideOptions[i];

            // LocalizedText? 또는 Texts? 타입 파싱 처리
            string optionText = GetText(option.Text);

            int num = i;
            choicePanels[visibleIndex].SetPanelText(optionText);

            var button = choicePanels[visibleIndex].GetButton();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                OutsideChoiceSelect();
                onSelected?.Invoke(curOutsideOptions[num]);
            });

            choicePanels[visibleIndex].SetActive(true);
            visibleIndex++;
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
                EAffinityTier targettier = (EAffinityTier)System.Enum.Parse(typeof(EAffinityTier), checkType.minTier, true);
                return characterTier >= targettier;

            case EConditionCheckType.Skill:
                ESkillTier targetSkill = (ESkillTier)System.Enum.Parse(typeof(ESkillTier), checkType.minTier, true);
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
        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
    }

    private void OutsideChoiceSelect()
    {
        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
    }

    // LocalizedText? 지원
    private string GetText(LocalizedText? textData)
    {
        if (!textData.HasValue) return string.Empty;

        var text = textData.Value;
        return CurrentLanguage switch
        {
            ELanguage.En => !string.IsNullOrEmpty(text.En) ? text.En : text.Ko,
            _ => text.Ko
        };
    }

    // Texts? 지원 오버로드
    private string GetText(Texts textData)
    {
        if (textData == null) return string.Empty;

        return CurrentLanguage switch
        {
            ELanguage.En => !string.IsNullOrEmpty(textData.En) ? textData.En : textData.Ko,
            _ => textData.Ko
        };
    }
}