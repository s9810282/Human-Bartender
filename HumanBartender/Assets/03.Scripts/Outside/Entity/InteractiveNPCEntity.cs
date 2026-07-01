using System.Collections.Generic;
using UnityEngine;

public abstract class InteractiveNPCEntity : InteractiveEntity
{
    [SerializeField] protected ITrackedbleEvent OnTrackedText;

    [SerializeField] protected List<FlowData> flows;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;


    protected bool isTalking = false;

    int curFlowIndex = 0;
    ESelectionType selectionType = ESelectionType.Random;

    public void InjectDialogue(List<FlowData> data, ESelectionType selection)
    {
        if (data.Count == 0)
            Logger.LogError($"Select Flow Data is Null");

        flows = data;
        selectionType = selection;
    }


    public override async void Interact(IInteractor player)
    {
        isTalking = true;
        isInteracting = true;
        player.State = EInteractorState.Interct;

        OnInteracted?.Raise(this);
        OnTrackedText?.Raise(this);

        player.InteractorEvent();

        if (selectionType == ESelectionType.Conditional)
            curFlowIndex = 0;

        runner.Bind(presenter);
        await runner.PlayAsync(flows[curFlowIndex].Dialogues);

        isTalking = false;
        isInteracting = false;
        player.State = EInteractorState.None;

        curFlowIndex++;
        curFlowIndex %= flows.Count;

        OnRefreshCondition?.Raise(new Void());
    }
}