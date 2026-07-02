using Cysharp.Threading.Tasks;
using Spine;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.TextCore.Text;
using VContainer;

/// <summary>
/// 캐릭터 퇴장 연출 커맨드.
/// 지정 슬롯의 캐릭터를 페이드 아웃 후 제거하고, 남은 캐릭터 수에 따라 카메라를 재조정한다.
/// </summary>
public class CustomerExitCommand : IDialogueCommand
{
    [Inject] ICameraControl cameraZoom;
    [Inject] ICharacterSetter characterSetter;
    [Inject] IDialogueFader characterFader;

    private string characterId;
    private string slot;
    private string exitEffect;
    private float exitDuration = 0.3f;
    private string sfx_mode;

    CancellationTokenSource cts = new();

    public CustomerExitCommand(TriggerDetailData data)
    {
        characterId = data.CharacterId;
        slot = data.Slot;
        exitEffect = data.ExitEffect;
        exitDuration = data.ExitDuration.Value;
        sfx_mode = data.SfxMode;
    }


    bool IDialogueCommand.IsSystemSwitch { get; set; }

    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        ESlotType slotType = slot == "left" ? ESlotType.Left :
             slot == "right" ? ESlotType.Right : ESlotType.Middle;

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var token = CancellationTokenSource
         .CreateLinkedTokenSource(cts.Token)
         .Token;

        await characterFader.FadeOutAsync(slotType, token);
        characterSetter.ResetCharacter(slotType);

        int c = characterSetter.GetCharacterCount();


        if (c == 1)
        {
            cameraZoom.CameraZoom(ECameraZoomType.Sub, exitDuration);
            cameraZoom.CameraMove(slotType == ESlotType.Left ? 
                ESlotType.Right : ESlotType.Left, exitDuration);
        }
        else
        {
            cameraZoom.CameraZoom(ECameraZoomType.Base, exitDuration);
            cameraZoom.CameraMove(ESlotType.Middle, exitDuration);
        }

        await UniTask.WaitForSeconds(exitDuration);

        return "";
    }
}
