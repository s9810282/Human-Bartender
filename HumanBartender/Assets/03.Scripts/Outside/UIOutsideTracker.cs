using UnityEngine;


public enum ETrackerType
{
    InteractButton,
    TextBubble,
}

public interface UITracker
{
    void SetTrackedTarget(ITrackedble target);
}


public class UIOutsideTracker : MonoBehaviour, UITracker
{
    [SerializeField] protected ITrackedbleEvent OnTracked;

    [SerializeField] protected ETrackerType Type;
    [SerializeField] protected RectTransform target;
    [SerializeField] protected Camera cam;
    [SerializeField] protected float referenceOrthoSize = 1.35f;

    protected ITrackedble trackedTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void OnEnable()
    {
        if(OnTracked != null)
            OnTracked.OnRaised += SetTrackedTarget;
    }

    public virtual void OnDisable()
    {
        if (OnTracked != null)
            OnTracked.OnRaised -= SetTrackedTarget;
    }


    public virtual void LateUpdate()
    {
        if (trackedTarget == null) return;

        if (!trackedTarget.IsAvaliable)
        {
            target.gameObject.SetActive(false);
            trackedTarget = null;
            return;
        }

        UpdatePosition();
    }

    public void StopTracking()
    {
        trackedTarget = null;
        target.gameObject.SetActive(false);
    }
    public void SetTrackedTarget(ITrackedble target)
    {
        trackedTarget = target;
    }

    public void UpdatePosition()
    {
        Vector2 offset =
            Type == ETrackerType.InteractButton ? trackedTarget.ButtonOffset : trackedTarget.TextOffset;

        if (cam.orthographic)
            offset *= referenceOrthoSize / cam.orthographicSize;

        Vector3 screenPos = cam.WorldToScreenPoint(trackedTarget.Transform.position);
        
        screenPos.x += offset.x;
        screenPos.y += offset.y;
        screenPos.z = 10f;

        target.position = screenPos;
    }
}
