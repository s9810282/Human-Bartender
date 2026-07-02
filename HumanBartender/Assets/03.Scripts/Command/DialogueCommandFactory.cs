using UnityEngine;

/// <summary>
/// TriggerData 타입에 따라 대응하는 IDialogueCommand 구현체를 생성하는 정적 팩토리.
/// </summary>
public static class DialogueCommandFactory
{
    public static IDialogueCommand CreateCommand(TriggerData? triggerData)
    {
        switch (triggerData.Value.Type)
        {
            case ETriggetType.None:
                return null;

            case ETriggetType.Effect:
                return new EffectCommand(triggerData.Value.Data);

            case ETriggetType.StartCraft:
                return new StartCraftCommand(triggerData.Value.Data);

            case ETriggetType.CustomerEnter:
                return new CustomerEnterCommand(triggerData.Value.Data);

            case ETriggetType.CustomerExit:
                return new CustomerExitCommand(triggerData.Value.Data);

            case ETriggetType.StartCutScene:
                return new StartCutSceneCommand(triggerData.Value.Data);

            case ETriggetType.SetStat:
                return new SetStatCommand(triggerData.Value.Data);

            case ETriggetType.AddStat:
                return new AddStatCommand(triggerData.Value.Data);

            case ETriggetType.ApplyEffect:
                return new AddStatCommand(triggerData.Value.Data);
                
            case ETriggetType.CharacterAction:
                return null;

            case ETriggetType.Day_End:
                return new DayEndCommand(triggerData.Value.Data);

            case ETriggetType.SetFlag:
                return new SetFlagCommand(triggerData.Value.Data);

            case ETriggetType.MoneyChange:
                return new MoneyChangeCommand(triggerData.Value.Data);

            default:
                Debug.LogWarning($"[Factory] 정의되지 않은 트리거 타입: {triggerData.Value.Type}");
                return null;
        }
    }
}
