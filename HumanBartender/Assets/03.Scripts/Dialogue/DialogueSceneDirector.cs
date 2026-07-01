using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대화 씬의 실제 UI(텍스트/선택지/캐릭터/배경) 연출을 담당하는 컴포넌트.
/// 현재 DialogueManager(레거시)가 직접 참조해서 호출하는 구조이며, IDialoguePresenter와 메서드 구성이
/// 유사하지만 시그니처가 달라(ShowDialogueAsync에 CancellationToken 없음 등) 인터페이스를 구현하지는 않는다.
/// </summary>
public class DialogueSceneDirector : MonoBehaviour
{
    [Header("SO Data")]
    [SerializeField] CharacterDataSO characterData;

    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private UIDialogueTextView typer;
    [SerializeField] private UIDialogueChoiceView choiceManager;
    [SerializeField] private DialogueCharacterManager characterManager;
    [SerializeField] private DialogueBackgroundManager backgroundManager;
    [SerializeField] private DialogueTriggerManager triggerManager;


    const string PLAYER_ID = "luna";

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

    /// <summary>연출/시스템 액션 진행을 위해 대화창을 숨긴다.</summary>
    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }
    /// <summary>
    /// 대사창을 열고 화자 이름 색상/캐릭터 표정을 반영한 뒤, 타이핑 애니메이션으로 텍스트를 출력한다.
    /// 표정이 지정된 경우 캐릭터 매니저에 캐릭터 교체를 요청한다.
    /// </summary>
    public async UniTask ShowDialogueAsync(DialogueData dialogueData)
    {
        //여기서 텍스트 및 사이즈가 이미 초기화 된 상태여야함

        typer.ClearText();
        dialoguePanel.SetActive(true);

        Color nameColor = Color.white;

        if (characterDB.TryGetValue(dialogueData.Speaker, out CharacterData speakerData))
        {
            if (ColorUtility.TryParseHtmlString(speakerData.NameColor, out nameColor))

            if (!string.IsNullOrEmpty(dialogueData.Expression))
            {
                await characterManager.SetCharacterAsync(dialogueData.Speaker, dialogueData.Expression);
            }
        }

        characterManager?.OnDialogueStart(dialogueData.Speaker);
        
        await typer.StartType(new TypingData
            (dialogueData.Text, 
            speakerData.DisplayName, 
            characterManager.GetCharacterPosition(dialogueData.Speaker), 
            nameColor, 
            dialogueData.Speaker == PLAYER_ID));

        
        characterManager?.OnDialogueEnd(dialogueData.Speaker);
    }

    /// <summary>선택지 UI 표시를 선택지 뷰에 위임한다.</summary>
    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onChoiceSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onChoiceSelected));
    }
    /// <summary>타이핑 중인 텍스트를 즉시 완성하고 캐릭터를 대사 종료 상태로 되돌린다.</summary>
    public void SkipTyping()
    {
        typer.OnScreenClick();
        characterManager.OnDialogueEnd();
    }

    /// <summary>트리거(연출/미니게임 등) 실행을 트리거 매니저에 위임하고 다음 대사 id를 반환받는다.</summary>
    public async UniTask<string> ExcuteTriggerAsync(TriggerData? trigger)
    {
        typer.ClearText();

        string id = await triggerManager.ExecuteTriggerAsync(trigger);

        return id;
    }

    /// <summary>대사창을 숨긴다.</summary>
    public void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }
}
