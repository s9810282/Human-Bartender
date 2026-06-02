using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;


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

        GameManager.Instance.GameFlow = EGameFlow.Attendance;
        SceneTransitionManager.Instance.LoadScene("Outside");
        return "";
    }
}
