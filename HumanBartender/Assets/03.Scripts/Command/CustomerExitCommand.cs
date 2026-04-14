using Cysharp.Threading.Tasks;
using Spine;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class CustomerExitCommand : IDialogueCommand
{
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
        Debug.Log($"[효과음 재생: {sfx_mode}]");

        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));

        Debug.Log("퇴장 연출 완료.");

        return "";
    }
}
