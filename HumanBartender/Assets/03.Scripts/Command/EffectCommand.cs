using Cysharp.Threading.Tasks;
using VContainer;

public class EffectCommand : IDialogueCommand
{
    [Inject] private IEffectPlayer effectPlayer;

    private string effectType = "";
    private float duration = 0.3f;
    private string next = "";

    public EffectCommand(TriggerDetailData data, string id = "")
    {
        effectType = data.EffectType;
        duration = data.Duration.Value;
        next = id;
    }

    bool IDialogueCommand.IsSystemSwitch { get; set; }

    async UniTask<string> IDialogueCommand.ExecuteAsync()
    {
        Logger.Log(effectPlayer == null);
        await effectPlayer.PlayEffectAsync(effectType, duration);
        return next;
    }
}
