using Cysharp.Threading.Tasks;
using Spine;
using System.Threading;
using UnityEngine;
using UnityEngine.TextCore.Text;
using VContainer;

public class StartCutSceneCommand : IDialogueCommand
{
    [Inject] ICutScenePlayer cutSceneManager;

    private string anim;

    public StartCutSceneCommand(TriggerDetailData data)
    {
        anim = data.CutsceneId;
    }

    public bool IsSystemSwitch { get; set; }


    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        await cutSceneManager.PlayCutScene(anim);

        return "";
    }
}
