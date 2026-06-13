using UnityEngine;


public enum ETrackerType
{
    InteractButton,
    TextBubble,
}

public interface UITracker
{
    void SetTrackedTarget(IInteractable target);
}


public class UIOutsideTracker : MonoBehaviour, UITracker
{
    [SerializeField] protected InteractableEvent OnTrackedText;

    [SerializeField] protected ETrackerType Type;
    [SerializeField] protected RectTransform target;
    [SerializeField] protected Camera cam;

    protected IInteractable trackedTarget;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public virtual void OnEnable()
    {
        if(OnTrackedText != null)
            OnTrackedText.OnRaised += SetTrackedTarget;
    }

    public virtual void OnDisable()
    {
        if (OnTrackedText != null)
            OnTrackedText.OnRaised -= SetTrackedTarget;
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
    public void SetTrackedTarget(IInteractable target)
    {
        trackedTarget = target;
    }

    public void UpdatePosition()
    {
        Vector2 offset =
            Type == ETrackerType.InteractButton ? trackedTarget.ButtonOffset : trackedTarget.TextOffset;

        Vector3 screenPos = cam.WorldToScreenPoint(trackedTarget.Position);
        
        screenPos.x += offset.x;
        screenPos.y += offset.y;
        screenPos.z = 10f;

        target.position = screenPos;
    }
}
