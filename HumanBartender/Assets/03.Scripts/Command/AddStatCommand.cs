using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;


/// <summary>
/// 특정 캐릭터의 호감도를 지정한 값만큼 더하는 커맨드.
/// SetStat과 달리 현재 값에 누적된다.
/// </summary>
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
