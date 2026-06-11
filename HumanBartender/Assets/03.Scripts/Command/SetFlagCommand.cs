using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using VContainer;

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
