using UnityEngine;

public interface IInteractable
{
    Vector3 Position { get; }
    int Priority { get; }
    bool IsAvailable { get; }

    public void Interact(IInteractor player);
    public void OnFocusEnter();
    public void OnFocusExit();
}
