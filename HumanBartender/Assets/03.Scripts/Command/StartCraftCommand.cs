using Cysharp.Threading.Tasks;
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
        craft_event_id = data.CraftEventId;
        Logger.Log(data.CraftEventId);

        IsSystemSwitch = true;
    }

    public bool IsSystemSwitch { get; set; }

    public async UniTask<string> ExecuteAsync()
    {
        craftData = craftMgr.GetCraftDataByID(craft_event_id);
        string nextId = await craftMgr.StartCraftAsync(craftData);
        await UniTask.Yield();

        return nextId;
    }
}
