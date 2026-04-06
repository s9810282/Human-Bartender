using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

public class SpriteAnimationManager : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator spriteAnimator;

    [SerializeField] RuntimeAnimatorController BaseController;

    private AnimatorOverrideController _overrideController;

    private const string ANIM_SLOT = "SpriteAnim";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _overrideController = new AnimatorOverrideController(BaseController);
        spriteAnimator.runtimeAnimatorController = _overrideController;
        spriteAnimator.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public async UniTask PlayAnimation(string id)
    {
        await LoadAsync(id);
    }    

    private async UniTask<bool> LoadAsync(string id)
    {
        if (id == "")
        {
            SetInactive();
            return false;
        }

        var handle = await TryLoadAsync<AnimationClip>(id);

        if(handle.HasValue)
        {
            await ApplyAnimation(handle.Value.Result);
            return true;
        }   
        else
        {
            Logger.Log($"해당 애니메이션 {id} 파일 미존재, 리소스 체크 요망");
            return false;
        }
    }

    private async UniTask ApplyAnimation(AnimationClip clip)
    {
        spriteRenderer.sprite = null;
        spriteAnimator.gameObject.SetActive(true);

        _overrideController[ANIM_SLOT] = clip;


        spriteAnimator.enabled = true;

        spriteAnimator.speed = 1f;
        spriteAnimator.Play(ANIM_SLOT, 0, 0f);

        await UniTask.WaitForSeconds(clip.length);

        if (spriteAnimator != null)
            spriteAnimator.speed = 0f;

        return;
    }

    public void SetInactive()
    {
        spriteAnimator.enabled = false;
        spriteRenderer.sprite = null;
        spriteAnimator.gameObject.SetActive(false);
    }


    /// <summary>
    /// 로드 성공 시 handle 반환, 실패/예외 시 null 반환.
    /// 실패한 handle은 내부에서 즉시 릴리즈.
    /// TODO : ExistsInAddressables 함수는 순수 파일 존재 여부만 검사하기에 따로 또 처리해야함.
    /// </summary>
    private async UniTask<AsyncOperationHandle<T>?> TryLoadAsync<T>(string address)
    {
        bool exists = await ExistsInAddressables(address);
        if (!exists)
        {
            Debug.LogWarning($"Addressable key not found: {address}");
            return null;
        }

        var handle = Addressables.LoadAssetAsync<T>(address);
        try
        {
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle;

            if (handle.IsValid()) Addressables.Release(handle);

            return null;
        }
        catch (Exception)
        {
            Logger.Log($"{address} 미 존재");
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
    }
    public async UniTask<bool> ExistsInAddressables(string key)
    {
        var handle = Addressables.LoadResourceLocationsAsync(key);
        IList<IResourceLocation> locations = await handle.ToUniTask();

        bool exists = locations != null && locations.Count > 0;

        Addressables.Release(handle);
        return exists;
    }

    public void Release()
    {
        if (_overrideController != null)
        {
            UnityEngine.Object.Destroy(_overrideController);
            _overrideController = null;
        }
    }


}
