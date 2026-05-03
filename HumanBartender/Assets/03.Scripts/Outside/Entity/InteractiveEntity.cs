using UnityEngine;

public abstract class InteractiveEntity : OutsideEntity, IInteractable
{
    [SerializeField] protected int priority;
    [SerializeField] protected bool isAvaliable;

    [SerializeField] protected OutlineHighlight outlineHighlight;

    int IInteractable.Priority => priority;

    bool IInteractable.IsAvaliable => isAvaliable;


    public abstract void Interact(IInteractor player);

    public virtual void OnFocusEnter() => outlineHighlight.SetHighlight(true);

    public virtual void OnFocusExit() => outlineHighlight.SetHighlight(false);
}
