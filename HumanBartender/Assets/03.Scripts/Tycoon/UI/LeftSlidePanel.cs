using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 좌측 버튼을 눌러 열고 닫는 슬라이드 패널. panel의 anchoredPosition.x를 화면 밖(왼쪽, -width)과
/// 0 사이로 애니메이션한다. 제조 UI 등 실제 콘텐츠는 panel의 자식으로 추후 채워 넣으면 된다.
/// </summary>
public class LeftSlidePanel : MonoBehaviour
{
    [SerializeField] RectTransform panel;
    [SerializeField] Button toggleButton;
    [SerializeField] float duration = 0.3f;

    float closedX;
    float openX;
    bool isOpen;
    Coroutine anim;

    void Awake()
    {
        closedX = -panel.rect.width;
        openX = 0f;

        panel.anchoredPosition = new Vector2(closedX, panel.anchoredPosition.y);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);
    }

    /// <summary>열려있으면 닫고, 닫혀있으면 연다.</summary>
    public void Toggle()
    {
        SetOpen(!isOpen);
    }

    public void Open() => SetOpen(true);
    public void Close() => SetOpen(false);

    /// <summary>
    /// 패널이 열리고 닫힐 때 발생한다. 안쪽 화면의 뒤로 버튼을 거치지 않고 토글로 바로 닫는 길이 있어서,
    /// 패널이 닫혔다는 사실을 밖에서 알 방법이 이것뿐이다.
    /// </summary>
    public event System.Action<bool> OpenChanged;

    void SetOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;

        OpenChanged?.Invoke(open);

        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateX(open ? openX : closedX));
    }

    /// <summary>panel의 anchoredPosition.x를 duration초 동안 ease-in-out으로 target까지 보간한다.</summary>
    IEnumerator AnimateX(float target)
    {
        float start = panel.anchoredPosition.x;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            float x = Mathf.Lerp(start, target, k);

            panel.anchoredPosition = new Vector2(x, panel.anchoredPosition.y);
            yield return null;
        }

        panel.anchoredPosition = new Vector2(target, panel.anchoredPosition.y);
    }
}
