using System;
using System.Threading;
using Cysharp.Threading.Tasks;
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

    [Header("랜덤 손님 파츠 렌더러 (guest_bodies.json: bodies/outfits/eyes/hairs)")]
    [SerializeField] SpriteRenderer bodyRenderer;
    [SerializeField] SpriteRenderer outfitRenderer;
    [SerializeField] SpriteRenderer eyesRenderer;
    [SerializeField] SpriteRenderer hairRenderer;

    [Header("테스트용 임시 손님 오브젝트 (파츠 addressable 로딩 대신 단순 On/Off)")]
    [SerializeField] GameObject tempAppearanceObject;

    [Header("말풍선 (Outside DynamicSpeechBubble 재사용, 위치는 슬롯 고정이라 트래커 없이 사용)")]
    [SerializeField] GameObject bubbleRoot;
    [SerializeField] DynamicSpeechBubble speechBubble;

    bool useTempAppearance;
    CancellationTokenSource bubbleCts;

    public ESlotType SlotType => slotType;
    public EGuestState CurrentState { get; private set; } = EGuestState.Empty;
    public Guest CurrentGuest { get; private set; }

    public bool IsEmpty => CurrentState == EGuestState.Empty;

    /// <summary>GuestManager가 테스트 모드 여부를 전달한다. true면 파츠 addressable 로딩 대신 tempAppearanceObject를 On/Off한다.</summary>
    public void SetTempAppearanceMode(bool enabled)
    {
        useTempAppearance = enabled;
    }

    /// <summary>손님을 이 자리에 배정하고 입장 상태로 전환한다. 파츠 스프라이트는 로딩이 끝나는 대로 비동기로 적용된다.</summary>
    public void Seat(Guest guest)
    {
        CurrentGuest = guest;
        CurrentState = EGuestState.Coming;

        ApplyAppearanceAsync(guest).Forget();
    }

    /// <summary>
    /// guest.bodySprites 로딩(GuestManager에서 미리 시작됨)이 끝날 때까지 기다렸다가 파츠 스프라이트를 렌더러에 적용한다.
    /// 단골 손님(appearance == null)은 대상이 아니므로 무시한다.
    /// </summary>
    async UniTaskVoid ApplyAppearanceAsync(Guest guest)
    {
        if (guest.appearance == null) return;

        if (useTempAppearance)
        {
            if (tempAppearanceObject != null) tempAppearanceObject.SetActive(true);
            return;
        }

        await UniTask.WaitUntil(() => guest.bodySprites != null, cancellationToken: this.GetCancellationTokenOnDestroy());

        if (CurrentGuest != guest) return; // 기다리는 동안 자리가 비워지거나 다른 손님으로 교체됨

        bodyRenderer.sprite = guest.bodySprites.Body;
        outfitRenderer.sprite = guest.bodySprites.Outfit;
        eyesRenderer.sprite = guest.bodySprites.Eyes;
        hairRenderer.sprite = guest.bodySprites.Hair;
    }

    /// <summary>
    /// 말풍선에 텍스트를 즉시 표시한다. durationSec이 0보다 크면 그 시간 뒤 자동으로 숨긴다.
    /// 슬롯이 고정 위치라 Outside처럼 매 프레임 화면 좌표를 추적할 필요는 없다.
    /// </summary>
    public void ShowBark(string text, float durationSec = 3f)
    {
        bubbleCts?.Cancel();
        bubbleCts?.Dispose();
        bubbleCts = new CancellationTokenSource();

        if (bubbleRoot != null) bubbleRoot.SetActive(true);
        speechBubble.SetText(text);

        if (durationSec > 0f)
            HideBarkAfterAsync(durationSec, bubbleCts.Token).Forget();
    }

    async UniTaskVoid HideBarkAfterAsync(float durationSec, CancellationToken token)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(durationSec), cancellationToken: token);
        HideBark();
    }

    /// <summary>말풍선을 즉시 숨긴다.</summary>
    public void HideBark()
    {
        bubbleCts?.Cancel();
        if (bubbleRoot != null) bubbleRoot.SetActive(false);
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

    /// <summary>손님이 자리를 완전히 비웠을 때 호출한다. 로드된 파츠 스프라이트의 addressable 핸들을 반납하고 렌더러를 비운다.</summary>
    public void Clear()
    {
        CurrentGuest?.bodySprites?.Release();

        HideBark();

        if (tempAppearanceObject != null) tempAppearanceObject.SetActive(false);

        bodyRenderer.sprite = null;
        outfitRenderer.sprite = null;
        eyesRenderer.sprite = null;
        hairRenderer.sprite = null;

        CurrentGuest = null;
        CurrentState = EGuestState.Empty;
    }
}
