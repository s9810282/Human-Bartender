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
    public void ShowDialogue(DialogueData dialogueData, Action onTypingComplete)
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

        typer.StartType(new TypingData(dialogueData.text, onTypingComplete));
    }
    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onChoiceSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onChoiceSelected));
    }
    public void SkipTyping()
    {
        typer.OnScreenClick();
    }


    public void PlayTrigger(TriggerData trigger, Action callBack)
    {
        characterManager.OffCharacter();
        StartCoroutine(ExcuteTrigger(trigger, callBack));
    }
    private IEnumerator ExcuteTrigger(TriggerData trigger, Action callBack)
    {
        yield return StartCoroutine(triggerManager.ExecuteTriggerCoroutine(trigger, callBack));
    }

    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
