using Cysharp.Threading.Tasks;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using VContainer;


public enum DialogueState
{
    Idle,
    WaitingForTrigger,
    Typing,
    WaitingForInput,
    WaitingForChoice,
    CompleteTrigger,
}



public class DialogueManager : MonoBehaviour
{
    [SerializeField] DayDataSO dayScripteData;
    
    [Inject] IObjectResolver resolver;

    [SerializeField] DialogueSceneDirector sceneDirector;

    #region Data Field

    private Dictionary<string, DialogueData> currentDialogueDB = new Dictionary<string, DialogueData>();

    private DialogueData currentDialogue;
    private SceneData currentSceneData;
    #endregion

    [SerializeField] private DialogueState currentState = DialogueState.Idle;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(!GameStateManager.Instance.IsStart)
            InitSystem();

        GameStateManager.Instance.IsStart = true;
    }

    // Update is called once per frame
    void Update()
    {

    }

    /// <summary>
    ///  최초 게임 플레이 씬 진입 시 호출하여 게임을 시작 함. 
    ///  캐릭터 데이터를 dic로 변환하여 보관 후 0번 인덱스 = 가장 처음 씬으로 작업.
    /// </summary>
    public void InitSystem()
    {
        if (dayScripteData.dayData.Scenes.Length > 0)
        {
            LoadScene(dayScripteData.dayData.Scenes[0]);
        }
    }

    /// <summary>
    /// 특정 씬(손님 방문 등)이 시작될 때 호출함.
    /// 기본적으로 Scene 단위로 화면이 진행되며 연출들 또한 sceneData 안에 있을 듯함.
    /// 해당하는 SceneData의 Dialogue를 따로 보관하여 접근 용이
    /// </summary>
    public void LoadScene(SceneData sceneData)
    {
        currentSceneData = sceneData;
        currentDialogueDB.Clear();

        foreach (var dialogue in sceneData.Dialogues)
        {
            currentDialogueDB[dialogue.Id] = dialogue;
        }

        Debug.Log($"[씬 로드 완료] 손님: {currentSceneData.Customer}");

        if (currentSceneData.Dialogues.Length > 0)
        {
            GameStateManager.Instance.CurrentGameState = GameState.Play;
            DialogueEvent(currentSceneData.Dialogues[0].Id);
        }
    }

    public void DialogueEvent(string id)
    {
        PlayDialogue(id).Forget();
    }


    /// <summary>
    /// Dialogue 실행
    /// - 데이터에 없을 시 return
    /// 
    /// </summary>
    /// <param name="dialogueId"></param>
    public async UniTaskVoid PlayDialogue(string dialogueId)
    {
        if (!currentDialogueDB.ContainsKey(dialogueId)) return;

        currentDialogue = currentDialogueDB[dialogueId];

        // type이 system일 때 처리
        if (currentDialogue.Type == "system")
        {
            sceneDirector.ShowSystemAction();

            if (!string.IsNullOrEmpty(currentDialogue.Trigger.Value.Type)) 
                ExecuteTriggerAsync(currentDialogue.Trigger).Forget();

            await sceneDirector.ShowDialogueAsync(currentDialogue);
            return;
        }

        currentState = DialogueState.Typing;

        await sceneDirector.ShowDialogueAsync(currentDialogue);

        currentState = DialogueState.WaitingForInput;

        return;
    }


    public async UniTask ExecuteTriggerAsync(TriggerData? trigger)
    {
        currentState = DialogueState.WaitingForTrigger; // 입력 잠금
        
        Debug.Log($"[트리거 시작] 타입: {trigger.Value.Type}");

        string id = await sceneDirector.ExcuteTriggerAsync(trigger);
        
        if (id == "")
        {
            DialogueEvent(currentDialogue.Next);
        }
        else
        {
            DialogueEvent(id);
            //미니게임 결과 등에 따른 next 처리.
        }

        return;
    }


 

    #region Choice
    
    private void ShowChoices()
    {
        currentState = DialogueState.WaitingForChoice;
        sceneDirector.ShowChoices(currentDialogue.Choices, (ChoiceData n) => ChoiceSelect(n));
    }
    public void ChoiceSelect(ChoiceData data)
    {
        currentState = DialogueState.Idle;
        DialogueEvent(data.Next);
    }

    #endregion


    private void EndScene()
    {
        currentState = DialogueState.Idle;
        Debug.Log("대화 씬이 모두 종료되었습니다.");
    }


    public void OnScreenClicked()
    {
        if (currentState == DialogueState.WaitingForTrigger || currentState == DialogueState.WaitingForChoice) return;

        if (currentState == DialogueState.Typing)
        {
            sceneDirector.SkipTyping();
            currentState = DialogueState.WaitingForInput;
        }
        else if (currentState == DialogueState.WaitingForInput)
        {
            if (currentDialogue.Choices != null && currentDialogue.Choices.Length > 0)
            {
                ShowChoices();
            }
            else if (currentDialogue.Trigger != null)
            {
                if (!string.IsNullOrEmpty(currentDialogue.Trigger.Value.Type))
                    ExecuteTriggerAsync(currentDialogue.Trigger).Forget();
            }
            else if (!string.IsNullOrEmpty(currentDialogue.Next))
            {
                DialogueEvent(currentDialogue.Next);
            }
            else
            {
                EndScene();
            }
        }
    }
}
