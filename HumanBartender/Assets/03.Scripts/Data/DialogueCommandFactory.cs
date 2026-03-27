using System.Collections;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;


public interface IDialogueCommand
{
    public bool IsSystemSwitch { get; set; }
    UniTask<string> ExecuteAsync();
}

public static class DialogueCommandFactory
{
    public static IDialogueCommand CreateCommand(TriggerData? triggerData)
    {
        if (string.IsNullOrEmpty(triggerData.Value.Type)) return null;

        switch (triggerData.Value.Type)
        {
            case "customer_enter":
                return new CustomerEnterCommand(triggerData.Value.Data);

            case "start_craft":
                return new StartCraftCommand(triggerData.Value.Data);

            default:
                Debug.LogWarning($"[Factory] 정의되지 않은 트리거 타입: {triggerData.Value.Type}");
                return null;
        }
    }
}
