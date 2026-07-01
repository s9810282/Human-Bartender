using UnityEngine;

public enum EInteractorState
{
    None,
    Interct,
    ForceMove,
    Lock,
}

public interface IInteractor : IEntity, ITrackedble
{ 
    EInteractorState State { get; set; }
    void InteractorEvent();
}

