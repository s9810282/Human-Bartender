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
    Middle,
}


[System.Serializable]
public class SlotCharacterPart
{
    public SlotType type;
    public string slotCharacterName = "";
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
    private const string SLOT_DIALOGUE = "Dialogue";


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

    public int GetCharacterCount()
    {
        int n = 0;
        foreach (var part in slotParts)
        {
            if (part.slotCharacterName != "")
                n++;
        }

        return n;
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

    /// <summary>
    /// Slot이 따로 지정되지 않았기에 검사를 통해 위치 획득
    /// 둘 다 비어있다면 우측부터
    /// 둘다 빈게 아니라면 검사 후 characterId가 동일한 쪽으로, 아니라면 무시'
    /// 
    /// </summary>
    /// <param name="characterId"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public async UniTask SetCharacterAsync(string characterId, string expression)
    {
        SlotType slot = new();

        for (int i = 0; i < slotParts.Length; i++)
        {
            if (slotParts[i].slotCharacterName == "")
                continue;
            
            if(characterId == slotParts[i].slotCharacterName)
            {
                slot = slotParts[i].type;
            }
        }
        
        
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


    public void ResetCharacter(SlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;

        slotData.slotCharacterName = "";

        foreach (var part in slotData.parts)
            part.SetInactive();

        if (slotData.portaitSpriteRenderer != null)
            slotData.portaitSpriteRenderer.sprite = null;
    }
    public void ResetCharacter()
    {
        foreach (var slot in slotParts)
        {
            slot.slotCharacterName = "";

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



    //************************************************************************************//
    // 스크립트 기능 분리 검토. 단순 컴포지션
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

        if (await LoadAnimAsync(slot, part, defaultData, token)) //Part Default Anim
            return;

        if (await LoadSpriteAsync(slot, part, data, token)) //Part Sprite
            return;

        if (await LoadSpriteAsync(slot, part, defaultData, token)) //Part Default Sprite
            return;


        if (defaultData == null) return;

        Logger.LogWarning($"[CharacterManager:{data.Clip}] 로드 실패 → default Portail Sprite");

        if (part.partName != "body")  //body의 경우만 Portail Image 로드 시도.
        {
            part.SetInactive();
            return;
        }

        if (await LoadPortaitSpriteAsync(slot, defaultData, token))
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
        if (data.Clip == null) //clipData가 null이면 false, 추후 default anim 삽입.
            return false;

        part.SetLoopMode(data.Loop);

        string clipAddress = data.Clip;
        string introAddress = $"{clipAddress}_Intro";
        string loopAddress = $"{clipAddress}_Loop";
        string dialogueAddress = $"{clipAddress}_Dialogue";

        var loopHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(loopAddress, token);
        slot.animHandles.Push(loopHandle);

        if (loopHandle.HasValue)
        {
            //LoopSetting
            part.SetClip(SLOT_LOOP, loopHandle);



            //Intro Setting
            var introHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(introAddress, token);
            slot.animHandles.Push(introHandle);

            if (introHandle.HasValue)
                part.SetClip(SLOT_INTRO, introHandle);
            else
                part.SetClip(SLOT_INTRO, loopHandle);



            //Dialogue Setting
            var dialogueHandle = await ResourceLoader.TryLoadAsync<AnimationClip>(dialogueAddress, token);
            slot.animHandles.Push(dialogueHandle);


            if (dialogueHandle.HasValue)
                part.SetClip(SLOT_DIALOGUE, dialogueHandle);
            else
                part.SetClip(SLOT_DIALOGUE, loopHandle);




            //추후 파츠별 실행 시점 동기화 예정.
            part.PlayAnimation(SLOT_INTRO, token);

            return true;
        }

        return false;
    }

    public async UniTask<bool> LoadSpriteAsync(
       SlotCharacterPart slot,
        CharacterPart part,
        PartAnimData data,
        CancellationToken token)
    {

        if(data.Clip == null)
            return false;

        string clipaddress = data.Clip;

        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(clipaddress, token);
        slot.spriteHandles.Push(spriteHandle);

        if (spriteHandle.HasValue)
        {
            part.ApplySprite(spriteHandle.Value.Result);
            return true;
        }

        Logger.LogWarning($"[CharacterPart:{part.partName}] '{clipaddress}' 리소스 없음");
        return false;
    }


    public async UniTask<bool> LoadPortaitSpriteAsync(
         SlotCharacterPart slot,
        PartAnimData data,
        CancellationToken token)
    {
        if (data.Clip == null)
            return false;

        string clipaddress = data.Clip;

        var spriteHandle = await ResourceLoader.TryLoadAsync<Sprite>(clipaddress, token);
        slot.spriteHandles.Push(spriteHandle);
        
        if (spriteHandle.HasValue)
        {
            slot.portaitSpriteRenderer.sprite = spriteHandle.Value.Result;
            return true;
        }

        Logger.LogWarning($"[CharacterPart:Portait] '{clipaddress}' 리소스 없음");
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
