using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;


/// <summary>
/// 하루를 마무리하고 Outside 씬으로 전환하는 커맨드.
/// GameFlow를 CommuteOut으로 설정한 뒤 씬을 로드한다.
/// </summary>
public class DayEndCommand : IDialogueCommand
{
    [Inject] IPlayerDataWriter playerData;

    private string character;
    private int value;

    public DayEndCommand(TriggerDetailData data)
    {
        character = data.CharacterId;
        value = data.Value;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync(CancellationToken cancellationToken)
    {
        GameStateManager.Instance.GameFlow = EGameFlow.CommuteOut;
        SceneTransitionManager.Instance.LoadScene("Outside");
        return "";
    }
}
