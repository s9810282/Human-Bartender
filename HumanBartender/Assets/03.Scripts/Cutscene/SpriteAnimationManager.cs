using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.U2D;




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
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(animName, 0, 0f);
    }
    public void ActiveSelf(bool active)
    {
        animator.gameObject.SetActive(active);
    }
}
