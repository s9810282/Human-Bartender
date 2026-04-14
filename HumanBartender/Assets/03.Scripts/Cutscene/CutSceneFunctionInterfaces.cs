using Cysharp.Threading.Tasks;

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

