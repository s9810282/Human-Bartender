using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ITrackedbleEvent", menuName = "Scriptable Objects/ITrackedbleEvent")]
public class ITrackedbleEvent : ScriptableObject
{
    public event Action<ITrackedble> OnRaised;

    public void Raise(ITrackedble interactor)
    {
        OnRaised?.Invoke(interactor);
    }

    private void OnDisable()
    {
        OnRaised = null;
    }
}
