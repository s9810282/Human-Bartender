using UnityEngine;

public abstract class InteractiveEntity : OutsideEntity, IInteractable
{
    [SerializeField] protected int priority;
    [SerializeField] protected bool isAvaliable;
    [SerializeField] protected bool isInteracting;

    [SerializeField] protected OutlineHighlight outlineHighlight;

    public bool IsInteracting { get => isInteracting; set => isInteracting = value; }

    int IInteractable.Priority => priority;

    bool IInteractable.IsAvaliable => isAvaliable;


    public abstract void Interact(IInteractor player);

    public virtual void OnFocusEnter()
    {
        if(!isInteracting)
            outlineHighlight.SetHighlight(true);
    }

    public virtual void OnFocusExit() => outlineHighlight.SetHighlight(false);
}
