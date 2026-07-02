using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

/// <summary>
/// 화면 이펙트(페이드인/아웃, 플래시 등)를 재생하는 커맨드.
/// IEffectPlayer를 통해 지정된 EEffectType 이펙트를 duration 초 동안 실행한다.
/// </summary>
public class EffectCommand : IDialogueCommand
{
    [Inject] private IEffectPlayer effectPlayer;

    private EEffectType effectType = EEffectType.None;
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
