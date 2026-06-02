using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public enum ESlotType
{
    /* Inside */

    Left,
    Right,
    Middle,
    None,
    /* Outside */


}


[System.Serializable]
public class SlotCharacterPart
{
    public GameObject slot;
    public ESlotType type;
    public string slotCharacterName = "";
    public string expression = "";
    public CharacterPart[] parts;
    public SpriteRenderer portaitSpriteRenderer;
    

    [System.NonSerialized] public Stack<AsyncOperationHandle<Sprite>?> spriteHandles = new();
    [System.NonSerialized] public Stack<AsyncOperationHandle<AnimationClip>?> animHandles = new();

    [System.NonSerialized] public Stack<AsyncOperationHandle<Sprite>?> spriteRemoveHandles = new();
    [System.NonSerialized] public Stack<AsyncOperationHandle<AnimationClip>?> animRemoveHandles = new();

    [System.NonSerialized] public CancellationTokenSource cts;
}


public class DialogueCharacterManager : MonoBehaviour, ICharacterSetter, IDialogueFader
{
    [Header("DATA")]
    [SerializeField] private CharacterAnimSO animConfig;

   
    [Header("Parts")]
    [SerializeField] private SlotCharacterPart[] slotParts;

    private Dictionary<ESlotType, SlotCharacterPart> _slotMap;

    private CharacterLoader characterLoader;


    private const string SLOT_INTRO = "Intro";
    private const string SLOT_LOOP = "Loop";
    private const string SLOT_DIALOGUE = "Dialogue";


    private void Awake()
    {
        _slotMap = new Dictionary<ESlotType, SlotCharacterPart>(slotParts.Length);
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
    public Vector3 GetCharacterPosition(string characterId)
    {
        for (int i = 0; i < slotParts.Length; i++)
        {
            if (slotParts[i].slotCharacterName == "")
                continue;

            if (characterId == slotParts[i].slotCharacterName)
            {
                return slotParts[i].slot.transform.position;
            }
        }

        return Vector3.zero;
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
    public async UniTask SetCharacterAsync(string characterId, string expression, ESlotType slotType = ESlotType.None)
    {
        ESlotType slot = ESlotType.None;

        //위치가 지정된 경우는 문제가 없지만 지정되지 않았을 경우.

        if (slotType != ESlotType.None)
            slot = slotType;
        else
        {
            for (int i = 0; i < slotParts.Length; i++)
            {
                if (slotParts[i].slotCharacterName == "")
                    continue;

                if (characterId == slotParts[i].slotCharacterName)
                {
                    slot = slotParts[i].type;
                }
            }
        }




        if (!_slotMap.TryGetValue(slot, out var slotData)) //Slot 존재 여부
        {
            Logger.LogWarning($"[DialogueCharacterManager] Slot '{slot}' not found");
            return;
        }


        Logger.Log($"{characterId} Load, Slot, {slotData.type}, Expression {slotData.expression}, Express {expression}");

        if (slotData.slotCharacterName == characterId && slotData.expression == expression) return;

        Logger.Log($"{characterId} Load");

        slotData.cts?.Cancel();
        slotData.cts?.Dispose();
        slotData.cts = new CancellationTokenSource();


        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
        slotData.cts.Token, this.GetCancellationTokenOnDestroy());
        var token = linkedCts.Token;

        MoveCurrentHandlesToRemove(slotData);

        slotData.slotCharacterName = characterId;
        slotData.expression = expression;

        bool isSprite = animConfig.CheckExpressionPortailSprite(characterId, expression);
        var parts = slotData.parts;

        if (!isSprite)
        {

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

                ReleaseRemoveHandles(slotData);

                tasks = new UniTask[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    tasks[i] = parts[i].PlayAnimation(SLOT_INTRO, token);
                }

                await UniTask.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
                ReleaseCurrentHandles(slotData);
                ReleaseRemoveHandles(slotData);
            }
            catch (Exception e)
            {
                Logger.LogError($"[SetCharacterAsync] 실패 - char:{characterId}, expr:{expression}\n{e}");
                ReleaseCurrentHandles(slotData);
            }
        }
        else
        {
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i].SetInactive();
            }

            string path = animConfig.GetSpritePath(characterId, expression);
            await characterLoader.LoadPortaitSpriteAsync(slotData, path, token);
        }
    }



    public void OnDialogueStart(ESlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;
        foreach (var part in slotData.parts)
            part.OnDialogueStart();
    }
    public void OnDialogueStart(string speaker)
    {
        foreach (var slot in slotParts)
        {
            if(slot.slotCharacterName == speaker)
            {
                foreach (var part in slot.parts)
                    part.OnDialogueStart();
            }
        }
    }

    public void OnDialogueEnd(ESlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;
        foreach (var part in slotData.parts)
            part.OnDialogueEnd();
    }
    public void OnDialogueEnd(string speaker)
    {
        foreach (var slot in slotParts)
        {
            if (slot.slotCharacterName == speaker)
            {
                foreach (var part in slot.parts)
                    part.OnDialogueEnd();
            }
        }
    }
    public void OnDialogueEnd()
    {
        foreach (var slot in slotParts)
        {
            foreach (var part in slot.parts)
                part.OnDialogueEnd();

        }
    }

    public void ResetCharacter(ESlotType slot)
    {
        if (!_slotMap.TryGetValue(slot, out var slotData)) return;

        slotData.slotCharacterName = "";
        slotData.expression = "";

        foreach (var part in slotData.parts)
            part.SetInactive();

        if (slotData.portaitSpriteRenderer != null)
            slotData.portaitSpriteRenderer.sprite = null;

        ReleaseCurrentHandles(slotData);
        ReleaseRemoveHandles(slotData);
    }
    public void ResetCharacter()
    {
        foreach (var slot in slotParts)
        {
            ResetCharacter(slot.type);
        }
    }


    public async UniTask FadeInAsync(ESlotType slot, CancellationToken token)
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
    public async UniTask FadeOutAsync(ESlotType slot, CancellationToken token)
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




    private void MoveCurrentHandlesToRemove(SlotCharacterPart slot)
    {
        while (slot.spriteHandles.Count > 0)
            slot.spriteRemoveHandles.Push(slot.spriteHandles.Pop());

        while (slot.animHandles.Count > 0)
            slot.animRemoveHandles.Push(slot.animHandles.Pop());
    }

    
    private void ReleaseRemoveHandles(SlotCharacterPart slot)
    {
        while (slot.spriteRemoveHandles.Count > 0)
        {
            var item = slot.spriteRemoveHandles.Pop();
            ResourceLoader.ReleaseHandle<Sprite>(ref item);
        }

        while (slot.animRemoveHandles.Count > 0)
        {
            var item = slot.animRemoveHandles.Pop();
            ResourceLoader.ReleaseHandle<AnimationClip>(ref item);
        }
    }

    private void ReleaseCurrentHandles(SlotCharacterPart slot)
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

    public void ReleaseAll()
    {
        foreach (var slot in slotParts)
        {
            ReleaseCurrentHandles(slot);
            ReleaseRemoveHandles(slot);
            foreach (var part in slot.parts)
                part.Release();
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
