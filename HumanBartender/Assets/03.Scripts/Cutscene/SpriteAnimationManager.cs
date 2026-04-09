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
        Animator.enabled = false;
        SpriteRenderer.sprite = sprite;
    }

    public override void PlayAnimation(string animName, CancellationToken token)
    {
        Animator.enabled = true;
        Animator.speed = 1f;
        Animator.Play(animName, 0, 0f);
    }
    public void ActiveSelf(bool active)
    {
        Animator.gameObject.SetActive(active);
    }
}
