using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;


public enum SlotType
{
    Left,
    Right,
}


[System.Serializable]
public class SlotCharacterPart
{
    public SlotType type;
    public string slotCharacterName;
    public CharacterPart[] parts;
    public SpriteRenderer portaitSpriteRenderer;

    [System.NonSerialized] public Stack<AsyncOperationHandle<Sprite>?> spriteHandles = new();
    [System.NonSerialized] public Stack<AsyncOperationHandle<AnimationClip>?> animHandles = new();
    [System.NonSerialized] public CancellationTokenSource cts;
}


public class DialogueCharacterManager : MonoBehaviour, ICharacterSetter, IDialogueFader
{
    [Header("Config")]
    [SerializeField] private CharacterAnimSO animConfig;

    [Header("Parts")]
    [SerializeField] private SlotCharacterPart[] slotParts;

    private Dictionary<SlotType, SlotCharacterPart> _slotMap;


    private const string SLOT_INTRO = "Intro";
    private const string SLOT_LOOP = "Loop";



    private void Awake()
    {
        _slotMap = new Dictionary<SlotType, SlotCharacterPart>(slotParts.Length);

        foreach (var slot in slotParts)
        {
            _slotMap[slot.type] = slot;
            foreach (var part in slot.parts)
                part.Initialize();
        }
    }



    public async UniTask SetCharacterAsync(SlotType slot, string characterId, string expression)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) //Slot 존재 여부
        {
            Logger.LogWarning($"[DialogueCharacterManager] Slot '{slot}' not found");
            return;
        }

        slotData.cts?.Cancel();
        slotData.cts?.Dispose();
        slotData.cts = new CancellationTokenSource();

        var token = CancellationTokenSource
           .CreateLinkedTokenSource(slotData.cts.Token, this.GetCancellationTokenOnDestroy())
           .Token;


        ReleaseSlotHandles(slotData);
        slotData.slotCharacterName = characterId;

        var parts = slotData.parts;
        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            PartAnimData data = animConfig.GetPartData(characterId, expression, parts[i].partName);
            PartAnimData defaultData = animConfig.GetDefaultPartData(characterId, parts[i].partName);
            tasks[i] = LoadPartAsync(slotData, parts[i], data, defaultData, token);
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

    //이 친구는
    public async UniTask SetCharacterAsync(string characterId, string expression)
    {
        
        //수정 에정

        //좌측 캐릭터가 character Id와 동일하다면, 아니라면 우측부터.
        SlotType slot = characterId == slotParts[0].slotCharacterName ? SlotType.Left : SlotType.Right;

        if (!_slotMap.TryGetValue(slot, out var slotData)) //Slot 존재 여부
        {
            Logger.LogWarning($"[DialogueCharacterManager] Slot '{slot}' not found");
            return;
        }

        slotData.cts?.Cancel();
        slotData.cts?.Dispose();
        slotData.cts = new CancellationTokenSource();

        var token = CancellationTokenSource
           .CreateLinkedTokenSource(slotData.cts.Token, this.GetCancellationTokenOnDestroy())
           .Token;


        ReleaseSlotHandles(slotData);
        slotData.slotCharacterName = characterId;

        var parts = slotData.parts;
        var tasks = new UniTask[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            PartAnimData data = animConfig.GetPartData(characterId, expression, parts[i].partName);
            PartAnimData defaultData = animConfig.GetDefaultPartData(characterId, parts[i].partName);
            tasks[i] = LoadPartAsync(slotData, parts[i], data, defaultData, token);
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

    public void OnDialogueStart(SlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;
        foreach (var part in slotData.parts)
            part.OnDialogueStart();
    }
    public void OnDialogueStart()
    {
        foreach (var slot in slotParts)
            foreach (var part in slot.parts)
                part.OnDialogueStart();
    }


    public void OnDialogueEnd(SlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;
        foreach (var part in slotData.parts)
            part.OnDialogueEnd();
    }
    public void OnDialogueEnd()
    {
        foreach (var slot in slotParts)
            foreach (var part in slot.parts)
                part.OnDialogueEnd();
    }


    public void OffCharacter(SlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;
        foreach (var part in slotData.parts)
            part.SetInactive();

        if (slotData.portaitSpriteRenderer != null)
            slotData.portaitSpriteRenderer.sprite = null;
    }
    public void OffCharacter()
    {
        foreach (var slot in slotParts)
        {
            foreach (var part in slot.parts)
                part.SetInactive();

            if (slot.portaitSpriteRenderer != null)
                slot.portaitSpriteRenderer.sprite = null;
        }
    }


    public async UniTask FadeInAsync(SlotType slot, CancellationToken token)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) //Slot 존재 여부
        {
            Logger.LogWarning($"[DialogueCharacterManager] Slot '{slot}' not found");
            return;
        }

        var tasks = new UniTask[slotData.parts.Length];
        
        for (int i = 0; i < slotData.parts.Length; i++)
            tasks[i] = slotData.parts[i].FadeIn(token);

        await UniTask.WhenAll(tasks);
    }
    public async UniTask FadeOutAsync(SlotType slot, CancellationToken token)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) //Slot 존재 여부
        {
            Logger.LogWarning($"[DialogueCharacterManager] Slot '{slot}' not found");
            return;
        }


        var tasks = new UniTask[slotData.parts.Length];

        for (int i = 0; i < slotData.parts.Length; i++)
            tasks[i] = slotData.parts[i].FadeOut(token);

        await UniTask.WhenAll(tasks);
    }

    public void ReleaseAll()
    {
        foreach (var slot in slotParts)
        {
            ReleaseSlotHandles(slot);
            foreach (var part in slot.parts)
                part.Release();
        }
    }


    private void ReleaseSlotHandles(SlotCharacterPart slot)
    {
        while (slot.spriteHandles.Count > 0)
        {
            var item = slot.spriteHandles.Pop();
            ResourceLoader.ReleaseHandle<Sprite>(ref item);
        }

        while (slot.animHandles.Count > 0)
        {
            var item = slot.animHandles.Pop();
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
        SlotCharacterPart slot,
        CharacterPart part,
        PartAnimData data,
        PartAnimData defaultData,
        CancellationToken token)
    {
        if (data == null)
        {
            Logger.LogWarning($"[CharacterManager] PartAnimData null");
            return;
        }
        if (data.Loop == "none")
        {
            part.SetInactive();
            
            if (slot.portaitSpriteRenderer != null)
                slot.portaitSpriteRenderer.sprite = null;

            return;
        }

        if (await LoadAnimAsync(slot, part, data, token)) //Part Anim
            return;

        if (await LoadSpriteAsync(slot, part, data.Clip, token)) //Part Sprite
            return;

        if (defaultData == null) return;

        Logger.LogWarning($"[CharacterManager:{data.Clip}] 로드 실패 → default Portail Sprite");

        if (part.partName != "body")  //body의 경우만 Portail Image 로드 시도.
        {
            part.SetInactive();
            return;
        }

        if (await LoadPortaitSpriteAsync(slot, defaultData.Clip, token)) //Default => 사실상 더미임. 이거 빼도 되는거아닌가
            return;

        Logger.LogWarning($"[CharacterManager:{data.Clip}] default도 없음 → fallback sprite");

        part.SetInactive();

        if (slot.portaitSpriteRenderer != null)
            slot.portaitSpriteRenderer.sprite = null;
    }

    public async UniTask<bool> LoadAnimAsync(
        SlotCharacterPart slot,
        CharacterPart part,
        PartAnimData data,
        CancellationToken token)
    {
        part.SetLoopMode(data.Loop);

        string clipAddress = data.Clip;
        string introAddress = $"{clipAddress}_Intro";
        string loopAddress = $"{clipAddress}_Loop";

        var loopHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(loopAddress, token);
        slot.animHandles.Push(loopHandle);

        if (loopHandle.HasValue)
        {
            part.SetClip(SLOT_LOOP, loopHandle);

            var introHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(introAddress, token);
            slot.animHandles.Push(introHandle);

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
       SlotCharacterPart slot,
        CharacterPart part,
        string address,
        CancellationToken token)
    {
        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(address, token);
        slot.spriteHandles.Push(spriteHandle);

        if (spriteHandle.HasValue)
        {
            part.ApplySprite(spriteHandle.Value.Result);
            return true;
        }

        Logger.LogWarning($"[CharacterPart:{part.partName}] '{address}' 리소스 없음");
        return false;
    }


    public async UniTask<bool> LoadPortaitSpriteAsync(
         SlotCharacterPart slot,
        string address,
        CancellationToken token)
    {
        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(address, token);
        slot.spriteHandles.Push(spriteHandle);
        
        if (spriteHandle.HasValue)
        {
            slot.portaitSpriteRenderer.sprite = spriteHandle.Value.Result;
            return true;
        }

        Logger.LogWarning($"[CharacterPart:Portait] '{address}' 리소스 없음");
        return false;
    }

    private void OnDestroy()
    {
        ReleaseAll();

        foreach (var slot in slotParts)
        {
            slot.cts?.Cancel();
            slot.cts?.Dispose();
            slot.cts = null;
        }
    }
}
