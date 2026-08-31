using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 2부 주문자 앞에 잠깐 생기는 서빙 자리.
///
/// 1부의 CoasterDropZone과 하는 일은 같지만 붙는 대상이 다르다. 그쪽은 씬에 미리 놓인 손님 슬롯에
/// 매여 있고, 이쪽은 대본이 주문을 만든 순간 그 좌석 앞에 생겼다가 잔이 나가면 사라진다 —
/// 2부의 좌석은 씬에 고정된 손님 자리가 아니라 대본이 그때그때 채우는 자리이기 때문이다.
///
/// 받아들일 잔인지는 판단하지 않는다. 여기 놓였다는 것 자체가 주문자에게 냈다는 뜻이다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StoryServeDropTarget : MonoBehaviour, IDropHandler
{
    Action<CraftedDrink> onServed;

    /// <summary>잔이 놓였을 때 부를 곳을 건다. 한 번 놓이면 스스로 연결을 끊는다.</summary>
    public void Bind(Action<CraftedDrink> handler)
    {
        onServed = handler;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (onServed == null) return;

        GameObject dragged = eventData.pointerDrag;
        if (dragged == null || !dragged.TryGetComponent(out DrinkDragItem item)) return;

        // 먼저 표시하고 알린다. 잔을 실제로 치우는 것은 곧 이어지는 OnEndDrag가 하는데,
        // 그 전에 오브젝트가 사라지면 남은 드래그 이벤트가 없는 대상에게 간다.
        item.MarkServed();

        Action<CraftedDrink> handler = onServed;
        onServed = null;
        handler(item.Drink);
    }
}
