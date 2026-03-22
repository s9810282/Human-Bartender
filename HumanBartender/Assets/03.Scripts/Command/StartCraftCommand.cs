using System.Collections;
using UnityEngine;
using VContainer;

public class StartCraftCommand : IDialogueCommand
{
    [Inject] private CocktailCraftManager craftMgr;
    

    private string craft_event_id;
    private CraftEventData craftData;


    public StartCraftCommand(TriggerDetailData data)
    {
        craft_event_id = data.craft_event_id;
        Logger.Log(data.craft_event_id);

        IsSystemSwitch = true;
    }

    public bool IsSystemSwitch { get; set; }

    public IEnumerator Execute()
    {
        craftData = craftMgr.GetCraftDataByID(craft_event_id);
        craftMgr.StartCraft(craftData);
        yield break;
    }
}
