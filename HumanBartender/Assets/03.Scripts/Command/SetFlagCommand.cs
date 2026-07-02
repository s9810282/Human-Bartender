using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;

/// <summary>
/// 플레이어 데이터에 불리언 플래그를 설정하는 커맨드.
/// 분기 조건이나 이벤트 발생 여부 추적에 사용된다.
/// </summary>
public class SetFlagCommand : IDialogueCommand
{
    [Inject] IPlayerDataWriter playerData;

    private string id;
    private bool value;

    public SetFlagCommand(TriggerDetailData data)
    {
        id = data.FlagId;
        value = data.BValue;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync(CancellationToken cancellationToken)
    {
        playerData.AddFlag(id, value);
        
        return "";
    }
}
