using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// 대화 씬의 실제 UI(텍스트/선택지/캐릭터/배경) 연출을 담당하는 컴포넌트.
/// DialogueRunner가 IDialoguePresenter로 호출하는 Play(Bar) 씬 전용 구현체.
/// </summary>
public class DialogueSceneDirector : MonoBehaviour, IDialoguePresenter
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
    public async UniTask ShowDialogueAsync(DialogueData dialogueData, CancellationToken token)
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
    public async UniTask<string> ExecuteTriggerAsync(TriggerData? trigger)
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

    /// <summary>씬 종료 시 텍스트를 정리한다.</summary>
    public void EndScene()
    {
        typer.ClearText();
    }
    /// <summary>
    /// Bar 씬에서는 외부 선택지를 쓰지 않으므로 빈 메서드로 유지
    /// </summary>
    public void ShowOutsideChoices(NewStreetOptionData[] options, Action<NewStreetOptionData> onSelected)
    {
        // Bar 씬에선 외부 선택지를 사용하지 않으므로 아무것도 하지 않음
    }

    /// <summary>
    /// 일반 대사 출력으로 우회하거나 최소한의 텍스트만 출력
    /// </summary>
    public async UniTask ShowDialogueAsync(string actor, string text, string expression, CancellationToken token)
    {
        typer.ClearText();
        dialoguePanel.SetActive(true);

        // 단순 텍스트 타이핑만 수행
        await typer.StartType(new TypingData(text, actor, Vector3.zero, Color.white, actor == PLAYER_ID));
    }
}
