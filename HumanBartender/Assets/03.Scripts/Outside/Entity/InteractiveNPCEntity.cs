using UnityEngine;

/// <summary>
/// 대사를 가진 NPC 엔티티의 베이스. InteractiveObjectEntity와 로직이 거의 동일하게 중복 구현되어 있으니
/// 함께 참고할 것. 여러 FlowData 중 하나를 순환(또는 조건부)으로 재생한다.
/// </summary>
public class InteractiveNPCEntity : InteractiveEntity
{
    [SerializeField] protected ITrackedbleEvent OnTrackedText;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;


    protected bool isTalking = false;

    public void Init(
        InteractableEvent onInteracted,
        VoidEvent onRefresh,
        ITrackedbleEvent onTrackedText,
        DialogueRunner runner,
        OutsideDialoguePresenter presenter)
    {
        // 부모 필드 초기화
        base.Init(onInteracted, onRefresh);

        // NPC 전용 필드 초기화
        OnTrackedText = onTrackedText;
        this.runner = runner;
        this.presenter = presenter;
    }
    /// <summary>
    /// 플레이어 상태를 Interct로 잠그고 대사를 재생한다. 재생 완료 후 다음 flow로 인덱스를 순환시키고
    /// (Conditional이면 매번 0으로 리셋) 상태를 원복, 조건 갱신 이벤트를 발생시킨다.
    /// </summary>
    public override async void Interact(IInteractor player)
    {
        if (presenter.playMode == EActivationMode.Proximity)
        {
            runner.Stop();
        }
        isTalking = true;
        isInteracting = true;
        if(ActivationMode == EActivationMode.Interact)
        player.State = EInteractorState.Interct;

        OnInteracted?.Raise(this);
        OnTrackedText?.Raise(this);
        player.InteractorEvent();
        presenter.playMode = ActivationMode;
        runner.Bind(presenter);

        // SO에서 첫 번째 Scene의 Steps 배열을 추출하여 실행
        if (steps != null)
        {
            await runner.PlayOutsideAsync(steps);
        }
        else
        {
            Debug.LogError($"[Interact] 지정된 스크립트를 찾을 수 없습니다.");
        }

        isTalking = false;
        isInteracting = false;
        player.State = EInteractorState.None;

        OnRefreshCondition?.Raise(new Void());
    }
}