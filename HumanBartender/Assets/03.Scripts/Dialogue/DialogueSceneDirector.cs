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
        foreach (var character in characterData.characterData.characters)
        {
            characterDB[character.id] = character;
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

        if (characterDB.TryGetValue(dialogueData.speaker, out CharacterData speakerData))
        {
            if (ColorUtility.TryParseHtmlString(speakerData.name_color, out Color color))
                typer.SetNameColor(color);
            
            typer.SetNameText(speakerData.display_name);

            if (!string.IsNullOrEmpty(dialogueData.expression))
                characterManager.SetCharacter(dialogueData.speaker, dialogueData.expression);
            else
                characterManager.OffCharacter();
        }

        await typer.StartType(new TypingData(dialogueData.text));
    }

    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onChoiceSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onChoiceSelected));
    }
    public void SkipTyping()
    {
        typer.OnScreenClick();
    }

    public async UniTask<string> ExcuteTriggerAsync(TriggerData trigger)
    {
        characterManager.OffCharacter();
        string id = await triggerManager.ExecuteTriggerAsync(trigger);

        return id;
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
