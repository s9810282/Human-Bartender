using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

/// <summary>
/// 칵테일 제조 미니게임을 시작하는 커맨드.
/// IsSystemSwitch가 true이므로 실행 중 대화 화면이 숨겨진다.
/// 미니게임 완료 후 결과에 따른 다음 대화 id를 반환한다.
/// </summary>
public class StartCraftCommand : IDialogueCommand
{
    [Inject] private ICocktailCraft craftMgr;
    
    private string craft_event_id;
    


    public StartCraftCommand(TriggerDetailData data)
    {
        craft_event_id = data.CraftEventId;
        Logger.Log(data.CraftEventId);

        IsSystemSwitch = true;
    }

    public bool IsSystemSwitch { get; set; }

    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        string nextId = await craftMgr.StartCraftAsync(craft_event_id);
        return nextId;
    }
}
