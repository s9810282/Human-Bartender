using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

public class StartCraftCommand : IDialogueCommand
{
    [Inject] private ICocktailCraft craftMgr;
    
    private string craft_event_id;
    


    public StartCraftCommand(TriggerDetailData data)
    {
        craft_event_id = data.CraftEventId;
        Logger.Log(data.CraftEventId);

        IsSystemSwitch = true;
    }

    public bool IsSystemSwitch { get; set; }

    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        string nextId = await craftMgr.StartCraftAsync(craft_event_id);
        return nextId;
    }
}
