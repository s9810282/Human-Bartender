using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Timeline;

public interface ITimeLinePlayer
{
    public void PlayTimeline(TimelineAsset timeline);
    public void StopTimeline();
}


public interface ICharacterSetter
{
    public UniTask SetCharacterAsync(string characterId, string expression, ESlotType slotType = ESlotType.Right);
    public int GetCharacterCount();
    public void ResetCharacter(ESlotType slot);
    public void ResetCharacter();
}

public interface IDialogueFader
{
    public UniTask FadeInAsync(ESlotType slot, CancellationToken token);
    public UniTask FadeOutAsync(ESlotType slot, CancellationToken token);
}


public interface IFade
{
    public UniTask FadeIn(CancellationToken token);
    public UniTask FadeOut(CancellationToken token);
}


public interface ICameraControl
{
    
    public void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null);
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base);
    public void CameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f);

    public void CameraMove(ESlotType slot, float dur = 1f);
    public void CameraMove(Vector3 pos, float dur = 1);
}


public interface ICocktailCraft
{
    UniTask<string> StartCraftAsync(string id);
}



public interface ICutScenePlayer
{
    UniTask PlayCutScene(
        string id,        
        UniTaskCompletionSource tcs = null);

    public void ClearCutScene();
    public void OnContinueTimeline();
}



public interface IEffectPlayer
{
    UniTask PlayEffectAsync(EEffectType type, float duration = 1f, float Intensity = 0f);
}

