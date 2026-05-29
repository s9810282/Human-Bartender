using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
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
    [SerializeField] public EAnimationPart partName;
    [SerializeField] public string partCurAnim;
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [SerializeField] protected RuntimeAnimatorController baseController;

    protected const float TARGET_LENGTH = 1f;
    protected const string CLIP_SPEED = "ClipSpeed";

    protected float curAnimSpeed = 0.3f;

    protected AnimatorOverrideController _overrideController;
    
    private readonly List<KeyValuePair<AnimationClip, AnimationClip>> _overrides
       = new List<KeyValuePair<AnimationClip, AnimationClip>>();

    public void Initialize()
    {
        _overrideController = new AnimatorOverrideController(baseController);
        animator.runtimeAnimatorController = _overrideController;
        animator.enabled = false;
    }

    public void SetSpeed(float s) => animator.speed = s;
    public void SetClipSpeed(float f)
    {
        float calculatedSpeed = f / TARGET_LENGTH;
        curAnimSpeed = calculatedSpeed;
        animator.SetFloat(CLIP_SPEED, calculatedSpeed);
    }
    public void SetInactive() 
    { 
        Logger.Log($"{partName.ToString()} SetInactive");  
        animator.enabled = false; 
        spriteRenderer.sprite = null;

        ClearAllClips();
    }
    public void SetActive() { animator.enabled = true; spriteRenderer.sprite = null; }

    public virtual void SetClip(string slot, AsyncOperationHandle<AnimationClip>? handle)
    {
        _overrideController[slot] = handle.Value.Result;
    }
    public virtual void SetClip(string slot, AnimationClip handle) => _overrideController[slot] = handle;

    public abstract UniTask PlayAnimation(string animName, CancellationToken token);
    public abstract void ApplySprite(Sprite? sprite);

    public void ClearAllClips()
    {
        _overrides.Clear();
        _overrideController.GetOverrides(_overrides);

        for (int i = 0; i < _overrides.Count; i++)
        {
            _overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(_overrides[i].Key, null);
        }

        _overrideController.ApplyOverrides(_overrides);
    }
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
public class CharacterPart : AnimationPart, IFade
{
     [SerializeField] EAnimLoopMode _currentLoopMode;

    public void SetLoopMode(EAnimLoopMode loopMode)
    {
        spriteRenderer.sprite = null;
        _currentLoopMode = loopMode;
    }

    public override async UniTask PlayAnimation(string animName, CancellationToken token)
    {
        var Clip = _overrideController[animName];
        
        //TargetDuration을 State 별로 두기
        SetClipSpeed(Clip.length);
        
        animator.enabled = true;
        switch (_currentLoopMode)
        {
            case EAnimLoopMode.Always_OnDialogue:
            case EAnimLoopMode.Always:
                animator.speed = 1f;
                animator.Play(animName, 0, 0f);
                break;

            case EAnimLoopMode.Special_OnDialogue:
                animator.speed = 1f;
                animator.Play(animName, 0, 0f);
                await WaitAndDelayAsync(animName, token);
                break;

            case EAnimLoopMode.Once:
                animator.speed = 1f;
                animator.Play(animName, 0, 0f);
                await WaitAndFreezeAsync(animName, token);
                break;
        }

        return;
    }


    /// <param name="introClip"></param>
    /// <param name="loopClip"></param>
    /// <returns></returns>

    private async UniTask WaitAndFreezeAsync(string animName, CancellationToken token)
    {
        await UniTask.Yield(PlayerLoopTiming.Update, token);
        
        await UniTask.WaitUntil(() =>
        {
            if (animator == null) return true;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(animName) && stateInfo.normalizedTime >= 0.99f;
        }, cancellationToken: token);

        if (animator != null) animator.speed = 0f;
    }

    private async UniTask WaitAndDelayAsync(string animName, CancellationToken token)
    {
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        await UniTask.WaitUntil(() =>
        {
            if (animator == null) return true;
            var stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return stateInfo.IsName(animName) && stateInfo.normalizedTime >= 0.99f;
        }, cancellationToken: token);

        animator.Play("Loop", 0, 0f);
    }

    public override void ApplySprite(Sprite sprite)
    {
        animator.enabled = false;
        spriteRenderer.sprite = sprite;
    }

    public async UniTask FadeIn(CancellationToken token)
    {
        spriteRenderer.color = new Color(0, 0, 0, 1);
        await spriteRenderer.DOColor(Color.white, 1f).ToUniTask();
    }
    public async UniTask FadeOut(CancellationToken token)
    {
        spriteRenderer.color = new Color(1, 1, 1, 1);
        await spriteRenderer.DOColor(new Color(0, 0, 0, 0), 1f).ToUniTask();
    }



    //지금 보니 이걸 여기서 체크하는건 모순인데

    public void OnDialogueStart()
    {
        if (_currentLoopMode == EAnimLoopMode.Once) return;

        if (partName != EAnimationPart.Lower_Face)
        {
            animator.speed = 0;
            return;
        }

        animator.Play("Dialogue", 0, 0f);

    }

    public void OnDialogueEnd()
    {
        if (_currentLoopMode == EAnimLoopMode.Once) return;
 
        animator.speed = 1;
        animator.Play("Loop", 0, 0f);
    }

    public void OnAnimation()
    {
        animator.enabled = true;
        animator.speed = 1f;
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
