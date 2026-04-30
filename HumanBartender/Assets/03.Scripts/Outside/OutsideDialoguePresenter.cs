using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.TextCore.Text;

public class OutsideDialoguePresenter : MonoBehaviour, IDialoguePresenter
{
    [SerializeField] GameObject dialoguePanel;
    
    [SerializeField] private UIDialogueTextView targetTyper;
    [SerializeField] private UIDialogueTextView playerTyper;
    [SerializeField] private DialogueTriggerManager triggerManager;
    [SerializeField] private UIDialogueChoice choiceManager;

    const string PLAYER_ID = "luna";


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public async UniTask<string> ExecuteTriggerAsync(TriggerData? trigger)
    {
        return "";

        targetTyper.ClearText();

        string id = await triggerManager.ExecuteTriggerAsync(trigger);

        return id;
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onSelected));
    }

    public async UniTask ShowDialogueAsync(DialogueData dialogueData, CancellationToken token)
    {
        dialoguePanel.SetActive(true);

        //추후 DB 추가
        //if (characterDB.TryGetValue(dialogueData.Speaker, out CharacterData speakerData))

        UIDialogueTextView typer = dialogueData.Speaker == PLAYER_ID ? playerTyper : targetTyper;

        typer.ClearText();
        
        //이름 텍스트 및, 애니메이션 전화 여기서, 일반 Dialgue와 동일함.
        
        await typer.StartType(new TypingData(dialogueData.Text));
    }

    public void SkipTyping()
    {
        targetTyper.OnScreenClick();
    }

    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }

    public void EndScene()
    {
        targetTyper.ClearText();
        playerTyper.ClearText();
    }
}
