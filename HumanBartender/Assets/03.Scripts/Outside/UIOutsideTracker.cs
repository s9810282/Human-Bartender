using UnityEngine;

public interface UITracker
{
    void SetTrackedTarget(IInteractable target);
}


public class UIOutsideTracker : MonoBehaviour, UITracker
{
    [SerializeField] protected InteractableEventChannel OnTrackedText;

    [SerializeField] protected RectTransform target;
    [SerializeField] protected Camera cam;
    [SerializeField] protected Vector2 offset;

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

        UpdateButtonPosition();
    }
    public void SetTrackedTarget(IInteractable target)
    {
        trackedTarget = target;
    }

    public void UpdateButtonPosition()
    {
        Vector2 screenPoint = cam.WorldToScreenPoint(trackedTarget.Position);
        screenPoint += offset;

        target.position = screenPoint;
    }
}
