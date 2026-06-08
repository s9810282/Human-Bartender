using UnityEngine;
using UnityEngine.SocialPlatforms;

public interface UITracker
{
    void SetTrackedTarget(IInteractable target);
}


public class UIOutsideTracker : MonoBehaviour, UITracker
{
    [SerializeField] protected InteractableEvent OnTrackedText;

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
        Logger.Log($"{name} : SetTracked Target");
        trackedTarget = target;
    }

    public void UpdatePosition()
    {
        Vector3 screenPos = cam.WorldToScreenPoint(trackedTarget.Position);
        
        screenPos.x += trackedTarget.Offset.x;
        screenPos.y += trackedTarget.Offset.y;
        screenPos.z = 10f;

        target.position = screenPos;
    }
}
