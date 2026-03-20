using System.Collections;
using UnityEngine;

public interface IDialogueCommand
{
    IEnumerator Execute();
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
