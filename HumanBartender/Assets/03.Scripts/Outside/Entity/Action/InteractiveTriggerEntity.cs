using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class InteractiveTriggerEntity : InteractiveEntity
{
    [Header("DAta")]
    [SerializeField] string cutSceneId;
    [SerializeField] List<CutsceneLine> lines;

    [Inject] IOutsideTimeliner timeliner;

    public void SetId(string id) => cutSceneId = id;


    public override void Interact(IInteractor player)
    {
        
    }

    public override void OnFocusEnter()
    {
        OnInteracted?.Raise(this);
        IsAvaliable = false;

        timeliner.InitHandler(lines);
        timeliner.PlayTimelineCutScene(cutSceneId);
    }

    public override void OnFocusExit()
    {

    }
}
