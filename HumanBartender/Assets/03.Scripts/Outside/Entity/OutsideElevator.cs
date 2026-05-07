using DG.Tweening;
using UnityEngine;



public class OutsideElevator : InteractiveEntity
{
    [SerializeField] Vector2Event externalDeltaEvent;
    [SerializeField] IntEvent changeCameraModeEvent;
    
    [SerializeField] Transform topPoint;
    [SerializeField] Transform bottomPoint;

    [SerializeField] GameObject wallColider;

    [SerializeField] float speed;
    [SerializeField] Ease ease;

    Transform targetPoint;

    bool isMoving = false;
    bool isTop = false;

    public override void Interact(IInteractor player)
    {
        if (isMoving) return;

        isMoving = true;
        isInteracting = true;

        player.State = EInteractorState.Interct;

        Vector3 oldPos = this.transform.position;
        oldPos.y = player.Transform.position.y;
        player.Transform.position = oldPos;
        player.Transform.SetParent(transform, worldPositionStays: true);

        wallColider.gameObject.SetActive(true);
        targetPoint = isTop ? bottomPoint : topPoint;

        OnFocusExit();
        changeCameraModeEvent?.Raise(1);

        transform.DOMove(targetPoint.position, speed)
              .SetSpeedBased(true)
              .SetEase(ease)
              .OnComplete(() =>
              {
                  transform.position = targetPoint.position;

                  player.Transform.SetParent(null, worldPositionStays: true);

                  isMoving = false;
                  isInteracting = false;
                  isTop = !isTop;
                  OnFocusEnter();
                  wallColider.gameObject.SetActive(false);

                  changeCameraModeEvent?.Raise(0);

                  player.State = EInteractorState.None;
              });
    }
}