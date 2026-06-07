using UnityEngine;
using VContainer;

public class InteractiveTriggerEntity : InteractiveEntity
{
    [SerializeField] string cutSceneId;
    [Inject] IOutsideTimeliner timeliner;

    public void SetId(string id) => cutSceneId = id;


    public override void Interact(IInteractor player)
    {
        
    }

    public override void OnFocusEnter()
    {
        OnInteracted?.Raise(this);
        IsAvaliable = false;
        timeliner.PlayTimelineCutScene(cutSceneId);
    }

    public override void OnFocusExit()
    {

    }
}
