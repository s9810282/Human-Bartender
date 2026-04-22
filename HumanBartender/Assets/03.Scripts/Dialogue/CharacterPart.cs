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
    [SerializeField] public string partName;
    [SerializeField] public string partCurAnim;
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    [SerializeField] protected RuntimeAnimatorController baseController;

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
    public void SetInactive() 
    { 
        Logger.Log($"{partName} SetInactive");  
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
     [SerializeField] string _currentLoopMode;

    public void SetLoopMode(string loopMode)
    {
        spriteRenderer.sprite = null;
        _currentLoopMode = loopMode;
    }

    public override async UniTask PlayAnimation(string animName, CancellationToken token)
    {
        //Play Animation,  IPlaybackPolicy.OnPlay로 변경 예정, 해야하긴함.

        //alway_on_dialogue, 일반 clip 실행 중 dialogue 시 전환 예정
       
        animator.enabled = true;
        switch (_currentLoopMode)
        {
            case "always_on_dialogue":
            case "always":
                animator.speed = 1f;
                animator.Play(animName, 0, 0f);
                break;

            case "on_dialogue":
                animator.Play(animName, 0, 0f);
                animator.speed = 0f;
                break;

            case "once":
                animator.speed = 1f;
                animator.Play(animName, 0, 0f);
                WaitAndFreezeAsync(token).Forget();
                break;
        }

        return;
    }

    /// <summary>
    /// clip의 loop설정을 바꾸는건 원본 자체를 건들기 때문에 No
    /// 빌드환경에서는 AnimationClip에 대한 쓰기 권한이 막히는 케이스가 존재함.
    /// 컨트롤러 내의 속도 고려가 안되어있음.
    /// 1회 실행 타임 체크 후 speed = 0
    /// </summary>
    /// <param name="introClip"></param>
    /// <param name="loopClip"></param>
    /// <returns></returns>

    private async UniTaskVoid WaitAndFreezeAsync(CancellationToken token)
    {
        var introClip = _overrideController["Intro"];
        if (introClip != null)
            await UniTask.Delay(TimeSpan.FromSeconds(introClip.length + (introClip.length * animator.GetCurrentAnimatorStateInfo(0).speed)), 
                cancellationToken: token);

        if (animator != null) animator.speed = 0f;
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


    // Dialogue
    //IPlaybackPolicy.OnDialogueStart
    
    public void OnDialogueStart()
    {
        if (partName != "lower_face")
        {
            animator.speed = 0;
            return;
        }

        animator.Play("Dialogue", 0, 0f);
        //animator.SetBool("OnDialogue", true);
    }

    //IPlaybackPolicy.OnDialogueStart
    public void OnDialogueEnd()
    {
        //animator.enabled = true;
        //animator.SetBool("OnDialogue", false);
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
