using UnityEngine;

public class Bubi : InteractiveEntity
{
    public override async void Interact(IInteractor player)
    {
        Logger.Log("bubi");

        //데이터 로드.
        //데이터에 따른 UI On
        // UI 위치 세팅

        isTalking = true;

        OnTrackedText?.Raise(this);

        runner.Bind(presenter);
        await runner.PlayAsync(dialogueData.dayData.Scenes[0].Dialogues);

        isTalking = false;
    }
}
