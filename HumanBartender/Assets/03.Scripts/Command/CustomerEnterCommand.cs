using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

public class CustomerEnterCommand : IDialogueCommand
{
    [Inject] ICameraControl cameraZoom;
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


    public async UniTask<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        ESlotType slotType = slot == "left" ? ESlotType.Left : 
            slot == "right" ? ESlotType.Right : ESlotType.Middle;

        await characterSetter.SetCharacterAsync(characterId, "default");
        int c = characterSetter.GetCharacterCount();

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var token = CancellationTokenSource
         .CreateLinkedTokenSource(cts.Token)
         .Token;

        cameraZoom.CameraMove(slotType, enterDuration);


        if (c == 1)
        {
            cameraZoom.CameraZoom(ECameraZoomType.Sub, enterDuration);
            cameraZoom.CameraMove(slotType, enterDuration);
        }
        else
        {
            cameraZoom.CameraZoom(ECameraZoomType.Base, enterDuration);
            cameraZoom.CameraMove(ESlotType.Middle, enterDuration);
        }

        characterFader.FadeInAsync(slotType, token).Forget();

        await UniTask.WaitForSeconds(enterDuration);

        return "";
    }
}