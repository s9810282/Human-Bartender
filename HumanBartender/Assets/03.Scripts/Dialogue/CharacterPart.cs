using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 캐릭터 파트(Body / Eyes / Mouth) 하나를 담당.
/// Intro(1회) → Loop 구조를 AnimatorOverrideController로 처리.
/// </summary>
[Serializable]
public class CharacterPart
{
    [Tooltip("Body / Eyes / Mouth 등 파트 식별자 (주소 생성에 사용)")]
    public string PartName;

    public Animator Animator;

    [Tooltip("ExpressionIntro → ExpressionLoop 두 상태를 가진 Base Controller")]
    public RuntimeAnimatorController BaseController;

    private const string SLOT_INTRO = "ExpressionIntro";
    private const string SLOT_LOOP  = "ExpressionLoop";

    private AnimatorOverrideController _overrideController;
    private AsyncOperationHandle? _handleIntro;
    private AsyncOperationHandle? _handleLoop;

    public void Initialize()
    {
        _overrideController = new AnimatorOverrideController(BaseController);
        Animator.runtimeAnimatorController = _overrideController;
    }

    /// <summary>
    /// {speaker}_{PartName}_{expression}_Loop 을 먼저 시도.
    /// 없으면 Animator 비활성(파트 숨김 또는 정지).
    /// </summary>
    public async UniTask LoadAsync(string speaker, string expression)
    {
        string baseAddress  = $"{speaker}_{PartName}_{expression}";
        string loopAddress  = $"{baseAddress}_Loop";
        string introAddress = $"{baseAddress}_Intro";

        var loopHandle = Addressables.LoadAssetAsync<AnimationClip>(loopAddress);
        await loopHandle.ToUniTask();

        if (loopHandle.Status != AsyncOperationStatus.Succeeded)
        {
            // 이 파트에 해당 expression 없음 → 비활성
            Addressables.Release(loopHandle);
            Animator.enabled = false;
            return;
        }

        // Intro 시도
        var introHandle = Addressables.LoadAssetAsync<AnimationClip>(introAddress);
        await introHandle.ToUniTask();

        bool hasIntro = introHandle.Status == AsyncOperationStatus.Succeeded;
        AnimationClip introClip = hasIntro ? introHandle.Result : loopHandle.Result;

        ReleaseHandles();
        _handleLoop  = loopHandle;
        _handleIntro = hasIntro ? introHandle : null;

        if (!hasIntro)
            Addressables.Release(introHandle);

        _overrideController[SLOT_INTRO] = introClip;
        _overrideController[SLOT_LOOP]  = loopHandle.Result;

        Animator.enabled = true;
        Animator.Play(SLOT_INTRO, 0, 0f);
    }

    public void Release()
    {
        ReleaseHandles();
        if (_overrideController != null)
        {
            UnityEngine.Object.Destroy(_overrideController);
            _overrideController = null;
        }
    }

    private void ReleaseHandles()
    {
        ReleaseHandle(ref _handleIntro);
        ReleaseHandle(ref _handleLoop);
    }

    private static void ReleaseHandle(ref AsyncOperationHandle? handle)
    {
        if (handle.HasValue && handle.Value.IsValid())
        {
            Addressables.Release(handle.Value);
            handle = null;
        }
    }
}
