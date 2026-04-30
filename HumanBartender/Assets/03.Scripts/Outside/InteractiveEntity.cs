using UnityEngine;

public abstract class InteractiveEntity : MonoBehaviour, IInteractable
{
    [SerializeField] protected InteractableEventChannel OnTrackedText;

    //고민해보기
    //data는 so같은 형태로 변경 필요
    [SerializeField] protected DayDataSO dialogueData;
    [SerializeField] protected DialogueRunner runner;
    [SerializeField] protected OutsideDialoguePresenter presenter;


    [SerializeField] protected int priority;
    [SerializeField] protected bool isAvaliable;

    [SerializeField] protected OutlineHighlight outlineHighlight;

    protected bool isTalking = false;

    Vector3 IInteractable.Position => transform.position;

    int IInteractable.Priority => priority;

    bool IInteractable.IsAvailable => isAvaliable;



    public abstract void Interact(IInteractor player);

    public void OnFocusEnter() => outlineHighlight.EnableHighlight();

    public void OnFocusExit() => outlineHighlight.DisableHighlight();
}
