using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;


[Serializable]
public class CharacterPart
{
    [Tooltip("JSON key와 일치하는 Name")]
    [SerializeField] public string partName;
    [SerializeField] Animator Animator;
    [SerializeField] SpriteRenderer SpriteRenderer;

    [Tooltip("Base Controller")]
    public RuntimeAnimatorController BaseController;

    private const string SLOT_INTRO = "Intro";
    private const string SLOT_LOOP  = "Loop";

    private AnimatorOverrideController _overrideController;
    private string _currentLoopMode;


    private AsyncOperationHandle<Sprite>? _spriteHandle;
    private AsyncOperationHandle<AnimationClip>? _introHandle;
    private AsyncOperationHandle<AnimationClip>? _loopHandle;


    public void Initialize()
    {
        _overrideController = new AnimatorOverrideController(BaseController);
        Animator.runtimeAnimatorController = _overrideController;
        Animator.enabled = false;
    }

    public void SetLoopMode(string loopMode)
    {
        _currentLoopMode = loopMode;
    }

    public void ApplyAnimation(
        AsyncOperationHandle<AnimationClip>? introHandle,
    AsyncOperationHandle<AnimationClip>? loopHandle)
    {
        ReleaseCurrentHandles();

        _introHandle = introHandle;
        _loopHandle = loopHandle;

        SpriteRenderer.sprite = null;

        var introClip = _introHandle.HasValue ? _introHandle.Value.Result : _loopHandle.Value.Result;

        _overrideController[SLOT_INTRO] = introClip;
        _overrideController[SLOT_LOOP] = _loopHandle.Value.Result;



        Logger.Log("Intro 확인 후 실행");
        Logger.Log($"{_introHandle.Value.Result.name}  {_loopHandle.Value.Result.name}  {_currentLoopMode}");

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
                WaitAndFreezeAsync().Forget();
                break;
        }
    }

    /// <summary>
    /// clip의 loop설정을 바꾸는건 원본 자체를 건들기 때문에 No
    /// 빌드환경에서는 AnimationClip에 대한 쓰기 권한이 막히는 케이스가 존재함.
    /// </summary>
    /// <param name="introClip"></param>
    /// <param name="loopClip"></param>
    /// <returns></returns>

    private async UniTaskVoid WaitAndFreezeAsync()
    {
        await UniTask.WaitForSeconds(_overrideController[SLOT_INTRO].length);
        await UniTask.WaitForSeconds(_overrideController[SLOT_LOOP].length);

        if (Animator != null)
            Animator.speed = 0f;
    }


    public void ApplySprite(AsyncOperationHandle<Sprite>? handle)
    {
        ReleaseCurrentHandles();
        _spriteHandle = handle;

        Animator.enabled = false;
        SpriteRenderer.sprite = handle.Value.Result;
    }

    public void SetInactive()
    {
        Animator.enabled = false;
        SpriteRenderer.sprite = null;
    }


    // Dialogue

    public void OnDialogueStart()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.enabled = true;
        Animator.speed = 1f;
    }

    public void OnDialogueEnd()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.speed = 0f;
    }

    private void ReleaseCurrentHandles()
    {
        ResourceLoader.ReleaseHandle(ref _spriteHandle);
        ResourceLoader.ReleaseHandle(ref _introHandle);
        ResourceLoader.ReleaseHandle(ref _loopHandle);
    }


    public void Release()//파괴 시
    {
        ReleaseCurrentHandles();

        if (_overrideController != null)
        {
            UnityEngine.Object.Destroy(_overrideController);
            _overrideController = null;
        }
    }
}
