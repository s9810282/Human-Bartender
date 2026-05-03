using UnityEngine;

public class InteractiveActionEntity : InteractiveEntity
{
    [SerializeField] private InteractionAction action;

    public override void Interact(IInteractor player)
    {
        action.Execute(this, player);
    }
}
