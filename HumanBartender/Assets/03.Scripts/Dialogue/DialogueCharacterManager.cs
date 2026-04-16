using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Unity.VisualScripting.Antlr3.Runtime;
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
    [Header("DATA")]
    [SerializeField] private CharacterAnimSO animConfig;

   
    [Header("Parts")]
    [SerializeField] private SlotCharacterPart[] slotParts;

    private Dictionary<SlotType, SlotCharacterPart> _slotMap;

    private CharacterLoader characterLoader;


    private const string SLOT_INTRO = "Intro";
    private const string SLOT_LOOP = "Loop";
    private const string SLOT_DIALOGUE = "Dialogue";


    private void Awake()
    {
        _slotMap = new Dictionary<SlotType, SlotCharacterPart>(slotParts.Length);
        characterLoader = new CharacterLoader();

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
            tasks[i] = characterLoader.LoadPartAsync(slotData, parts[i], data, defaultData, token);
        }

        try
        {
            await UniTask.WhenAll(tasks);

            tasks = new UniTask[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                tasks[i] = characterLoader.PartAnimationSyncStart(parts[i], token);
            }

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
            tasks[i] = characterLoader.LoadPartAsync(slotData, parts[i], data, defaultData, token);
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
    public void OnDialogueStart(string speaker)
    {

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
    public void OnDialogueEnd(string speaker)
    {

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
