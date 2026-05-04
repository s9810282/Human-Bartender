using UnityEngine;

public abstract class InteractiveNPCEntity : InteractiveEntity
{
    [SerializeField] protected InteractableEvent OnTrackedText;

    //고민해보기
    //data는 so같은 형태로 변경 필요
    //Runner 및 Presenter DI로 받아야함 동적 생성 이유.
    [SerializeField] protected DayDataSO dialogueData;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;


    protected bool isTalking = false;


    public override async void Interact(IInteractor player)
    {
        isTalking = true;
        player.State = EInteractorState.Interct;

        OnTrackedText?.Raise(this);

        runner.Bind(presenter);
        await runner.PlayAsync(dialogueData.dayData.Scenes[0].Dialogues);

        isTalking = false;
        player.State = EInteractorState.None;
    }
}