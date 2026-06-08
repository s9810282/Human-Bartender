using UnityEngine;

public enum EInteractorState
{
    None,
    Interct,
    ForceMove,
    Lock,
}

public interface IInteractor
{
    GameObject GameObject { get; }   
    Transform Transform { get; }     
    EInteractorState State { get; set; }
    void InteractorEvent();
}
