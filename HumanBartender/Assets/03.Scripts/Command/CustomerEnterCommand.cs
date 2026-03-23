using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public class CustomerEnterCommand : IDialogueCommand
{
    private string characterId;
    private string sfx;
    private string animation;

    
    public CustomerEnterCommand(TriggerDetailData data)
    {
        characterId = data.character_id;
        sfx = data.sfx;
        animation = data.animation;

        IsSystemSwitch = false;
    }

    public bool IsSystemSwitch { get; set; }


    public async UniTask<string> ExecuteAsync()
    {
        Debug.Log($"[효과음 재생: {sfx}]");
        Debug.Log($"{characterId} 캐릭터가 {animation} 상태로 입장합니다.");


        await UniTask.Delay(System.TimeSpan.FromSeconds(1f));

        Debug.Log("입장 연출 완료.");
        return "";
    }
}