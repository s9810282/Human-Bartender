using Cysharp.Threading.Tasks;
using System.Net;
using System.Threading;
using UnityEngine;

public class CharacterLoader
{
    private const string SLOT_INTRO = "Intro";
    private const string SLOT_LOOP = "Loop";
    private const string SLOT_DIALOGUE = "Dialogue";


    public async UniTask PartAnimationSyncStart(CharacterPart part, CancellationToken token)
    {
        part.PlayAnimation(SLOT_INTRO, token);
    }


    //************************************************************************************//
    // 스크립트 기능 분리 검토. 단순 컴포지션 분리
    /// <summary>
    /// 애니메이션 로드시도 :  loop 클립 로드 -> intro 클립 로드 시도 -> Part.Applyanimaton
    /// 스프라이트 로드시도 : 해당 파츠 Sprite 로드 시도 -> Part.ApplySprite
    /// 디폴트 스프라이트 로드 시도 : 존재 여부 확인 후 -> Part.ApplySprite
    /// 
    /// 검사로직 수정 필요. Data 자체가 Null일 경우 default로 돌리게 만들기
    /// 모두 실패한다면 SetInactive()
    /// 
    /// 파츠 현재 애님이랑 비교해서 같다면 스킵하기
    /// 
    /// </summary>
    /// <param name="part"></param>
    /// <param name="data"></param>
    /// <param name="defaultData"></param>
    /// <returns></returns>
    public async UniTask LoadPartAsync(
        SlotCharacterPart slot,
        CharacterPart part,
        PartAnimData data,
        PartAnimData defaultData,
        CancellationToken token)
    {
        if (data == null)
        {
            Logger.LogWarning($"[CharacterManager] PartAnimData null, Default Data");
            
        }
        else if (data.Loop == "none")
        {
            part.SetInactive();

            if (slot.portaitSpriteRenderer != null)
                slot.portaitSpriteRenderer.sprite = null;

            return;
        }

        if (part.partCurAnim == data.Clip) // 이미 실행 중
            return;

        if (await LoadAnimAsync(slot, part, data, token)) //Part Anim
            return;

        if (await LoadAnimAsync(slot, part, defaultData, token)) //Part Default Anim
            return;

        if (await LoadSpriteAsync(slot, part, data, token)) //Part Sprite
            return;

        if (await LoadSpriteAsync(slot, part, defaultData, token)) //Part Default Sprite
            return;


        if (defaultData == null) return;

        if (part.partName != "body")  //body의 경우만 Portail Image 로드 시도.
        {
            part.SetInactive();
            return;
        }

        if (await LoadPortaitSpriteAsync(slot, defaultData, token))
            return;

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
        if (data == null) //clipData가 null이면 false, 추후 default anim 삽입.
        {
            Logger.LogWarning($"{part.partName} Null");
            return false;
        }
        
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

            part.partCurAnim = clipAddress;
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

        if (data == null)
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
}
