using Cysharp.Threading.Tasks;
using Spine;
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

    public CustomerExitCommand(TriggerDetailData data)
    {
        characterId = data.CharacterId;
        slot = data.Slot;
        exitEffect = data.ExitEffect;
        exitDuration = data.ExitDuration.Value;
        sfx_mode = data.SfxMode;
    }


    bool IDialogueCommand.IsSystemSwitch { get; set; }

    public async UniTask<string> ExecuteAsync()
    {
        SlotType slotType = slot == "left" ? SlotType.Left : SlotType.Right;





        return "";
    }
}
