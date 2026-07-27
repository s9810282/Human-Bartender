using UnityEngine;

/// <summary>손님 한 명이 자리에서 거치는 상태.</summary>
public enum EGuestState
{
    Empty,
    Coming,
    Sit,
    WaitinOrder,

    Leaving,
}

/// <summary>
/// 타이쿤(1부)에서 손님 한 명이 앉는 물리적 자리 하나.
/// slotType(Left/Right/Middle)은 대사(2부)의 DialogueCharacterManager/PlayCamera가 쓰는
/// ESlotType과 동일한 위치 개념을 공유한다.
/// </summary>
public class GuestSlot : MonoBehaviour
{
    [SerializeField] ESlotType slotType;

    public ESlotType SlotType => slotType;
    public EGuestState CurrentState { get; private set; } = EGuestState.Empty;
    public Guest CurrentGuest { get; private set; }

    public bool IsEmpty => CurrentState == EGuestState.Empty;

    /// <summary>손님을 이 자리에 배정하고 입장 상태로 전환한다.</summary>
    public void Seat(Guest guest)
    {
        CurrentGuest = guest;
        CurrentState = EGuestState.Coming;
    }

    /// <summary>손님이 자리에 앉는 연출이 끝났을 때 호출한다.</summary>
    public void MarkSit()
    {
        CurrentState = EGuestState.Sit;
    }

    /// <summary>주문 대기 상태로 전환한다.</summary>
    public void MarkWaitingOrder()
    {
        CurrentState = EGuestState.WaitinOrder;
    }

    /// <summary>응대가 끝나 손님이 떠나는 상태로 전환한다.</summary>
    public void MarkLeaving()
    {
        CurrentState = EGuestState.Leaving;
    }

    /// <summary>손님이 자리를 완전히 비웠을 때 호출한다.</summary>
    public void Clear()
    {
        CurrentGuest = null;
        CurrentState = EGuestState.Empty;
    }
}
