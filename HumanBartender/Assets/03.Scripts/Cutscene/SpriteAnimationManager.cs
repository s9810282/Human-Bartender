using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;




[Serializable]
/// <summary>
/// AnimationPart를 상속받아 스프라이트 애니메이션 클립을 Animator로 재생하는 컴포넌트.
/// ApplySprite()로 단일 스프라이트 표시, PlayAnimation()으로 애니메이션 클립을 재생한다.
/// </summary>
public class SpriteAnimationManager : AnimationPart
{
    public override void ApplySprite(Sprite sprite)
    {
        animator.enabled = false;
        spriteRenderer.sprite = sprite;
    }

    public override async UniTask PlayAnimation(string animName, CancellationToken token)
    {
        Vector3 vec = Camera.main.transform.position;
        vec.z = 0;

        animator.transform.localPosition = vec;

        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(animName, 0, 0f);
    }
    public void ActiveSelf(bool active)
    {
        animator.gameObject.SetActive(active);
    }
}
