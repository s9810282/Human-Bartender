using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;


public class DialogueCharacterManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private CharacterAnimSO animConfig;

    [Header("Parts")]
    [SerializeField] private CharacterPart[] parts;
    [SerializeField] private SpriteRenderer portaitSpriteRenderer;

    [SerializeField] private const string SLOT_INTRO = "Intro";
    [SerializeField] private const string SLOT_LOOP = "Loop";

    private CancellationTokenSource _cts;

    Stack<AsyncOperationHandle<Sprite>?> spriteHandles = new();
    Stack<AsyncOperationHandle<AnimationClip>?> animHandles = new();

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

        ReleaseCurrentHandles();

        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            PartAnimData data = animConfig.GetPartData(characterId, expression, parts[i].partName);
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

    public void ReleaseCurrentHandles()
    {
        while (spriteHandles.Count > 0)
        {
            var item = spriteHandles.Pop();
            ResourceLoader.ReleaseHandle<Sprite>(ref item);
        }

        while (animHandles.Count > 0)
        {
            var item = animHandles.Pop();
            ResourceLoader.ReleaseHandle<AnimationClip>(ref item);
        }
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
            portaitSpriteRenderer.sprite = null;
            return;
        }

        if (await LoadAnimAsync(part, data, token)) //Part Anim
            return;

        if (await LoadSpriteAsync(part, data.Clip, token)) //Part Sprite
            return;

        if (defaultData == null) return;

        Logger.LogWarning($"[CharacterManager:{data.Clip}] 로드 실패 → default Portail Sprite");

        if (part.partName != "body")  //body의 경우만 Portail Image 로드 시도.
        {
            part.SetInactive();
            return;
        }

        if (await LoadPortaitSpriteAsync(part, defaultData.Clip, token)) //Default => 사실상 더미임. 이거 빼도 되는거아닌가
            return;

        Logger.LogWarning($"[CharacterManager:{data.Clip}] default도 없음 → fallback sprite");

        part.SetInactive();
        portaitSpriteRenderer.sprite = null;
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

        animHandles.Push(loopHandle);

        if (loopHandle.HasValue)
        {
            part.SetClip(SLOT_LOOP, loopHandle);

            var introHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(introAddress, token);
            animHandles.Push(introHandle);

            if (introHandle.HasValue)
                part.SetClip(SLOT_INTRO, introHandle);
            else
                part.SetClip(SLOT_INTRO, loopHandle);


            part.PlayAnimation(SLOT_INTRO, token);

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
        spriteHandles.Push(spriteHandle);

        if (spriteHandle.HasValue)
        {
            part.ApplySprite(spriteHandle.Value.Result);
            return true;
        }

        Logger.LogWarning($"[CharacterPart:{part.partName}] '{address}' 리소스 없음");
        return false;
    }


    public async UniTask<bool> LoadPortaitSpriteAsync(
        CharacterPart part,
        string address,
        CancellationToken token)
    {
        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(address, token);
        spriteHandles.Push(spriteHandle);
        if (spriteHandle.HasValue)
        {
            portaitSpriteRenderer.sprite = spriteHandle.Value.Result;
            return true;
        }

        Logger.LogWarning($"[CharacterPart:Portait] '{address}' 리소스 없음");
        return false;
    }



    private void OnDestroy()
    {
        ReleaseCurrentHandles();
        ReleaseAll();

        _cts?.Cancel();
        _cts?.Dispose();
    }
}
