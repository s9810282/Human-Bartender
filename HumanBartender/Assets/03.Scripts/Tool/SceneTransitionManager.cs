using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;

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
            FadeIn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string sceneName, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        if (isFading) return;
        StartCoroutine(FadeAndLoadScene(sceneName, sceneMode));
    }
    public void UnLoadScene(string sceneName, LoadSceneMode sceneMode = LoadSceneMode.Single)
    {
        if (isFading) return;
        StartCoroutine(FadeAndUnLoadScene(sceneName));
    }

    public void FadeOut(Action onFadeComplete = null)
    {
        if (isFading) return;
        StartCoroutine(Fade(1f, fadeDuration, onFadeComplete));
    }

    public void FadeIn(Action onFadeComplete = null)
    {
        if (isFading) return;
        StartCoroutine(Fade(0f, fadeDuration, onFadeComplete));
    }

    public void FadeOut(float duration, Action onFadeComplete = null)
    {
        if (isFading) return;
        StartCoroutine(Fade(1f, duration, onFadeComplete));
    }

    public void FadeIn(float duration, Action onFadeComplete = null)
    {
        if (isFading) return;
        StartCoroutine(Fade(0f, duration, onFadeComplete));
    }


    private IEnumerator FadeAndLoadScene(string sceneName , LoadSceneMode sceneMode)
    {
        isFading = true;
        yield return StartCoroutine(Fade(1f));

        yield return SceneManager.LoadSceneAsync(sceneName, sceneMode);

        yield return StartCoroutine(Fade(0f, 3f));
        isFading = false;
    }
    private IEnumerator FadeAndUnLoadScene(string sceneName)
    {
        isFading = true;
        yield return StartCoroutine(Fade(1f));

        yield return SceneManager.UnloadSceneAsync(sceneName);

        yield return StartCoroutine(Fade(0f, 3f));
        isFading = false;
    }


    // 기존 Fade 코루틴을 콜백을 받도록 수정
    private IEnumerator Fade(float targetAlpha, float duration = 1f, Action onFadeComplete = null)
    {
        isFading = true;
        fadeCanvasGroup.blocksRaycasts = true;

        float speed = Mathf.Abs(fadeCanvasGroup.alpha - targetAlpha) / duration;
        while (!Mathf.Approximately(fadeCanvasGroup.alpha, targetAlpha))
        {
            fadeCanvasGroup.alpha = Mathf.MoveTowards(fadeCanvasGroup.alpha, targetAlpha, speed * Time.deltaTime);
            yield return null;
        }

        onFadeComplete?.Invoke();

        fadeCanvasGroup.blocksRaycasts = false;
        isFading = false;
    }
}