using System;
using System.Collections.Generic;
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



public class UIDialogueChoice : MonoBehaviour
{
    public GameObject choicesPanel;
    public List<ChoicePanel> choicePanels = new();

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
            int num = i;
            choicePanels[i].SetPanelText(curChoiceData[i].text);
            choicePanels[i].GetButton().onClick.AddListener
                (() => data.callBackEvent.Invoke(curChoiceData[num]));
            choicePanels[i].GetButton().onClick.AddListener(() => ChoiceSelect(num));
            choicePanels[i].SetActive(true);
        }
    }

    public void ChoiceSelect(int num)
    {
        ChoiceData choiceData = curChoiceData[num];

        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
    }
}
