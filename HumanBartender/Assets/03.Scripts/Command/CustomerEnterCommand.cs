using System.Collections;
using UnityEngine;
using Newtonsoft.Json.Linq;

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

    public IEnumerator Execute()
    {
        Debug.Log($"[효과음 재생: {sfx}]");
        Debug.Log($"{characterId} 캐릭터가 {animation} 상태로 입장합니다.");

        
        yield return new WaitForSeconds(1f);

        Debug.Log("입장 연출 완료.");
    }
}