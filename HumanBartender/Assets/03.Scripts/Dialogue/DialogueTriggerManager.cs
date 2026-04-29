using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using VContainer;

public class DialogueTriggerManager : MonoBehaviour
{
    [Inject] IObjectResolver resolver;

    Action triggerCallBack = null;
    CancellationTokenSource _skipCts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }


    public async UniTask<string> ExecuteTriggerAsync(TriggerData? trigger)
    {
        Debug.Log($"[트리거 시작] 타입: {trigger.Value.Type}");

        _skipCts = new CancellationTokenSource();

        IDialogueCommand command = DialogueCommandFactory.CreateCommand(trigger);
        string nextId = "";

        if (command != null)
        {
            if (resolver == null)
            {
                Debug.LogError("DI 에러] DialogueManager가 resolver를 받지 못했습니다!");
            }

            resolver.Inject(command);
            nextId = await command.ExecuteAsync(_skipCts.Token);
        }

        return nextId;
    }

    public void SkipTrigger()
    {
        _skipCts?.Cancel();
        _skipCts?.Dispose();
        _skipCts = new CancellationTokenSource();
    }
}
