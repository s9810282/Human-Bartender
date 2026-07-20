using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
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



/// <summary>
/// 대사 데이터(DialogueData) 배열을 순회하며 실제 진행(상태 전이, 트리거 실행, 분기, 선택지)을 담당하는
/// 대화 시스템의 핵심 컨트롤러. 화면 표시는 IDialoguePresenter(Bind로 주입)에 위임한다.
/// </summary>
public class DialogueRunner : MonoBehaviour
{
    [Inject] IPlayerDataReader PlayerData;
    private IDialoguePresenter presenter;

    private readonly Dictionary<string, DialogueData> currentDB = new();
    private DialogueData currentDialogue;
    private DialogueState currentState = DialogueState.Idle;

    private UniTaskCompletionSource completionSource;
    private CancellationTokenSource runnerCts;

    public DialogueState CurrentState => currentState;
    public bool IsRunning => currentState != DialogueState.Idle;

    /// <summary>대사 표시를 담당할 프레젠터를 연결한다. PlayAsync 호출 전에 반드시 호출되어야 한다.</summary>
    public void Bind(IDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }


    /// <summary>
    /// 대사 배열 하나(하나의 씬)를 처음부터 끝까지 재생한다. 이미 실행 중이거나 presenter 미바인딩, 빈 배열이면 무시.
    /// 완료/취소/예외 어느 경우든 finally에서 상태를 Idle로 되돌리고 리소스를 정리한다.
    /// </summary>
    public async UniTask PlayAsync(DialogueData[] dialogues, string startId = null, CancellationToken externalToken = default)
    {
        if (presenter == null)
        {
            Debug.LogError("[DialogueRunner] Presenter가 바인딩되지 않았습니다. Bind()를 먼저 호출하세요.");
            return;
        }

        if (dialogues == null || dialogues.Length == 0)
        {
            Debug.LogWarning("[DialogueRunner] 빈 dialogue 배열");
            return;
        }

        if (IsRunning)
        {
            Debug.LogWarning("[DialogueRunner] 이미 실행 중입니다.");
            return;
        }

        currentDB.Clear();
        foreach (var d in dialogues)
            currentDB[d.Id] = d;

        string firstId = string.IsNullOrEmpty(startId) ? dialogues[0].Id : startId;

        completionSource = new UniTaskCompletionSource();
        runnerCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalToken, this.GetCancellationTokenOnDestroy());

        try
        { 
            DialogueEvent(firstId);
            await completionSource.Task.AttachExternalCancellation(runnerCts.Token);
        }
        catch (OperationCanceledException)
        {
            

        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueRunner] PlayAsync 오류: {e}");
        }
        finally
        {
            currentState = DialogueState.Idle;
            currentDialogue = new();
            currentDB.Clear();

            try { presenter?.HideDialogue(); }
            catch (Exception e) { Debug.LogError($"[DialogueRunner] HideDialogue 오류: {e}"); }

            runnerCts?.Dispose();
            runnerCts = null;
            completionSource = null;
        }
    }

    
    /// <summary>진행 중인 대사 재생을 취소한다.</summary>
    public void Stop()
    {
        runnerCts?.Cancel();
    }

    /// <summary>
    /// 플레이어의 "다음으로" 입력 처리. 타이핑 중이면 즉시 완성(스킵), 입력 대기 중이면
    /// 선택지 -> 트리거(단일/복수) -> 다음 대사 -> 씬 종료 순으로 우선순위를 확인해 진행한다.
    /// </summary>
    public void OnAdvanceInput()
    {
        if (currentState == DialogueState.WaitingForTrigger
            || currentState == DialogueState.WaitingForChoice
            || currentState == DialogueState.Idle)
            return;

        if (currentState == DialogueState.Typing)
        {
            presenter.SkipTyping();
            currentState = DialogueState.WaitingForInput;
            return;
        }

        if (currentState == DialogueState.WaitingForInput)
        {
            if (currentDialogue.Choices != null && currentDialogue.Choices.Length > 0)
            {
                ShowChoices();
            }
            else if (currentDialogue.Trigger != null
                     && currentDialogue.Trigger.Value.Type != ETriggetType.None)
            {
                ExecuteTriggerAsync(currentDialogue.Trigger, currentDialogue.Next).Forget();
            }
            else if (currentDialogue.Triggers != null && currentDialogue.Triggers.Length > 0)
            {
                ExecuteTriggersAsync(currentDialogue.Triggers, currentDialogue.Next).Forget();
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

    /// <summary>id로 대사를 조회해 재생을 시작한다. id가 비어있거나 DB에 없으면 씬을 종료한다.</summary>
    private void DialogueEvent(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            EndScene();
            return;
        }

        if (!currentDB.ContainsKey(id))
        {
            Debug.LogWarning($"[DialogueRunner] dialogue id '{id}' 을(를) 찾을 수 없습니다.");
            EndScene();
            return;
        }

        PlayDialogueAsync(id).Forget();
    }


    /// <summary>
    /// 대사 타입별 분기 처리: System(연출 트리거만 실행), ConditionBranch(조건에 따라 다음 id 결정),
    /// Choice/ChoiceRoot(선택지 표시), 그 외 일반 대사는 프레젠터에 타이핑 표시를 요청한다.
    /// </summary>
    private async UniTaskVoid PlayDialogueAsync(string dialogueId)
    {
        try
        {
            currentDialogue = currentDB[dialogueId];

            if (currentDialogue.Type == EDialogueType.System)
            {
                presenter.ShowSystemAction();

                if (currentDialogue.Trigger != null
                     && currentDialogue.Trigger.Value.Type != ETriggetType.None)
                {
                    await ExecuteTriggerAsync(currentDialogue.Trigger, currentDialogue.Next);
                }
                else if(currentDialogue.Triggers != null && currentDialogue.Triggers.Length > 0)
                {
                    await ExecuteTriggersAsync(currentDialogue.Triggers, currentDialogue.Next);
                }
                else
                {
                    DialogueEvent(currentDialogue.Next);
                }

                return;
            }
            else if (currentDialogue.Type == EDialogueType.ConditionBranch)
            {
                NextConditions next = currentDialogue.Nextconditions.Value;
                string nextid = CheckCondition(next);
                DialogueEvent(nextid);

                return;
            }
            else if (currentDialogue.Type == EDialogueType.ChoiceRoot || currentDialogue.Type == EDialogueType.Choice)
            {
                if (currentDialogue.Choices != null && currentDialogue.Choices.Length > 0)
                {
                    ShowChoices();
                    return;
                }
            }

            currentState = DialogueState.Typing;
            await presenter.ShowDialogueAsync(currentDialogue, runnerCts.Token);
            currentState = DialogueState.WaitingForInput;
        }
        catch (OperationCanceledException)
        {

        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueRunner] PlayDialogueAsync 오류: {e}");
            EndScene();
        }
    }

    /// <summary>
    /// 조건 분기(ConditionBranch) 대사에서 사용. Stat 종류(호감도/스킬/재화/플래그)에 따라
    /// 해당하는 분기의 Goto id를 찾아 반환하고, 없으면 checkType.Default를 반환한다.
    /// </summary>
    public string CheckCondition(NextConditions checkType)
    {
        switch (checkType.Stat)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                EAffinityTier characterTier = PlayerData.GetCurCharacterAffinityTier(checkType.Character);

                foreach (var item in checkType.Branches)
                {
                    if (item.Tier == characterTier) return item.Goto;
                }

                return checkType.Default;

            case EConditionCheckType.Skill:
                return checkType.Default;

            case EConditionCheckType.Money:
                if (PlayerData.HasEnoughMoney(checkType.MinAmount)) 
                    return checkType.Branches[0].Goto;

                return checkType.Default;

            case EConditionCheckType.Flag:
                //추후 작업
                break;
        }

        return checkType.Default;
    }



    /// <summary>단일 트리거(연출/미니게임 등)를 실행하고, 결과로 받은 다음 id(없으면 fallback)로 진행한다.</summary>
    private async UniTask ExecuteTriggerAsync(TriggerData? trigger, string fallbackNextId)
    {
        currentState = DialogueState.WaitingForTrigger;

        string nextId = "";
        try
        {
            nextId = await presenter.ExecuteTriggerAsync(trigger);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception e)
        {
            Debug.LogError($"[DialogueRunner] ExecuteTriggerAsync 오류: {e}");
            EndScene();
            return;
        }

        if (string.IsNullOrEmpty(nextId))
            DialogueEvent(fallbackNextId);
        else
            DialogueEvent(nextId);
    }
    /// <summary>여러 트리거를 순차 실행한다. 마지막으로 받은 다음 id(없으면 fallback)로 진행한다.</summary>
    private async UniTask ExecuteTriggersAsync(TriggerData[] triggers, string fallbackNextId)
    {
        currentState = DialogueState.WaitingForTrigger;
        string nextId = null;

        foreach (var triggerData in triggers)
        {
            try
            {
                nextId = await presenter.ExecuteTriggerAsync(triggerData);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DialogueRunner] ExecuteTriggerAsync 오류: {e}");
                EndScene();
                return;
            }
        }

        if (string.IsNullOrEmpty(nextId))
            DialogueEvent(fallbackNextId);
        else
            DialogueEvent(nextId);
    }


    /// <summary>상태를 선택지 대기로 전환하고 프레젠터에 선택지 표시를 요청한다.</summary>
    private void ShowChoices()
    {
        currentState = DialogueState.WaitingForChoice;
        presenter.ShowChoices(currentDialogue.Choices, OnChoiceSelected);
    }
    /// <summary>선택지 선택 콜백. 선택된 항목의 Next id로 진행한다.</summary>
    private void OnChoiceSelected(ChoiceData data)
    {
        DialogueEvent(data.Next);
    }
    /// <summary>씬 종료 처리: 상태를 Idle로 되돌리고 PlayAsync의 대기를 완료시킨다.</summary>
    private void EndScene()
    {
        currentState = DialogueState.Idle;
        presenter.EndScene();
        completionSource?.TrySetResult();
    }
}