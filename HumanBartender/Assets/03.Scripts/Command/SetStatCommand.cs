using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;


/// <summary>
/// 특정 캐릭터의 호감도를 지정한 값으로 직접 설정하는 커맨드.
/// AddStat과 달리 현재 값에 관계없이 절대값으로 덮어쓴다.
/// </summary>
public class SetStatCommand : IDialogueCommand
{
    [Inject] IPlayerDataWriter playerData;

    private string character;
    private int value;

    public SetStatCommand(TriggerDetailData data)
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
