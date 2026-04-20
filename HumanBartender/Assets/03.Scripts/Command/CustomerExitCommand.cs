using Cysharp.Threading.Tasks;
using Spine;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.TextCore.Text;
using VContainer;

public class CustomerExitCommand : IDialogueCommand
{
    [Inject] ICameraMove cameraMove;
    [Inject] ICameraZoom cameraZoom;
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
        SlotType slotType = slot == "left" ? SlotType.Left :
             slot == "right" ? SlotType.Right : SlotType.Middle;

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
            cameraZoom.ZoomIn(exitDuration);
            cameraMove.CameraMove(slotType == SlotType.Left ? 
                SlotType.Right : SlotType.Left, exitDuration);
        }
        else
        {
            cameraZoom.ZoomOut(exitDuration);
            cameraMove.CameraMove(SlotType.Middle, exitDuration);
        }

        await UniTask.WaitForSeconds(exitDuration);

        return "";
    }
}
