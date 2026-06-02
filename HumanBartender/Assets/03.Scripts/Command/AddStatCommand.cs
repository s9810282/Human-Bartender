using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;


public class AddStatCommand : IDialogueCommand
{
    [Inject] IPlayerDataWriter playerData;

    private string character;
    private int value;

    public AddStatCommand(TriggerDetailData data)
    {
        character = data.CharacterId;
        value = data.Value;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync(CancellationToken cancellationToken)
    {
        playerData.SetCharacterAffinityAmount(character, value);
        return "";
    }
}
