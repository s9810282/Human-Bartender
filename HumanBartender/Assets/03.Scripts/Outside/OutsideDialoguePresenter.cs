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
    [SerializeField] private UIDialogueChoiceView choiceManager;

    const string PLAYER_ID = "luna";


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

        //이름 텍스트 및, 애니메이션 전화 여기서, 일반 Dialgue와 동일함.

        await typer.StartType(new TypingData(
            dialogueData.Text,
            dialogueData.Speaker,
            Vector2.zero,
            Color.white,
            dialogueData.Speaker == PLAYER_ID));
    }

    public void SkipTyping()
    {
        typer.OnScreenClick();
    }

    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }

    public void EndScene()
    {
        typer.ClearText();
    }
}
