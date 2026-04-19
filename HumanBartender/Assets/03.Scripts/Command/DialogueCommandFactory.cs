using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;


public interface IDialogueCommand
{
    public bool IsSystemSwitch { get; set; }
    UniTask<string> ExecuteAsync(CancellationToken ct);
}

public static class DialogueCommandFactory
{
    public static IDialogueCommand CreateCommand(TriggerData? triggerData)
    {
        if (string.IsNullOrEmpty(triggerData.Value.Type)) return null;

        switch (triggerData.Value.Type)
        {
            case "effect":
                return new EffectCommand(triggerData.Value.Data);

            case "start_craft":
                return new StartCraftCommand(triggerData.Value.Data);

            case "customer_enter":
                return new CustomerEnterCommand(triggerData.Value.Data);

            case "customer_exit":
                return new CustomerExitCommand(triggerData.Value.Data);

            case "play_sideview_anim":
                return new PlaySideviewAnimCommand(triggerData.Value.Data);

            default:
                Debug.LogWarning($"[Factory] 정의되지 않은 트리거 타입: {triggerData.Value.Type}");
                return null;
        }
    }
}
