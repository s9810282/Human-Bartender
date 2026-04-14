using Cysharp.Threading.Tasks;
using Spine;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class PlaySideviewAnimCommand : IDialogueCommand
{
    private string anim;
    private bool isResumeAfter = false;

    public PlaySideviewAnimCommand(TriggerDetailData data)
    {
        anim = data.AnimId;
        isResumeAfter = data.ResumeAfter.Value;
    }

    public bool IsSystemSwitch { get; set; }


    public async UniTask<string> ExecuteAsync()
    {
        await UniTask.Yield();
        return "";
    }
}
