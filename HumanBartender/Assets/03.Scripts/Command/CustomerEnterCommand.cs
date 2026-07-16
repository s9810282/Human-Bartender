using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;

/// <summary>
/// 캐릭터 등장 연출 커맨드.
/// 지정된 슬롯에 캐릭터를 배치하고 카메라 줌/이동 후 페이드 인을 수행한다.
/// 캐릭터 수가 1명이면 서브 줌으로 해당 슬롯에 집중하고, 2명 이상이면 기본 줌으로 중앙을 바라본다.
/// </summary>
public class CustomerEnterCommand : IDialogueCommand
{
    [Inject] ICameraControlNew cameraZoom;
    [Inject] ISlotCamera slotCamera;
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

        Logger.Log($"{characterId} : {slot}");
        await characterSetter.SetCharacterAsync(characterId, "default", slotType);
        int c = characterSetter.GetCharacterCount();

        cts?.Cancel();
        cts?.Dispose();
        cts = new CancellationTokenSource();

        var token = CancellationTokenSource
         .CreateLinkedTokenSource(cts.Token)
         .Token;

        if (c == 1)
        {
            cameraZoom.TransitionCameraZoom(ECameraZoomType.Sub, enterDuration);
            slotCamera.MoveToSlot(slotType, enterDuration);
        }
        else
        {
            cameraZoom.TransitionCameraZoom(ECameraZoomType.Base, enterDuration);
            slotCamera.MoveToSlot(ESlotType.Middle, enterDuration);
        }

        characterFader.FadeInAsync(slotType, token).Forget();

        await UniTask.WaitForSeconds(enterDuration);

        return "";
    }
}