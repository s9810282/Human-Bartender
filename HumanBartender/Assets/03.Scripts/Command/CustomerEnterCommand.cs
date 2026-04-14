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
        Debug.Log($"[효과음 재생: {sfx_mode}]");

        SlotType slotType = slot == "left" ? SlotType.Left : SlotType.Right;
        await characterSetter.SetCharacterAsync(slotType, characterId, "default");

        
        CancellationTokenSource cts = new CancellationTokenSource();
        var token = CancellationTokenSource
         .CreateLinkedTokenSource(cts.Token)
         .Token;


        cameraMove.CameraMove(slot);
        cameraZoom.ZoomIn();

        characterFader.FadeInAsync(slotType, token).Forget();

        Debug.Log("입장 연출 완료.");
        return "";
    }
}