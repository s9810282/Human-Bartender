using UnityEngine;

public abstract class InteractiveEntity : MonoBehaviour, IInteractable
{
    [SerializeField] protected int priority;
    [SerializeField] protected bool isAvaliable;

    [SerializeField] protected OutlineHighlight outlineHighlight;

    Vector3 IInteractable.Position => transform.position;

    int IInteractable.Priority => priority;

    bool IInteractable.IsAvailable => isAvaliable;



    public abstract void Interact(IInteractor player);

    public void OnFocusEnter() => outlineHighlight.EnableHighlight();

    public void OnFocusExit() => outlineHighlight.DisableHighlight();
}
