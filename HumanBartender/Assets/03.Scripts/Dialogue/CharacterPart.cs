using Cysharp.Threading.Tasks;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public class CharacterPart
{
    [Tooltip("eyes / mouth / body — JSON key와 일치")]
    public string PartName;

    public Animator Animator;

    [Tooltip("ExpressionIntro → ExpressionLoop 두 상태를 가진 Base Controller")]
    public RuntimeAnimatorController BaseController;

    [SerializeField] public SpriteRenderer SpriteRenderer;

    private const string SLOT_INTRO = "ExpressionIntro";
    private const string SLOT_LOOP  = "ExpressionLoop";

    private AnimatorOverrideController _overrideController;

    // 성공한 핸들만 저장 (릴리즈 추적용)
    private AsyncOperationHandle<AnimationClip>? _handleIntro;
    private AsyncOperationHandle<AnimationClip>? _handleLoop;
    private AsyncOperationHandle<Sprite>?         _handleSprite;

    private string _currentLoopMode;

    // ── 초기화 ───────────────────────────────────────────────────────────

    public void Initialize()
    {
        _overrideController = new AnimatorOverrideController(BaseController);
        Animator.runtimeAnimatorController = _overrideController;
        Animator.enabled = false;
    }

    // ── 로드 ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 로드 순서:
    ///   1. {clip}_Intro  존재 → 1회 재생 (없으면 skip)
    ///   2. {clip}_Loop   존재 → LoopMode에 따라 재생
    ///   3. Loop 없음     → Sprite {clip} 로드
    ///   4. 전부 없음     → false 반환 (호출부에서 default 폴백)
    /// </summary>
    public async UniTask<bool> LoadAsync(PartAnimData data)
    {
        if (data == null || data.Loop == "none")
        {
            SetInactive();
            return true; // None은 의도적 비활성 → 폴백 불필요
        }

        _currentLoopMode = data.Loop;

        string clipAddress  = data.Clip;
        string introAddress = $"{clipAddress}_Intro";
        string loopAddress  = $"{clipAddress}_Loop";

        // ── 1. Loop 로드 시도 ─────────────────────────────────────────────
        var loopHandle = await TryLoadAsync<AnimationClip>(loopAddress);

        if (loopHandle.HasValue)
        {
            // LoadAsync 성공 직후 임시로 추가
            var clip = loopHandle.Value.Result;
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                Debug.Log($"Path: '{binding.path}' / Property: {binding.propertyName}");

            // ── 2. Intro 로드 시도 (없으면 Loop로 대체) ──────────────────
            var introHandle = await TryLoadAsync<AnimationClip>(introAddress);

            ReleaseHandles();
            _handleLoop  = loopHandle;
            _handleIntro = introHandle; // null이면 그냥 null

            

            AnimationClip introClip = introHandle.HasValue
                ? introHandle.Value.Result
                : loopHandle.Value.Result;

            ApplyAnimation(introClip, loopHandle.Value.Result);
            return true;
        }

        // ── 3. Sprite 로드 시도 ───────────────────────────────────────────
        var spriteHandle = await TryLoadAsync<Sprite>(clipAddress);

        if (spriteHandle.HasValue)
        {
            ReleaseHandles();
            _handleSprite = spriteHandle;

            ApplySprite(spriteHandle.Value.Result);
            return true;
        }

        // ── 4. 리소스 없음 ────────────────────────────────────────────────
        Logger.LogWarning($"[CharacterPart:{PartName}] '{clipAddress}' 리소스 없음");
        return false;
    }

    // ── 외부 제어 ────────────────────────────────────────────────────────

    public void OnDialogueStart()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.enabled = true;
        Animator.speed   = 1f;
    }

    public void OnDialogueEnd()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.speed = 0f;
    }

    // ── 내부 적용 ────────────────────────────────────────────────────────

    private void ApplyAnimation(AnimationClip introClip, AnimationClip loopClip)
    {
        SpriteRenderer.sprite = null;

        _overrideController[SLOT_INTRO] = introClip;
        _overrideController[SLOT_LOOP]  = loopClip;

        

        Logger.Log("Intro 확인 후 실행");
        Logger.Log($"{introClip.name}  {loopClip.name}  {_currentLoopMode}");

        Animator.enabled = true;

        switch (_currentLoopMode)
        {
            case "always":
                Animator.speed = 1f;
                Animator.Play(SLOT_INTRO, 0, 0f);
                break;

            case "on_dialogue":
                Animator.Play(SLOT_INTRO, 0, 0f);
                Animator.speed = 0f;
                break;

            case "once":
                Animator.speed = 1f;
                Animator.Play(SLOT_INTRO, 0, 0f);
                WaitAndFreezeAsync(introClip, loopClip).Forget();
                break;
        }
    }

    private void ApplySprite(Sprite sprite)
    {
        Animator.enabled      = false;
        SpriteRenderer.sprite = sprite;
    }

    private void SetInactive()
    {
        Animator.enabled      = false;
        SpriteRenderer.sprite = null;
    }

    /// <summary>Once 모드: Intro + Loop 1회 재생 후 마지막 프레임 정지</summary>
    private async UniTaskVoid WaitAndFreezeAsync(AnimationClip introClip, AnimationClip loopClip)
    {
        await UniTask.WaitForSeconds(introClip.length);
        await UniTask.WaitForSeconds(loopClip.length);

        if (Animator != null)
            Animator.speed = 0f;
    }

    // ── 로드 유틸 ────────────────────────────────────────────────────────

    /// <summary>
    /// 로드 성공 시 handle 반환, 실패/예외 시 null 반환.
    /// 실패한 handle은 내부에서 즉시 릴리즈.
    /// </summary>
    private static async UniTask<AsyncOperationHandle<T>?> TryLoadAsync<T>(string address)
    {
        var handle = Addressables.LoadAssetAsync<T>(address);
        try
        {
            await handle.ToUniTask();

            if (handle.Status == AsyncOperationStatus.Succeeded)
                return handle;

            // 로드 실패 — 핸들 즉시 릴리즈
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
        catch (Exception)
        {
            Logger.Log($"{address} 미 존재");
            // 주소 없음 (InvalidKeyException 등) — 정상 케이스
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }
    }

    // ── 릴리즈 ───────────────────────────────────────────────────────────

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
        ReleaseHandle(ref _handleSprite);
    }

    private static void ReleaseHandle<T>(ref AsyncOperationHandle<T>? handle)
    {
        if (handle.HasValue && handle.Value.IsValid())
        {
            Addressables.Release(handle.Value);
            handle = null;
        }
    }
}
