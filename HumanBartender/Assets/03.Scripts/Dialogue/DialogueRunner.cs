using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;



public class DialogueRunner : MonoBehaviour
{
    private IDialoguePresenter presenter;

    private readonly Dictionary<string, DialogueData> currentDB = new();
    private DialogueData currentDialogue;
    private DialogueState currentState = DialogueState.Idle;

    private UniTaskCompletionSource completionSource;
    private CancellationTokenSource runnerCts;

    public DialogueState CurrentState => currentState;
    public bool IsRunning => currentState != DialogueState.Idle;

    public void Bind(IDialoguePresenter presenter)
    {
        this.presenter = presenter;
    }




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

    
    public void Stop()
    {
        runnerCts?.Cancel();
    }


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
                     && !string.IsNullOrEmpty(currentDialogue.Trigger.Value.Type))
            {
                ExecuteTriggerAsync(currentDialogue.Trigger, currentDialogue.Next).Forget();
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

    private async UniTaskVoid PlayDialogueAsync(string dialogueId)
    {
        try
        {
            currentDialogue = currentDB[dialogueId];

            if (currentDialogue.Type == EDialogueType.System)
            {
                presenter.ShowSystemAction();

                if (currentDialogue.Trigger != null
                    && !string.IsNullOrEmpty(currentDialogue.Trigger.Value.Type))
                {
                    await ExecuteTriggerAsync(currentDialogue.Trigger, currentDialogue.Next);
                }
                else
                {
                    DialogueEvent(currentDialogue.Next);
                }
                return;
            }
            else if (currentDialogue.Type == EDialogueType.ChoiceRoot)
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

    private void ShowChoices()
    {
        currentState = DialogueState.WaitingForChoice;
        presenter.ShowChoices(currentDialogue.Choices, OnChoiceSelected);
    }
    private void OnChoiceSelected(ChoiceData data)
    {
        DialogueEvent(data.Next);
    }
    private void EndScene()
    {
        currentState = DialogueState.Idle;
        presenter.EndScene();
        completionSource?.TrySetResult();
    }
}