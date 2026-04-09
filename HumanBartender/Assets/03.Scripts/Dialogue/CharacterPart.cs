using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;


public interface IPlaybackPolicy
{
    void OnPlay(AnimationPart animPart);
    void OnDialogueStart(AnimationPart animPart);
    void OnDialogueEnd(AnimationPart animPart);
}

public class AlwaysPlayback : IPlaybackPolicy
{
    public void OnDialogueEnd(AnimationPart animPart)
    {
        
    }

    public void OnDialogueStart(AnimationPart animPart)
    {
        
    }

    public void OnPlay(AnimationPart animPart)
    {
        
    }
}


[Serializable]
public abstract class AnimationPart
{
    [SerializeField] public string partName;
    [SerializeField] protected Animator Animator;
    [SerializeField] protected SpriteRenderer SpriteRenderer;

    [SerializeField] protected RuntimeAnimatorController BaseController;

    protected AnimatorOverrideController _overrideController;
    
    public void Initialize()
    {
        _overrideController = new AnimatorOverrideController(BaseController);
        Animator.runtimeAnimatorController = _overrideController;
        Animator.enabled = false;
    }

    public void SetSpeed(float s) => Animator.speed = s;
    public void SetInactive() { Animator.enabled = false; SpriteRenderer.sprite = null; }
    public void SetActive() { Animator.enabled = true; SpriteRenderer.sprite = null; }

    public virtual void SetClip(string slot, AsyncOperationHandle<AnimationClip>? handle)
    {
        _overrideController[slot] = handle.Value.Result;
    }
    public virtual void SetClip(string slot, AnimationClip handle) => _overrideController[slot] = handle;

    public abstract void PlayAnimation(string animName, CancellationToken token);
    public abstract void ApplySprite(Sprite? sprite);

    public virtual void Release()
    {
        if (_overrideController != null)
        {
            UnityEngine.Object.Destroy(_overrideController);
            _overrideController = null;
        }
    }
}



    [Serializable]
public class CharacterPart : AnimationPart
{
     private string _currentLoopMode;

    public void SetLoopMode(string loopMode)
    {
        SpriteRenderer.sprite = null;
        _currentLoopMode = loopMode;
    }

    public override void PlayAnimation(string animName, CancellationToken token)
    {
        //Play Animation,  IPlaybackPolicy.OnPlay로 변경 예정

        Animator.enabled = true;
        switch (_currentLoopMode)
        {
            case "always":
                Animator.speed = 1f;
                Animator.Play(animName, 0, 0f);
                break;

            case "on_dialogue":
                Animator.Play(animName, 0, 0f);
                Animator.speed = 0f;
                break;

            case "once":
                Animator.speed = 1f;
                Animator.Play(animName, 0, 0f);
                WaitAndFreezeAsync(token).Forget();
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

    private async UniTaskVoid WaitAndFreezeAsync(CancellationToken token)
    {
        for(int i = 0; i <  _overrideController.animationClips.Length; i++)
        {
            await UniTask.WaitForSeconds(_overrideController.animationClips[i].length);
        }

        if (Animator != null)
            Animator.speed = 0f;
    }


    public override void ApplySprite(Sprite sprite)
    {
        Animator.enabled = false;
        SpriteRenderer.sprite = sprite;
    }


    // Dialogue
    //IPlaybackPolicy.OnDialogueStart
    public void OnDialogueStart()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.enabled = true;
        Animator.speed = 1f;
    }

    //IPlaybackPolicy.OnDialogueStart
    public void OnDialogueEnd()
    {
        if (_currentLoopMode != "on_dialogue") return;
        Animator.speed = 0f;
    }


    public override void Release()//파괴 시
    {
        if (_overrideController != null)
        {
            UnityEngine.Object.Destroy(_overrideController);
            _overrideController = null;
        }
    }
}
