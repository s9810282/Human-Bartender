using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using VContainer;

/// <summary>
/// 대화 중 발생하는 트리거(연출, 미니게임 진입 등)를 DialogueCommandFactory로 커맨드 객체를 생성해 실행하는 매니저.
/// VContainer의 IObjectResolver로 커맨드에 의존성을 주입한다.
/// </summary>
public class DialogueTriggerManager : MonoBehaviour
{
    [Inject] IObjectResolver resolver;

    Action triggerCallBack = null;
    CancellationTokenSource _skipCts;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    /// <summary>
    /// 트리거 데이터에 맞는 커맨드를 생성/실행하고, 실행 결과로 반환된 다음 대사 id를 돌려준다.
    /// 해당하는 커맨드가 없으면 빈 문자열을 반환한다.
    /// </summary>
    public async UniTask<string> ExecuteTriggerAsync(TriggerData? trigger)
    {
        Debug.Log($"[트리거 시작] 타입: {trigger.Value.Type}");
        _skipCts = new CancellationTokenSource();

        IDialogueCommand command = DialogueCommandFactory.CreateCommand(trigger);
        if (command == null)
        {
            return "";
        }

        string nextId = "";

        if (resolver == null)
        {
            Debug.LogError("DI 에러] DialogueManager가 resolver를 받지 못했습니다!");
        }

        resolver.Inject(command);
        nextId = await command.ExecuteAsync(_skipCts.Token);
        
        return nextId;
    }

    /// <summary>실행 중인 트리거를 취소하고 다음 트리거를 위한 취소 토큰을 새로 준비한다.</summary>
    public void SkipTrigger()
    {
        _skipCts?.Cancel();
        _skipCts?.Dispose();
        _skipCts = new CancellationTokenSource();
    }
}
