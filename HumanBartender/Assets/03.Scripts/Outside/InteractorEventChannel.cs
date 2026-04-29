using System;
using UnityEngine;

[CreateAssetMenu(fileName = "InteractorEventChannel", menuName = "Scriptable Objects/InteractorEventChannel")]
public class InteractorEventChannel : ScriptableObject
{
    public event Action<IInteractor> OnRaised;

    public void Raise(IInteractor interactor)
    {
        OnRaised?.Invoke(interactor);
    }

    private void OnDisable()
    {
        OnRaised = null;
    }

}