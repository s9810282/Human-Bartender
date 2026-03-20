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
    [SerializeField] CharacterDataSO characterData;
    [Inject] IObjectResolver resolver;

    [Header("UI Components")]
    public GameObject dialoguePanel;      // 대화창 전체 패널
    public TextMeshProUGUI nameTMPText;      // 이름 텍스트
    public TextMeshProUGUI dialogueTMPText;  // 대사 텍스트

    public Text nameText;
    public Text dialogueText;

    public SpriteRenderer portraitImage;           // 캐릭터 초상화 이미지
    public GameObject choicesPanel;
    public List<ChoicePanel> choicePanels = new();


    #region Data Field
    private Dictionary<string, CharacterData> characterDB = new Dictionary<string, CharacterData>();
    private Dictionary<string, DialogueData> currentDialogueDB = new Dictionary<string, DialogueData>();

    private SceneData currentSceneData;
    #endregion

    private DialogueData currentDialogue;
    private Coroutine typingCoroutine;
    private Coroutine triggerCoroutine;
    private bool isTyping = false;

    [SerializeField] private DialogueState currentState = DialogueState.Idle;
    private WaitForSeconds defaultTypingDelay = new WaitForSeconds(0.05f);

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
        characterDB.Clear();
        foreach (var character in characterData.characterData.characters)
        {
            characterDB[character.id] = character;
        }

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
            dialoguePanel.SetActive(false);

            if (!string.IsNullOrEmpty(currentDialogue.trigger.type)) yield return StartCoroutine(ExecuteTriggerCoroutine(currentDialogue.trigger));
            if (!string.IsNullOrEmpty(currentDialogue.next)) StartCoroutine(PlayDialogue(currentDialogue.next));

            yield break;
        }

        dialoguePanel.SetActive(true);


        //캐릭터에 따른 name Color 및 캐릭터 이미지 적용
        if (characterDB.TryGetValue(currentDialogue.speaker, out CharacterData speakerData))
        {
            if (ColorUtility.TryParseHtmlString(speakerData.name_color, out Color parsedColor))
            {
                nameText.color = parsedColor;
            }

            nameText.text = speakerData.display_name;

            //캐릭터 리소스 처리 (null 체크)
            if (!string.IsNullOrEmpty(currentDialogue.expression))
            {
                portraitImage.gameObject.SetActive(true);
                Logger.Log($"캐릭터 리소스 {currentDialogue.expression} 표기");

                // TODO: Resources나 Addressables에서 스프라이트 로드
                // portraitImage.sprite = Resources.Load<Sprite>($"Portraits/{speakerData.id}_{currentDialogue.expression}");
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
            }
        }

        dialogueText.fontStyle = FontStyle.Normal;
        dialogueTMPText.fontStyle = FontStyles.Normal;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        yield return typingCoroutine = StartCoroutine(TypeSentence(currentDialogue.text));
    }



    private IEnumerator ExecuteTriggerCoroutine(TriggerData trigger)
    {
        currentState = DialogueState.WaitingForTrigger; // 입력 잠금
        GameStateManager.Instance.CurrentGameState = GameState.Trigger;

        Debug.Log($"[트리거 시작] 타입: {trigger.type}");

        yield return null;
        yield return new WaitForSeconds(1.0f); // 임시 대기 시간

        IDialogueCommand command = DialogueCommandFactory.CreateCommand(trigger);
        if (command != null)
        {
            if (resolver == null)
            {
                Debug.LogError("DI 에러] DialogueManager가 resolver를 받지 못했습니다!");
                yield break;
            }

            resolver.Inject(command);
            currentState = DialogueState.PlayingTrigger; // 입력 잠금
            StartCoroutine(command.Execute());

            yield return new WaitUntil(() => GameStateManager.Instance.CurrentGameState == GameState.Play);
        }

        triggerCoroutine = null;
    }

    #region Typing
    private IEnumerator TypeSentenceTMP(string rawSentence)
    {
        if (!(rawSentence.Length > 0)) yield break;

        currentState = DialogueState.Typing;

        string cleanSentence = rawSentence;
        Dictionary<int, float> delayDict = new Dictionary<int, float>();
        Regex tagRegex = new Regex(@"<(\d+)>");
        MatchCollection matches = tagRegex.Matches(rawSentence);

        int offset = 0;
        foreach (Match match in matches)
        {
            int delayMs = int.Parse(match.Groups[1].Value);
            int targetIndex = match.Index - offset;
            delayDict[targetIndex] = delayMs / 1000f;

            cleanSentence = cleanSentence.Remove(targetIndex, match.Length);
            offset += match.Length;
        }

        dialogueTMPText.text = cleanSentence;
        dialogueTMPText.maxVisibleCharacters = 0;
        dialogueTMPText.ForceMeshUpdate();
        int totalVisibleChars = dialogueTMPText.textInfo.characterCount;

        for (int i = 0; i <= totalVisibleChars; i++)
        {
            dialogueTMPText.maxVisibleCharacters = i;

            if (delayDict.ContainsKey(i))
            {
                yield return new WaitForSeconds(delayDict[i]);
            }

            if (i < totalVisibleChars)
            {
                yield return defaultTypingDelay; // 캐싱된 WaitForSeconds 사용
            }
        }

        // 타이핑 종료 시 입력 대기 상태로 전환
        currentState = DialogueState.WaitingForInput;
    }
    public void OnScreenClickedTMP()
    {
        // 트리거 연출 중이거나 선택지를 고르는 중이면 클릭 무시
        if (currentState == DialogueState.WaitingForTrigger || currentState == DialogueState.WaitingForChoice) return;

        if (currentState == DialogueState.Typing)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            dialogueTMPText.maxVisibleCharacters = dialogueTMPText.textInfo.characterCount;
            currentState = DialogueState.WaitingForInput;
        }
        else if (currentState == DialogueState.WaitingForInput)
        {
            if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
            {
                ShowChoices();
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


    #region Legacy Text
    private string currentCleanText = "";
    private IEnumerator TypeSentence(string rawSentence)
    {
        currentState = DialogueState.Typing;

        string cleanSentence = rawSentence;
        Dictionary<int, float> delayDict = new Dictionary<int, float>();
        Regex tagRegex = new Regex(@"<(\d+)>");
        MatchCollection matches = tagRegex.Matches(rawSentence);

        int offset = 0;
        foreach (Match match in matches)
        {
            int delayMs = int.Parse(match.Groups[1].Value);
            int targetIndex = match.Index - offset;
            delayDict[targetIndex] = delayMs / 1000f;

            cleanSentence = cleanSentence.Remove(targetIndex, match.Length);
            offset += match.Length;
        }

        currentCleanText = cleanSentence;

        dialogueText.text = "";
        int totalChars = cleanSentence.Length;

        for (int i = 0; i <= totalChars; i++)
        {
            dialogueText.text = cleanSentence.Substring(0, i);

            if (delayDict.ContainsKey(i))
            {
                yield return new WaitForSeconds(delayDict[i]);
            }

            if (i < totalChars)
            {
                yield return defaultTypingDelay;
            }
        }

        currentState = DialogueState.WaitingForInput;
    }
    public void OnScreenClicked()
    {
        if (currentState == DialogueState.WaitingForTrigger || currentState == DialogueState.WaitingForChoice) return;

        if (currentState == DialogueState.Typing)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);

            dialogueText.text = currentCleanText;
            currentState = DialogueState.WaitingForInput;
        }
        else if (currentState == DialogueState.WaitingForInput)
        {
            if (currentDialogue.choices != null && currentDialogue.choices.Length > 0)
            {
                ShowChoices();
            }
            else if (!string.IsNullOrEmpty(currentDialogue.trigger.type) && typingCoroutine != null)
            {
                triggerCoroutine = StartCoroutine(ExecuteTriggerCoroutine(currentDialogue.trigger));
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
    #endregion
    #endregion


    private void ShowChoices()
    {
        currentState = DialogueState.WaitingForChoice;
        
        // TODO: currentDialogue.choices 배열을 순회하며 선택지 버튼(프리팹) 생성 및 텍스트 할당
        Debug.Log("선택지 UI 표시 중...");

        choicesPanel.SetActive(true);

        ChoiceData[] choiceDatas = currentDialogue.choices;

        for(int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].SetActive(false);

        for(int i = 0; i < choiceDatas.Length; i++)
        {
            choicePanels[i].SetPanelText(choiceDatas[i].text);
            choicePanels[i].SetActive(true);
        }
    }
    public void ChoiceSelect(int num)
    {
        currentState = DialogueState.Idle;

        ChoiceData choiceData = currentDialogue.choices[num];

        choicesPanel.SetActive(false);

        for (int i = 0; i < choicePanels.Count; i++)
            choicePanels[i].ResetPanel();
       
        StartCoroutine(PlayDialogue(choiceData.next));
    }
    private void EndScene()
    {
        currentState = DialogueState.Idle;
        dialoguePanel.SetActive(false);
        Debug.Log("대화 씬이 모두 종료되었습니다.");
    }
}
