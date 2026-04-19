using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

public class EffectCommand : IDialogueCommand
{
    [Inject] private IEffectPlayer effectPlayer;

    private string effectType = "";
    private float duration = 0.3f;
    

    public EffectCommand(TriggerDetailData data)
    {
        effectType = data.EffectType;
        duration = data.Duration.Value;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync(CancellationToken cancellationToken)
    {
        await effectPlayer.PlayEffectAsync(effectType, duration);
        return "";
    }
}
