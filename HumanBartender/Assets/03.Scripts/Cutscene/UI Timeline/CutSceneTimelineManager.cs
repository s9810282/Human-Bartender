using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class CutSceneTimelineManager : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] Canvas        cutSceneCanvas;
    [SerializeField] RectTransform canvasRect;
    [SerializeField] CanvasScaler  canvasScaler;

    [Header("CutScene Root (패닝 대상)")]
    [SerializeField] RectTransform cutSceneRoot;

    [Header("Overlay / Images")]
    [SerializeField] Image          bgImage;
    [SerializeField] Image          effectOverlay;
    [SerializeField] List<Image>    images = new();

    [Header("Dialogue Bubbles")]
    [SerializeField] List<DynamicSpeechBubble> dialogueBubbles = new();

    [Header("Padding")]
    [SerializeField] float padding_X = 0;
    [SerializeField] float padding_Y = 0;

    [Header("Timeline")]
    [SerializeField] PlayableDirector director;

    [Header("Signal")]
    [SerializeField] VoidEvent OnServeComplete;

    public RectTransform CanvasRect    => canvasRect;
    public RectTransform CutSceneRoot  => cutSceneRoot;
    public CanvasScaler  CanvasScaler  => canvasScaler;
    public Image EffectOverlay => effectOverlay;

    Queue<Image>              imagePool    = new();
    Dictionary<string, Image> activeImages = new();

    static readonly Dictionary<AnchorType, Vector2> anchorPreset = new()
    {
        { AnchorType.Center,      new Vector2(0.5f, 0.5f) },
        { AnchorType.Left,        new Vector2(0.0f, 0.5f) },
        { AnchorType.Right,       new Vector2(1.0f, 0.5f) },
        { AnchorType.TopLeft,     new Vector2(0.0f, 1.0f) },
        { AnchorType.TopRight,    new Vector2(1.0f, 1.0f) },
        { AnchorType.BottomLeft,  new Vector2(0.0f, 0.0f) },
        { AnchorType.BottomRight, new Vector2(1.0f, 0.0f) },
    };


    private bool poolInitialized;

    public SignalReceiver signalReceiver; // 인스펙터에서 할당
    public AssetReferenceT<SignalAsset> serveEndSignalRef; // 인스펙터에서 ServeEnd 에셋 할당
    public UnityEvent OnServeTimelineComplete;

    void Awake()
    {
        EnsurePoolInitialized();

        if (director != null)
            director.stopped += OnTimelineStopped;

        cutSceneRoot.anchoredPosition = Vector2.zero;

        effectOverlay.gameObject.SetActive(false);

    }

    private async void Start()
    {
        SignalAsset trueSignal = await Addressables.LoadAssetAsync<SignalAsset>(serveEndSignalRef).Task;
        signalReceiver.AddReaction(trueSignal, OnServeTimelineComplete);
    }

    void EnsurePoolInitialized()
    {
        if (poolInitialized) return;
        poolInitialized = true;

        imagePool = new Queue<Image>();
        activeImages = new();

        foreach (var img in images)
        {
            if (img != null)
            {
                img.gameObject.SetActive(false);
                imagePool.Enqueue(img);
            }
        }
    }

    void OnDestroy()
    {
        if (director != null)
            director.stopped -= OnTimelineStopped;
    }


    /// <summary>
    /// Timeline 전체 종료 시 호출 — Root 위치 복구 + 잔여 정리
    /// </summary>
    void OnTimelineStopped(PlayableDirector pd)
    {
        ResetRootPosition();
        ResetImages();
        ClearBackground();

        if (effectOverlay != null)
            effectOverlay.gameObject.SetActive(false);
    }

    public void ServeAnimEnd()
    {
        Logger.Log("ServeEnd");
        director.Pause();

        OnServeComplete?.Raise(new Void());
    }

    public void OnContinueCutScene()
    {
        director.Stop();
    }



    public void PlayTimelineCutScene(TimelineAsset timeline)
    {
        if (director == null) return;
        cutSceneRoot.anchoredPosition = Vector2.zero;
        director.playableAsset = timeline;

        foreach (var track in timeline.GetOutputTracks())
        {
            director.SetGenericBinding(track, this);
        }

        director.time = 0;
        director.Play();
    }
    public void StopTimeline()
    {
        if (director != null)
            director.Stop();
    }


    public Image GetPooledImage()
    {
        EnsurePoolInitialized();

        if (imagePool.Count == 0)
        {
            Debug.LogWarning("[CutSceneTimelineManager] 이미지 풀 비어있음 — 모든 이미지가 사용 중");
            return null;
        }

        Image img = imagePool.Dequeue();

        RectTransform rect = img.GetComponent<RectTransform>();
        img.DOKill();
        rect.DOKill();

        img.color = new Color(1, 1, 1, 1);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        return img;
    }
    public void RegisterActive(string imageId, Image img)
    {
        EnsurePoolInitialized();
        activeImages[imageId] = img;
    }
    public Image GetActiveImage(string imageId)
    {
        EnsurePoolInitialized();
        if (string.IsNullOrEmpty(imageId)) return null;
        activeImages.TryGetValue(imageId, out Image img);
        return img;
    }
    public void ReturnToPool(string imageId, Image img)
    {
        EnsurePoolInitialized();

        img.gameObject.SetActive(false);
        img.color = Color.white;

        img.GetComponent<RectTransform>().localScale = Vector3.one;
        img.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        img.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        img.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        img.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

        activeImages.Remove(imageId);
        imagePool.Enqueue(img);
    }
    public void ResetImages()
    {
        EnsurePoolInitialized();

        foreach (var kvp in activeImages)
        {
            kvp.Value.DOKill();
            kvp.Value.GetComponent<RectTransform>().DOKill();

            kvp.Value.gameObject.SetActive(false);
            imagePool.Enqueue(kvp.Value);
        }

        activeImages.Clear();

        if (effectOverlay != null)
            effectOverlay.gameObject.SetActive(false);
    }



    public void SetImagePosition(Image img, AnchorType anchorType, float offsetX, float offsetY)
    {
        RectTransform rect = img.GetComponent<RectTransform>();
        Vector2 anchor = anchorPreset[anchorType];

        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot     = anchor;

        float w = canvasRect.rect.width  - padding_X;
        float h = canvasRect.rect.height - padding_Y;
        rect.anchoredPosition = new Vector2(w * offsetX, h * offsetY);
    }
    public void ResetRootPosition()
    {
        if (cutSceneRoot != null)
        {
            cutSceneRoot.anchoredPosition = Vector2.zero;
        }
    }
    public Image BgImage => bgImage;
    public void SetBackground(string path)
    {
        if (bgImage == null) return;
        Sprite bg = Resources.Load<Sprite>($"Cutscenes/{path}");
        if (bg != null)
        {
            bgImage.sprite = bg;
            bgImage.gameObject.SetActive(true);
        }
    }
    public void ClearBackground()
    {
        if (bgImage != null)
        {
            bgImage.sprite = null;
            bgImage.gameObject.SetActive(false);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Dialogue Bubbles
    // ══════════════════════════════════════════════════════════════════

    /// <summary> 인덱스로 말풍선 가져오기. Mixer에서 사용. </summary>
    public DynamicSpeechBubble GetDialogueBubble(int index)
    {
        if (dialogueBubbles == null || index < 0 || index >= dialogueBubbles.Count)
            return null;
        return dialogueBubbles[index];
    }

    /// <summary> 모든 말풍선 숨기기. Timeline 종료 시 호출. </summary>
    public void HideAllDialogueBubbles()
    {
        if (dialogueBubbles == null) return;

        foreach (var bubble in dialogueBubbles)
        {
            if (bubble == null) continue;

            bubble.GetComponent<RectTransform>().DOKill();
            bubble.gameObject.SetActive(false);
            bubble.textLabel.text = "";
            bubble.textLabel.maxVisibleCharacters = 99999;
            bubble.GetComponent<RectTransform>().localScale = Vector3.one * 0.5f;
        }
    }


    // ══════════════════════════════════════════════════════════════════
    //  에디터 전용 — 풀 없이 직접 접근 (플레이 모드 불필요)
    // ══════════════════════════════════════════════════════════════════

#if UNITY_EDITOR

    public Image GetImageByIndex(int index)
    {
        if (images == null || index < 0 || index >= images.Count) return null;
        return images[index];
    }
    public int ImageCount => images?.Count ?? 0;

    public void ResetImageEditor(int index)
    {
        if (images == null || index < 0 || index >= images.Count) return;

        var img = images[index];
        img.sprite = null;
        img.color = Color.white;
        img.gameObject.SetActive(false);
        img.GetComponent<RectTransform>().localScale = Vector3.one;
        img.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        img.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        img.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        img.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
    }

    public void ResetAllEditor()
    {
        for (int i = 0; i < ImageCount; i++)
            ResetImageEditor(i);

        ClearBackground();

        if (effectOverlay != null)
            effectOverlay.gameObject.SetActive(false);
    }



#endif

}
