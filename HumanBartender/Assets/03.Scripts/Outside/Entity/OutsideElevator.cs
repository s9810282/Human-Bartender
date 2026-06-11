using DG.Tweening;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;



public class OutsideElevator : InteractiveEntity
{
    [Header("Logo")]
    [SerializeField] LogoFade logoEvent;
    [SerializeField] float logoFadeTiming = 0.2f;

    [Header("Radio")]
    [SerializeField] OutsideElevatorRadio radio;

    [Header("Event")]
    [SerializeField] Vector2Event externalDeltaEvent;
    [SerializeField] IntEvent changeCameraModeEvent;
    [SerializeField] float cameraReturnTiming = 0.8f;

    [Header("Stat")]
    [SerializeField] Transform topPoint;
    [SerializeField] Transform bottomPoint;

    [SerializeField] GameObject wallColider;

    [SerializeField] float duration = 1f;

    [Tooltip("Camera Controller Duration이랑 맞추기")]
    [SerializeField] float delayDuration = 3f;
    [SerializeField] Ease ease;


    Transform targetPoint;

    bool isMoving = false;
    bool isTop = false;

    public void SetPosition(bool isTop)
    {
        this.isTop = isTop;
        transform.position = isTop ? topPoint.transform.position : bottomPoint.transform.position;
    }

    public override void Interact(IInteractor player)
    {
        if (isMoving) return;

        // 1. 상태 및 플래그 설정
        isMoving = true;
        isInteracting = true;

        player.State = EInteractorState.ForceMove;


        Vector3 startPos = this.transform.position;
        startPos.y = player.Transform.position.y;

        player.Transform.position = startPos;
        player.Transform.SetParent(this.transform, worldPositionStays: true);


        wallColider.gameObject.SetActive(true);
        targetPoint = isTop ? bottomPoint : topPoint;


        changeCameraModeEvent?.Raise(1);
        OnInteracted?.Raise(this);
        radio.Interact(null);

        Sequence moveSeq = DOTween.Sequence();

        moveSeq.Append(transform.DOMove(targetPoint.position, duration).SetEase(ease));

        if (!GameStateManager.Instance.IsOutsideLogo)
        {
            GameStateManager.Instance.IsOutsideLogo = true;
            float logoEventTime = duration * logoFadeTiming;
            moveSeq.InsertCallback(logoEventTime, () =>
            {
                logoEvent.ShowLogo();
            });
        }

        float cameraEventTiming = duration * cameraReturnTiming;
        moveSeq.InsertCallback(cameraEventTiming, () =>
        {
            changeCameraModeEvent?.Raise(0);
            //changeCameraModeEvent?.Raise(2);
        });

        moveSeq.OnComplete(() =>
        {
            // 최종 위치 보정
            transform.position = targetPoint.position;

            // 플레이어 종속 해제 및 상태 원복
            player.Transform.SetParent(null, worldPositionStays: true);
            wallColider.gameObject.SetActive(false);
            //changeCameraModeEvent?.Raise(0);

            radio.EndInteract();

            StartCoroutine(DelayToChangeState(player));
        });
    }
    public IEnumerator DelayToChangeState(IInteractor player)
    {
        yield return new WaitForSeconds(delayDuration);
        
        isMoving = false;
        isInteracting = false;
        isTop = !isTop;

        OnFocusEnter();
        player.State = EInteractorState.None;
    }
}