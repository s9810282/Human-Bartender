using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.Timeline;
using UnityEngineInternal;

public interface ITimeLinePlayer
{
    public void PlayTimeline(TimelineAsset timeline);
    public void StopTimeline();
}


public interface ICharacterSetter
{
    public UniTask SetCharacterAsync(string characterId, string expression);
    public int GetCharacterCount();
    public void ResetCharacter(SlotType slot);
    public void ResetCharacter();
}

public interface IDialogueFader
{
    public UniTask FadeInAsync(SlotType slot, CancellationToken token);
    public UniTask FadeOutAsync(SlotType slot, CancellationToken token);
}


public interface IFade
{
    public UniTask FadeIn(CancellationToken token);
    public UniTask FadeOut(CancellationToken token);
}


public interface ICameraZoom
{
    public void ZoomIn(float dur = 1f);
    public void ZoomOut(float dur = 1f);
    public void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null);
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base);
}


public interface ICameraMove
{
    public void CameraMove(SlotType slot, float dur = 1f);
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
}


public interface IEffectPlayer
{
    UniTask PlayEffectAsync(EEffectType type, float duration = 1f, float Intensity = 0f);
}

