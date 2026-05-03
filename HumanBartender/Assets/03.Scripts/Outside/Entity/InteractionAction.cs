using UnityEngine;

[CreateAssetMenu(fileName = "InteractionAction", menuName = "Scriptable Objects/InteractionAction")]
public abstract  class InteractionAction : ScriptableObject
{
    public abstract void Execute(InteractiveActionEntity source, IInteractor player);
}
