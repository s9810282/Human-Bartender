using UnityEngine;

public enum EInteractorState
{
    None,
    Interct,
}

public interface IInteractor
{
    GameObject GameObject { get; }   
    Transform Transform { get; }     
    EInteractorState State { get; set; }
}
