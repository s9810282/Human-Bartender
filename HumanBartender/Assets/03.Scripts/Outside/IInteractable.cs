using UnityEngine;



public interface IInteractable : IEntity
{
    int Priority { get; }
    bool IsAvaliable { get; }

    public void Interact(IInteractor player);
    public void OnFocusEnter();
    public void OnFocusExit();
}
