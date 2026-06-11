using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HandleDragHandler : MonoBehaviour,
    IPointerDownHandler, IInitializePotentialDragHandler, IDragHandler
{
    Scrollbar sb;
    RectTransform slidingArea;
    RectTransform handleRect;
    float pointerOffset; // 클릭 지점과 핸들 중심의 오프셋 (sliding area 로컬)

    public void Setup(Scrollbar scrollbar, RectTransform area)
    {
        sb = scrollbar;
        slidingArea = area;
        handleRect = scrollbar.handleRect;
    }

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        e.useDragThreshold = false; // 임계값 없이 즉시 드래그 시작
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (slidingArea == null) return;

        Vector2 localPointer;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                slidingArea, e.position, e.pressEventCamera, out localPointer))
            return;

        // 핸들 중심의 sliding area 로컬 좌표
        Vector2 handleCenter = (Vector2)handleRect.localPosition;

        bool vertical = sb.direction == Scrollbar.Direction.BottomToTop ||
                        sb.direction == Scrollbar.Direction.TopToBottom;

        // 잡은 위치와 핸들 중심의 차이를 기억 → 점프 방지
        pointerOffset = vertical
            ? (localPointer.y - handleCenter.y)
            : (localPointer.x - handleCenter.x);
    }

    public void OnDrag(PointerEventData e)
    {
        if (slidingArea == null) return;

        Vector2 localPointer;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                slidingArea, e.position, e.pressEventCamera, out localPointer))
            return;

        bool vertical = sb.direction == Scrollbar.Direction.BottomToTop ||
                        sb.direction == Scrollbar.Direction.TopToBottom;

        Rect r = slidingArea.rect;
        float t;

        if (vertical)
        {
            float adjusted = localPointer.y - pointerOffset;
            t = Mathf.InverseLerp(r.yMin, r.yMax, adjusted);
        }
        else
        {
            float adjusted = localPointer.x - pointerOffset;
            t = Mathf.InverseLerp(r.xMin, r.xMax, adjusted);
        }

        // direction 보정 (위→아래, 오른→왼 방향이면 뒤집기)
        if (sb.direction == Scrollbar.Direction.TopToBottom ||
            sb.direction == Scrollbar.Direction.RightToLeft)
            t = 1f - t;

        sb.value = Mathf.Clamp01(t);
    }
}