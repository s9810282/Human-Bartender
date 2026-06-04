using Cysharp.Threading.Tasks;
using Spine;
using System.Threading;
using UnityEngine;
using UnityEngine.TextCore.Text;
using VContainer;

public class StartCutSceneCommand : IDialogueCommand
{
   
    [Inject] IEffectPlayer effectPlayer;
    [Inject] ICutScenePlayer cutScenePlayer;
    [Inject] ICameraControl cameraZoom;
    [Inject] ISoundManager soundManager;

    private string anim;
    private ECutSceneType type;
    private ECameraZoomType cameraType;

    UniTaskCompletionSource cameraTcs;

    public StartCutSceneCommand(TriggerDetailData data)
    {
        anim = data.CutsceneId;
        type = data.CutsceneType;
        cameraType = data.CameraType;

        cameraTcs = new UniTaskCompletionSource();
    }

    public bool IsSystemSwitch { get; set; }


    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        await effectPlayer.PlayEffectAsync(EEffectType.FadeIn, 1f);
        soundManager.PauseBGM();

        //카메라 사이즈 넣어야함
        ECameraZoomType zoomType = cameraType;
        cameraZoom.ActionZoomAndBack(zoomType, cameraTcs);

        //추후 type 값 추가.
        await cutScenePlayer.PlayCutScene(anim);
        await UniTask.WaitForSeconds(1f);

        cutScenePlayer.ClearCutScene();

        soundManager.ResumeBGM();

        if (cameraTcs != null)
            cameraTcs.TrySetResult();

        return "";
    }
}
