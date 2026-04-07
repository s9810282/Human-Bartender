using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;


public class DialogueCharacterManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CharacterAnimSO animConfig;

    [Header("Parts")]
    [SerializeField] private CharacterPart[] parts;

    private CancellationTokenSource _cts;


    private void Awake()
    {
        foreach (var part in parts)
            part.Initialize();
    }

    

    public async UniTask SetCharacterAsync(string characterId, string expression)
    {        
        //cancel 토큰 초기화
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        var token = CancellationTokenSource
            .CreateLinkedTokenSource(_cts.Token, this.GetCancellationTokenOnDestroy())
            .Token;

        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            PartAnimData data       = animConfig.GetPartData(characterId, expression, parts[i].partName);
            PartAnimData defaultData = animConfig.GetDefaultPartData(characterId, parts[i].partName);
            tasks[i] = LoadPartAsync(parts[i], data, defaultData, token);
        }

        try
        {
            await UniTask.WhenAll(tasks);
        }
        catch (OperationCanceledException e)
        {
            Logger.Log(e.Message);

        }
    }

    
    public void OnDialogueStart()
    {
        foreach (var part in parts)
            part.OnDialogueStart();
    }

    public void OnDialogueEnd()
    {
        foreach (var part in parts)
            part.OnDialogueEnd();
    }

    public void OffCharacter()
    {
        foreach (var part in parts)
            part.SetInactive();
    }

    public void ReleaseAll()
    {
        foreach (var part in parts)
            part.Release();
    }



    /// <summary>
    /// 애니메이션 로드시도 :  loop 클립 로드 -> intro 클립 로드 시도 -> Part.Applyanimaton
    /// 스프라이트 로드시도 : 해당 파츠 Sprite 로드 시도 -> Part.ApplySprite
    /// 디폴트 스프라이트 로드 시도 : 존재 여부 확인 후 -> Part.ApplySprite
    /// 
    /// 모두 실패한다면 SetInactive()
    /// </summary>
    /// <param name="part"></param>
    /// <param name="data"></param>
    /// <param name="defaultData"></param>
    /// <returns></returns>
    private async UniTask LoadPartAsync(
        CharacterPart part,
        PartAnimData data,
        PartAnimData defaultData,
        CancellationToken token)
    {
        if (data == null) return;
        if (data.Loop == "none")
        {
            part.SetInactive();
            return;
        }

        if (await LoadAnimAsync(part, data, token)) //Anim
            return;

        if (await LoadSpriteAsync(part, data.Clip, token)) //Sprite
            return;

        if (defaultData == null) return;

        Logger.LogWarning($"[CharacterManager:{part.partName}] 로드 실패 → default 폴백");

        if (await LoadSpriteAsync(part, defaultData.Clip, token)) //Default => 사실상 더미임. 이거 빼도 되는거아닌가
            return;

        Logger.LogWarning($"[CharacterManager:{part.partName}] default도 없음 → fallback sprite");
        part.SetInactive();
    }

    public async UniTask<bool> LoadAnimAsync(
        CharacterPart part, 
        PartAnimData data,
        CancellationToken token)
    {
        part.SetLoopMode(data.Loop);

        string clipAddress = data.Clip;
        string introAddress = $"{clipAddress}_Intro";
        string loopAddress = $"{clipAddress}_Loop";

        var loopHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(loopAddress, token);


        if (loopHandle.HasValue)
        {
            var introHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(introAddress, token);
            part.ApplyAnimation(introHandle, loopHandle);

            return true;
        }

        return false;
    }

    public async UniTask<bool> LoadSpriteAsync(
        CharacterPart part, 
        string address,
        CancellationToken token)
    {
        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(address, token);

        if (spriteHandle.HasValue)
        {
            part.ApplySprite(spriteHandle);
            return true;
        }

        Logger.LogWarning($"[CharacterPart:{part.partName}] '{address}' 리소스 없음");
        return false;
    }

    private void OnDestroy()
    { 
        ReleaseAll();

        _cts?.Cancel();
        _cts?.Dispose();
    }
}
