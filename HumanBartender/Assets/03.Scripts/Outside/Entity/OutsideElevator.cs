using DG.Tweening;
using System.Collections;
using UnityEngine;



public class OutsideElevator : InteractiveEntity
{
    [SerializeField] Vector2Event externalDeltaEvent;
    [SerializeField] IntEvent changeCameraModeEvent;
    
    [SerializeField] Transform topPoint;
    [SerializeField] Transform bottomPoint;

    [SerializeField] GameObject wallColider;

    [SerializeField] float speed = 1f;
    [Tooltip("Camera Controller Duration이랑 맞추기")]
    [SerializeField] float delayDuration = 3f;
    [SerializeField] Ease ease;

    Transform targetPoint;

    bool isMoving = false;
    bool isTop = false;

    public override void Interact(IInteractor player)
    {
        if (isMoving) return;

        isMoving = true;
        isInteracting = true;

        player.State = EInteractorState.ForceMove;

        Vector3 oldPos = this.transform.position;
        oldPos.y = player.Transform.position.y;
        player.Transform.position = oldPos;
        player.Transform.SetParent(transform, worldPositionStays: true);

        wallColider.gameObject.SetActive(true);
        targetPoint = isTop ? bottomPoint : topPoint;

        changeCameraModeEvent?.Raise(1);

        OnInteracted?.Raise(this);

        transform.DOMove(targetPoint.position, speed)
              .SetSpeedBased(true)
              .SetEase(ease)
              .OnComplete(() =>
              {
                  transform.position = targetPoint.position;

                  player.Transform.SetParent(null, worldPositionStays: true);
                  changeCameraModeEvent?.Raise(0);

                  StartCoroutine(DelayToChangeState(player));
              });
    }

    public IEnumerator DelayToChangeState(IInteractor player)
    {
        yield return new WaitForSeconds(delayDuration);
        
        isMoving = false;
        isInteracting = false;
        isTop = !isTop;
        wallColider.gameObject.SetActive(false);

        OnFocusEnter();
        player.State = EInteractorState.None;
    }
}