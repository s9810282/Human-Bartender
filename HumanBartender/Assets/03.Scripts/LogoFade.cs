using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 씬 전환 시 로고를 페이드 인/아웃하며 이동시키는 연출 컴포넌트.
/// ShowLogo() 호출 시 페이드 → 로고 이동 → 유지 → 페이드 아웃 순서로 비동기 처리된다.
/// </summary>
public class LogoFade : MonoBehaviour
{
    [SerializeField] CanvasGroup logoGroup;
    [SerializeField] CanvasGroup fadeCanvasGroup;

    [SerializeField] RectTransform targetRect;
    [SerializeField] private Vector2 targetAnchorPosition;
    [SerializeField] private Vector2 targetOriginPosition;
    [SerializeField] private Ease moveEaseGraph = Ease.Linear;
    [SerializeField] private float moveDuration = 1f;

    [SerializeField] float fadeDuration = 1f;
    [SerializeField] float logoDuration = 1f;

    [SerializeField] bool isFading = false;

    void Start()
    {
        fadeCanvasGroup.blocksRaycasts = false;
        isFading = false;

        fadeCanvasGroup.alpha = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowLogo()
    {
        if (isFading) return;
        LoadSceneAsync().Forget();
    }


    private async UniTask LoadSceneAsync()
    {
        isFading = true;
        targetRect.anchoredPosition = targetOriginPosition;
        targetRect.gameObject.SetActive(true);


        await FadeCanvasAsync(1f, fadeDuration);

        targetRect.DOKill();
        targetRect.DOAnchorPos(targetAnchorPosition, moveDuration)
                  .SetEase(moveEaseGraph);

        await FadeLogoAsync(1f, fadeDuration);

        await UniTask.Delay(TimeSpan.FromSeconds(logoDuration));

        await FadeCanvasAsync(0f, fadeDuration);
        await FadeLogoAsync(0f, fadeDuration);

        isFading = false;
    }


    private async UniTask FadeCanvasAsync(float targetAlpha, float duration)
    {
        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;

        await fadeCanvasGroup.DOFade(targetAlpha, duration)
                             .SetEase(Ease.Linear)
                             .ToUniTask();

        fadeCanvasGroup.blocksRaycasts = false;
        isFading = false;
    }

    private async UniTask FadeLogoAsync(float targetAlpha, float duration)
    {
        isFading = true;
        logoGroup.blocksRaycasts = true;

        await logoGroup.DOFade(targetAlpha, duration)
                             .SetEase(Ease.Linear)
                             .ToUniTask();

        logoGroup.blocksRaycasts = false;
        isFading = false;
    }
}
