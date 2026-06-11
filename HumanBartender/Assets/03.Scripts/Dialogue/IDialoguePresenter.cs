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
    void EndScene();

    UniTask<string> ExecuteTriggerAsync(TriggerData? trigger);
}