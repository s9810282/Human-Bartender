using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;

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
