using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public interface IDialoguePresenter
{
    UniTask ShowDialogueAsync(DialogueData dialogue, CancellationToken token);
    void ShowChoices(ChoiceData[] choices, Action<ChoiceData> onSelected);
    void SkipTyping();
    void HideDialogue();
    void ShowSystemAction();

    UniTask<string> ExecuteTriggerAsync(TriggerData? trigger);
}