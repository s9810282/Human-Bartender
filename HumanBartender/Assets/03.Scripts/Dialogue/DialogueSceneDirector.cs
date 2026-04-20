using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueSceneDirector : MonoBehaviour
{
    [Header("SO Data")]
    [SerializeField] CharacterDataSO characterData;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private UIDialogueTextView typer;
    [SerializeField] private UIDialogueChoice choiceManager;
    [SerializeField] private DialogueCharacterManager characterManager;
    [SerializeField] private DialogueBackgroundManager backgroundManager;
    [SerializeField] private DialogueTriggerManager triggerManager;



    private Dictionary<string, CharacterData> characterDB = new Dictionary<string, CharacterData>();


    void Start()
    {
        characterDB.Clear();
        foreach (var character in characterData.characterData.Characters)
        {
            characterDB[character.Id] = character;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetImage()
    {

    }

    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }
    public async UniTask ShowDialogueAsync(DialogueData dialogueData)
    {
        dialoguePanel.SetActive(true);

        if (characterDB.TryGetValue(dialogueData.Speaker, out CharacterData speakerData))
        {
            if (ColorUtility.TryParseHtmlString(speakerData.NameColor, out Color color))
                typer.SetNameColor(color);

            typer.ClearText();
            typer.SetNameText(speakerData.DisplayName);
            
            if (!string.IsNullOrEmpty(dialogueData.Expression))
            {
                await characterManager.SetCharacterAsync(dialogueData.Speaker, dialogueData.Expression);
            }
        }

        characterManager.OnDialogueStart(dialogueData.Speaker);
        await typer.StartType(new TypingData(dialogueData.Text));
        characterManager.OnDialogueEnd(dialogueData.Speaker);
    }

    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onChoiceSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onChoiceSelected));
    }
    public void SkipTyping()
    {
        typer.OnScreenClick();
        characterManager.OnDialogueEnd();
    }

    public async UniTask<string> ExcuteTriggerAsync(TriggerData? trigger)
    {
        typer.ClearText();

        Logger.Log(typer.dialogueText.text);

        string id = await triggerManager.ExecuteTriggerAsync(trigger);
        
        return id;
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
