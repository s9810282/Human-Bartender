using System;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractableEventChannel", menuName = "Scriptable Objects/InteractableEventChannel")]
public class InteractableEventChannel : ScriptableObject
{
    public event Action<IInteractable> OnRaised;

    public void Raise(IInteractable interactor)
    {
        OnRaised?.Invoke(interactor);
    }

    private void OnDisable()
    {
        OnRaised = null;
    }

}