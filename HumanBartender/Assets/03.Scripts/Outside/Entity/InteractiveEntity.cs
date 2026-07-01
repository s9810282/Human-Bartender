using UnityEngine;

public abstract class InteractiveEntity : OutsideEntity, IInteractable
{
    [SerializeField] protected int priority;
    [SerializeField] private bool isAvaliable;
    [SerializeField] protected bool isInteracting;
    [SerializeField] private string label;
    [SerializeField] protected Vector2 buttonOffset;
    [SerializeField] protected Vector2 textOffset;

    [SerializeField] protected OutlineHighlight outlineHighlight;
    [SerializeField] protected InteractableEvent OnInteracted;
    [SerializeField] protected VoidEvent OnRefreshCondition;

    public bool IsInteracting { get => isInteracting; set => isInteracting = value; }
    public bool IsAvaliable { get => isAvaliable; set => isAvaliable = value; }
    public string EntityLabel { get => label; set => label = value; }


    int IInteractable.Priority => priority;
    string IInteractable.Label => label;
    Vector2 ITrackedble.ButtonOffset => buttonOffset;
    Vector2 ITrackedble.TextOffset => textOffset;
    bool ITrackedble.IsAvaliable => isAvaliable;

    

    public abstract void Interact(IInteractor player);

    public virtual void OnFocusEnter()
    {
        if(!isInteracting && outlineHighlight != null)
            outlineHighlight.SetHighlight(true);
    }
    public virtual void OnFocusExit()
    {
        if(outlineHighlight != null)
            outlineHighlight.SetHighlight(false);
    }
}
