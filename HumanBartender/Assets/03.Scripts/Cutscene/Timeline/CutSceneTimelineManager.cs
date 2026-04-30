using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

/// <summary>
/// Timeline 커스텀 트랙들의 바인딩 대상.
/// Image 풀, Canvas, EffectOverlay 등 공용 리소스를 관리한다.
/// 
/// 사용법:
/// 1. 이 컴포넌트를 Canvas 하위 오브젝트에 부착
/// 2. PlayableDirector의 각 트랙 바인딩에 이 오브젝트를 할당
/// 3. Timeline 에디터에서 클립 배치 후 재생
/// </summary>
public class CutSceneTimelineManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Canvas")]
    [SerializeField] Canvas        cutSceneCanvas;
    [SerializeField] RectTransform canvasRect;

    [Header("CutScene Root")]
    [SerializeField] RectTransform cutSceneRoot;

    [Header("Overlay / Images")]
    [SerializeField] Image         effectOverlay;
    [SerializeField] Image         bgImage;
    [SerializeField] List<Image>   images = new();

    [Header("Padding")]
    [SerializeField] float padding_X = 0;
    [SerializeField] float padding_Y = 0;

    [Header("Timeline")]
    [SerializeField] PlayableDirector director;

    public RectTransform CanvasRect    => canvasRect;
    public RectTransform CutSceneRoot  => cutSceneRoot;
    public Image         EffectOverlay => effectOverlay;

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


    void Awake()
    {
        imagePool = new Queue<Image>(images);
        ResetImages();

        if (director != null)
            director.stopped += OnTimelineStopped;
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

    public void PlayTimeline(TimelineAsset timeline)
    {
        if (director == null) return;
        director.playableAsset = timeline;
        director.Play();
    }

    public void StopTimeline()
    {
        if (director != null)
            director.Stop();
    }

    public Image GetPooledImage()
    {
        if (imagePool.Count == 0)
        {
            Debug.LogWarning("[CutSceneTimelineManager] 이미지 풀 비어있음");
            return null;
        }
        return imagePool.Dequeue();
    }

    public void RegisterActive(string imageId, Image img)
    {
        activeImages[imageId] = img;
    }

    /// <summary>
    /// imagePath로 현재 활성화된 Image를 찾는다.
    /// SpriteAnim, Move 등 보조 트랙에서 대상을 참조할 때 사용.
    /// </summary>
    public Image GetActiveImage(string imageId)
    {
        if (string.IsNullOrEmpty(imageId)) return null;
        activeImages.TryGetValue(imageId, out Image img);
        return img;
    }

    public void ReturnToPool(string imageId, Image img)
    {
        img.gameObject.SetActive(false);
        img.color = Color.white;
        img.GetComponent<RectTransform>().localScale = Vector3.one;
        activeImages.Remove(imageId);
        imagePool.Enqueue(img);
    }

    public void ResetImages()
    {
        foreach (var kvp in activeImages)
        {
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
}
