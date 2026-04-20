using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngineInternal;


public interface ICharacterSetter
{
    public UniTask SetCharacterAsync(SlotType slot, string characterId, string expression);
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
    public void ActionZoomAndBack(UniTaskCompletionSource tcs);
}


public interface ICameraMove
{
    public void CameraMove(SlotType slot, float dur = 1f);
}


public interface ICocktailCraft
{
    UniTask<string> StartCraftAsync(string id);
}


public interface IComicCutScenePlayer
{
    float GetComicCutSceneTime(string id);
    UniTask PlayComicCutSceneAsync(string id, UniTaskCompletionSource tcs = null);
}

public interface ISpriteAnimationCutScenePlayer
{
    UniTask PlaySpriteAnimationCutScene(string id);
}


public interface IEffectPlayer
{
    UniTask PlayEffectAsync(string type, float duration, float Intensity = 0f);
}

