using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.TextCore.Text;

public class OutsideDialoguePresenter : MonoBehaviour, IDialoguePresenter
{
    [SerializeField] GameObject dialoguePanel;
    
    [SerializeField] private UIDialogueTextView typer;
    [SerializeField] private DialogueTriggerManager triggerManager;
    [SerializeField] private UIDialogueChoice choiceManager;




    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public async UniTask<string> ExecuteTriggerAsync(TriggerData? trigger)
    {
        typer.ClearText();

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



        typer.ClearText();

        await typer.StartType(new TypingData(dialogueData.Text));

    }

    public void SkipTyping()
    {
        typer.OnScreenClick();
    }

    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }
}
