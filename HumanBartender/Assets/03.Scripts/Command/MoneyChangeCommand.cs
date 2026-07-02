using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;

/// <summary>
/// 플레이어 보유 금액을 변경하는 커맨드.
/// 양수이면 증가, 음수이면 감소한다.
/// </summary>
public class MoneyChangeCommand : IDialogueCommand
{
    [Inject] IPlayerDataWriter playerData;

    private int value;

    public MoneyChangeCommand(TriggerDetailData data)
    {
        value = data.Amount;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync(CancellationToken cancellationToken)
    {
        playerData.AddMoney(value);
        return "";
    }
}
