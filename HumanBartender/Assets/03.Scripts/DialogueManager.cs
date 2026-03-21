using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
    PlayingTrigger,
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
        if (dayScripteData.dayData.scenes.Length > 0)
        {
            LoadScene(dayScripteData.dayData.scenes[0]);
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

        foreach (var dialogue in sceneData.dialogues)
        {
            currentDialogueDB[dialogue.id] = dialogue;
        }

        Debug.Log($"[씬 로드 완료] 손님: {currentSceneData.customer_display_name}");

        if (currentSceneData.dialogues.Length > 0)
        {
            GameStateManager.Instance.CurrentGameState = GameState.Play;
            StartCoroutine(PlayDialogue(currentSceneData.dialogues[0].id));
        }
    }

    public void DialogueEvent(string id)
    {
        StartCoroutine(PlayDialogue(id));
    }


    /// <summary>
    /// Dialogue 실행
    /// - 데이터에 없을 시 return
    /// 
    /// </summary>
    /// <param name="dialogueId"></param>
    private IEnumerator PlayDialogue(string dialogueId)
    {
        if (!currentDialogueDB.ContainsKey(dialogueId)) yield break;

        currentDialogue = currentDialogueDB[dialogueId];

        // type이 system일 때 처리
        if (currentDialogue.type == "system")
        {
            sceneDirector.ShowSystemAction();

            if (!string.IsNullOrEmpty(currentDialogue.trigger.type)) 
                ExecuteTriggerCoroutine(currentDialogue.trigger);

            yield break;
        }

        currentState = DialogueState.Typing;
        sceneDirector.ShowDialogue(currentDialogue , () => CallBackTypingComplete());
    }


    public void CallBackTypingComplete()
    {
        currentState = DialogueState.WaitingForInput;
    }

    public void CallBackTriggerComplete()
    {
        currentState = DialogueState.WaitingForInput;
    }


    private void ExecuteTriggerCoroutine(TriggerData trigger)
    {
        currentState = DialogueState.WaitingForTrigger; // 입력 잠금
        
        Debug.Log($"[트리거 시작] 타입: {trigger.type}");

        sceneDirector.PlayTrigger(trigger, () => CallBackTriggerComplete());
    }


 

    #region Choice
    
    private void ShowChoices()
    {
        currentState = DialogueState.WaitingForChoice;
        sceneDirector.ShowChoices(currentDialogue.choices, (ChoiceData n) => ChoiceSelect(n));
    }
    public void ChoiceSelect(ChoiceData data)
    {
        currentState = DialogueState.Idle;
        StartCoroutine(PlayDialogue(data.next));
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
        }
        else if (currentState == DialogueState.WaitingForInput)
        {
            if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
            {
                ShowChoices();
            }
            else if (!string.IsNullOrEmpty(currentDialogue.trigger.type))
            {
                ExecuteTriggerCoroutine(currentDialogue.trigger);
            }
            else if (!string.IsNullOrEmpty(currentDialogue.next))
            {
                StartCoroutine(PlayDialogue(currentDialogue.next));
            }
            else
            {
                EndScene();
            }
        }
    }
}
