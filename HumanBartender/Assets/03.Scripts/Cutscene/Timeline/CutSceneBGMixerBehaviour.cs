using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class CutSceneBGMixerBehaviour : PlayableBehaviour
{
    private CutSceneTimelineManager manager;
    private string currentBgPath;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        manager = playerData as CutSceneTimelineManager;
        if (manager == null || manager.BgImage == null) return;

        int inputCount = playable.GetInputCount();

        for (int i = 0; i < inputCount; i++)
        {
            float weight = playable.GetInputWeight(i);
            var inputPlayable = (ScriptPlayable<CutSceneBGBehaviour>)playable.GetInput(i);
            var behaviour = inputPlayable.GetBehaviour();
            behaviour.manager = manager;

            if (weight > 0f && !behaviour.isActive)
            {
                behaviour.isActive = true;
                behaviour.fadeOutStarted = false;

                Sprite sprite = behaviour.LoadSprite();
                if (sprite == null) continue;

                Image bg = manager.BgImage;

                if (behaviour.bgPath != currentBgPath)
                {
                    currentBgPath = behaviour.bgPath;
                    TransitionIn(bg, sprite, behaviour).Forget();
                }
            }
            else if (weight <= 0f && behaviour.isActive)
            {
                behaviour.isActive = false;

                if (!behaviour.fadeOutStarted)
                {
                    behaviour.fadeOutStarted = true;

                    bool hasNext = HasActiveClipAfter(playable, i);

                    if (!hasNext)
                    {
                        currentBgPath = null;
                        TransitionOut(manager.BgImage, behaviour).Forget();
                    }
                }
            }
        }
    }

    /// <summary> 현재 클립 이후에 활성화될 클립이 있는지 확인 </summary>
    bool HasActiveClipAfter(Playable playable, int currentIndex)
    {
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            if (i == currentIndex) continue;

            float weight = playable.GetInputWeight(i);
            if (weight > 0f) return true;

            // 아직 weight가 0이지만 바로 다음에 활성화될 클립
            var input = (ScriptPlayable<CutSceneBGBehaviour>)playable.GetInput(i);
            var b = input.GetBehaviour();
            if (!b.isActive && !b.fadeOutStarted) 
            {
                // 이 클립이 아직 시작 안 했으면 다음 클립으로 간주
                // (weight 체크만으로 충분하므로 여기서는 skip)
            }
        }
        return false;
    }

    async UniTaskVoid TransitionIn(Image bg, Sprite sprite, CutSceneBGBehaviour behaviour)
    {
        bg.DOKill();

        if (behaviour.fadeInDuration > 0)
        {
            bg.color = new Color(behaviour.tint.r, behaviour.tint.g, behaviour.tint.b, 0);
            bg.sprite = sprite;
            bg.gameObject.SetActive(true);
            await bg.DOFade(behaviour.tint.a, behaviour.fadeInDuration).ToUniTask();
        }
        else
        {
       
            bg.sprite = sprite;
            bg.color = behaviour.tint;
            bg.gameObject.SetActive(true);
        }
    }

    async UniTaskVoid TransitionOut(Image bg, CutSceneBGBehaviour behaviour)
    {
        bg.DOKill();

        if (behaviour.fadeOutDuration > 0)
        {
            await bg.DOFade(0f, behaviour.fadeOutDuration).ToUniTask();
            bg.gameObject.SetActive(false);
        }
        else
        {
            bg.color = new Color(1, 1, 1, 0);
            bg.gameObject.SetActive(false);
        }

        bg.sprite = null;
    }

    public override void OnPlayableDestroy(Playable playable)
    {
        if (manager != null)
            manager.ClearBackground();

        currentBgPath = null;
    }
}
