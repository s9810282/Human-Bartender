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
    public static IDialogueCommand CreateCommand(TriggerData triggerData)
    {
        if (string.IsNullOrEmpty(triggerData.type)) return null;

        switch (triggerData.type)
        {
            case "customer_enter":
                return new CustomerEnterCommand(triggerData.data);

            case "start_craft":
                return new StartCraftCommand(triggerData.data);

            default:
                Debug.LogWarning($"[Factory] 정의되지 않은 트리거 타입: {triggerData.type}");
                return null;
        }
    }
}
