using System;
using System.Threading;
using UnityEngine;




[Serializable]
public class SpriteAnimationManager : AnimationPart
{

    public override void ApplySprite(Sprite sprite)
    {
        animator.enabled = false;
        spriteRenderer.sprite = sprite;
    }

    public override void PlayAnimation(string animName, CancellationToken token)
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
