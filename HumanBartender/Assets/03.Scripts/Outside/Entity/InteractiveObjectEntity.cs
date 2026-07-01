using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 대사가 걸려 있는 상호작용 오브젝트(NPC가 아닌 사물 등). InteractiveNPCEntity와 로직이 거의 동일하게
/// 중복 구현되어 있으니 함께 참고할 것. 여러 FlowData 중 하나를 순환(또는 조건부)으로 재생한다.
/// </summary>
public class InteractiveObjectEntity : InteractiveEntity
{
    [SerializeField] protected ITrackedbleEvent OnTrackedText;
    [SerializeField] protected string object_Id;

    [SerializeField] protected List<FlowData> flows;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;

    protected bool isTalking = false;

    [SerializeField] int curFlowIndex = 0;
    [SerializeField] ESelectionType selectionType = ESelectionType.Random;

    /// <summary>외부(매니저 등)에서 대사 목록과 선택 방식을 주입한다.</summary>
    public void InjectDialogue(List<FlowData> data, ESelectionType selection)
    {
        if (data.Count == 0)
            Logger.LogError($"Select Flow Data is Null");

        flows = data;
        selectionType = selection;
    }

    /// <summary>
    /// 플레이어 상태를 Interct로 잠그고 대사를 재생한다. 재생 완료 후 다음 flow로 인덱스를 순환시키고
    /// (Conditional이면 매번 0으로 리셋) 상태를 원복, 조건 갱신 이벤트를 발생시킨다.
    /// </summary>
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