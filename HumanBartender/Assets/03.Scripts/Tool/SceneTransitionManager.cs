using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private bool isFading = false;

    private void Awake()
    {
        fadeCanvasGroup.alpha = 1f;

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            FadeInAsync().Forget();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── 씬 로드/언로드 ─────────────────────────────────────────────

    public void LoadScene(string sceneName, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        if (isFading) return;
        LoadSceneAsync(sceneName, sceneMode).Forget();
    }

    public void UnLoadScene(string sceneName)
    {
        if (isFading) return;
        UnLoadSceneAsync(sceneName).Forget();
    }

    // ── 페이드 퍼블릭 API ──────────────────────────────────────────

    public void FadeIn(float duration = -1f)
    {
        if (isFading) return;
        FadeInAsync(duration).Forget();
    }

    public void FadeOut(float duration = -1f)
    {
        if (isFading) return;
        FadeOutAsync(duration).Forget();
    }

    // await 가 필요한 경우 직접 호출
    public UniTask FadeInAsync(float duration = -1f)  => FadeAsync(0f, duration < 0 ? fadeDuration : duration);
    public UniTask FadeOutAsync(float duration = -1f) => FadeAsync(1f, duration < 0 ? fadeDuration : duration);

    // ── 내부 구현 ──────────────────────────────────────────────────

    private async UniTask LoadSceneAsync(string sceneName, LoadSceneMode sceneMode)
    {
        isFading = true;

        await FadeAsync(1f, fadeDuration);
        await SceneManager.LoadSceneAsync(sceneName, sceneMode);
        await FadeAsync(0f, fadeDuration);

        isFading = false;
    }

    private async UniTask UnLoadSceneAsync(string sceneName)
    {
        isFading = true;

        await FadeAsync(1f, fadeDuration);
        await SceneManager.UnloadSceneAsync(sceneName);
        await FadeAsync(0f, fadeDuration);

        isFading = false;
    }

    private async UniTask FadeAsync(float targetAlpha, float duration)
    {
        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;

        await fadeCanvasGroup.DOFade(targetAlpha, duration)
                             .SetEase(Ease.Linear)
                             .ToUniTask();

        fadeCanvasGroup.blocksRaycasts = false;
        isFading = false;
    }
}
