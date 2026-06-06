using UnityEngine;



public interface IInteractable : IEntity
{
    int Priority { get; }
    Vector2 Offset { get; }
    string Label { get; }
    bool IsAvaliable { get; }
    bool IsInteracting { get; set; }


    public void Interact(IInteractor player);
    public void OnFocusEnter();
    public void OnFocusExit();
}
