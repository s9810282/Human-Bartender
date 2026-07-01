using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.TextCore.Text;

/// <summary>
/// 실외(길거리 등) 씬에서 사용하는 IDialoguePresenter 구현체.
/// 실내(DialogueSceneDirector)와 달리 캐릭터/배경 연출 없이 텍스트/선택지/트리거만 처리하는 단순화된 버전.
/// </summary>
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


    /// <summary>트리거 실행을 트리거 매니저에 위임하고 다음 대사 id를 반환받는다.</summary>
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

    /// <summary>선택지 UI 표시를 선택지 뷰에 위임한다.</summary>
    public void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onSelected)
    {
        choiceManager.ShowChoice(new ChoiceSelectData(choices, onSelected));
    }

    /// <summary>대사창을 열고 타이핑 효과로 텍스트를 표시한다. (인수인계 메모) 화자 위치 미지정으로 Vector2.zero 고정, 캐릭터 DB 조회는 미구현(TODO).</summary>
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

    /// <summary>타이핑 중인 텍스트를 즉시 완성한다.</summary>
    public void SkipTyping()
    {
        typer.OnScreenClick();
    }

    /// <summary>연출/시스템 액션 진행을 위해 대화창을 숨긴다.</summary>
    public void ShowSystemAction()
    {
        dialoguePanel.SetActive(false);
    }

    /// <summary>씬 종료 시 텍스트를 정리한다.</summary>
    public void EndScene()
    {
        typer.ClearText();
    }
}
