using UnityEngine;

public class InteractiveObjectEntity : InteractiveEntity
{
    [SerializeField] protected InteractableEvent OnTrackedText;

    //고민해보기
    //data는 so같은 형태로 변경 필요
    //Runner 및 Presenter DI로 받아야함 동적 생성 이유.
    
    [SerializeField] protected string object_Id;
    [SerializeField] protected OutsideObjectDataSO obejctData;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;

    protected bool isTalking = false;


    public override async void Interact(IInteractor player)
    {
        isTalking = true;
        isInteracting = true;
        player.State = EInteractorState.Interct;

        ///dialogueData.dayData.InteractData.Label;

        OnInteracted?.Raise(this);
        OnTrackedText?.Raise(this);


        //그니까 여기서 selectionType 값을 이용해서 시작 순서를 결정하기

        runner.Bind(presenter);
        await runner.PlayAsync(obejctData.outsideObjects[object_Id].Dialogues);

        isTalking = false;
        isInteracting = false;
        player.State = EInteractorState.None;
    }
}