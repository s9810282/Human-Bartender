using Cysharp.Threading.Tasks;
using Spine;
using System.Threading;
using UnityEngine;
using VContainer;

public class CustomerEnterCommand : IDialogueCommand
{
    [Inject] ICameraMove cameraMove;
    [Inject] ICameraZoom cameraZoom;
    [Inject] ICharacterSetter characterSetter;
    [Inject] IDialogueFader characterFader;

    private string characterId;
    private string slot;
    private string enterEffect;
    private float enterDuration = 0.3f;
    private string sfx_mode;

    CancellationTokenSource cts = new();



    public CustomerEnterCommand(TriggerDetailData data)
    {
        characterId = data.CharacterId;
        slot = data.Slot;
        enterEffect = data.EnterEffect;
        enterDuration = data.EnterDuration.Value;
        sfx_mode = data.SfxMode;

        IsSystemSwitch = false;
    }

    public bool IsSystemSwitch { get; set; }


    public async UniTask<string> ExecuteAsync()
    {
        SlotType slotType = slot == "left" ? SlotType.Left : 
            slot == "right" ? SlotType.Right : SlotType.Middle;

        await characterSetter.SetCharacterAsync(slotType, characterId, "default");
        int c = characterSetter.GetCharacterCount();

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var token = CancellationTokenSource
         .CreateLinkedTokenSource(cts.Token)
         .Token;

        cameraMove.CameraMove(slotType);


        if (c == 1)
        {
            cameraZoom.ZoomIn();
            cameraMove.CameraMove(slotType);
        }
        else
        {
            cameraZoom.ZoomOut();
            cameraMove.CameraMove(SlotType.Middle);
        }

        characterFader.FadeInAsync(slotType, token).Forget();

        await UniTask.WaitForSeconds(enterDuration);

        return "";
    }
}