using UnityEngine;

public class OutsideElevator : InteractiveEntity
{
    [SerializeField] Vector2Event externalDeltaEvent;
    
    [SerializeField] Transform topPoint;
    [SerializeField] Transform bottomPoint;

    [SerializeField] GameObject wallColider;

    [SerializeField] float speed;

    Transform targetPoint;

    bool isHasPassenger = false;
    bool isMoving = false;
    bool isTop = false;

    public override void Interact(IInteractor player)
    {
        isMoving = true;
        isHasPassenger = true;

        Vector3 vec = this.transform.position;
        vec.y = player.Transform.position.y;

        player.Transform.position = vec;

        wallColider.gameObject.SetActive(true);

        targetPoint = isTop ? bottomPoint : topPoint;
    }

    private void Update()
    {
        if (!isMoving) return;

        Vector3 oldPos = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, speed * Time.deltaTime);

        Vector2 delta = transform.position - oldPos;

        if (isHasPassenger && delta != Vector2.zero)
        {
            externalDeltaEvent.Raise(delta);
        }

        if (Mathf.Approximately(transform.position.y, targetPoint.position.y))
        {
            transform.position = targetPoint.position;
            isMoving = false;
            isTop = !isTop;
            wallColider.gameObject.SetActive(false);
        }
    }
}