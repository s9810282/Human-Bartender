using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// GuestSlot 앞에 위치한 코스터 드롭 존. 코스터 아이콘(CoasterDragItem)이 놓이면 GuestManager에
/// 배치를 요청해, 손님이 코스터를 기다리는 상태였다면 주문 대기 상태로 전환시키고 인내심 타이머를 멈춘다.
/// 놓인 코스터는 이 위치(손님 앞)에 그대로 고정되며, 손님이 자리를 뜨면 치운다.
/// </summary>
public class CoasterDropZone : MonoBehaviour, IDropHandler
{
    [SerializeField] GuestSlot targetSlot;
    [SerializeField] GuestManager guestManager;

    [Tooltip("코스터가 놓였을 때 이 드롭존 중앙 기준으로 얼마나 떨어진 위치에 고정될지(로컬 UI 좌표). 손님 자리에 맞게 조정.")]
    [SerializeField] Vector2 placedOffset;

    CoasterDragItem placedCoaster;

    void OnEnable()
    {
        if (guestManager != null)
            guestManager.GuestReleased += OnGuestReleased;
    }

    void OnDisable()
    {
        if (guestManager != null)
            guestManager.GuestReleased -= OnGuestReleased;
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragItem = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<CoasterDragItem>();
        if (dragItem == null) return;

        if (!guestManager.TryPlaceCoaster(targetSlot)) return;

        dragItem.PlaceAt((RectTransform)transform, placedOffset);
        placedCoaster = dragItem;
    }

    /// <summary>손님이 응대/이탈로 자리를 떠 슬롯이 비면, 놓여있던 코스터를 치운다.</summary>
    void OnGuestReleased(Guest guest)
    {
        if (placedCoaster == null || !targetSlot.IsEmpty) return;

        placedCoaster.ResetAndHide();
        placedCoaster = null;
    }
}
