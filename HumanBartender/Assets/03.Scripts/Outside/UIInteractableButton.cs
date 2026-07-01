using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UIInteractableButton : UIOutsideTracker
{
    [SerializeField] InteractableEvent OnInteractbleEvent;
    [SerializeField] TMP_Text interactText;

    public override void OnEnable()
    {
        base.OnEnable();

        if (OnInteractbleEvent != null)
            OnInteractbleEvent.OnRaised += OnVisibleInteractButton;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (OnInteractbleEvent != null)
            OnInteractbleEvent.OnRaised -= OnVisibleInteractButton;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    public void OnVisibleInteractButton(IInteractable interactable)
    {
        if (interactable == null)
        {
            target.gameObject.SetActive(false);
            trackedTarget = null;
            return;
        }

        SetTrackedTarget(interactable);
        interactText.text = interactable.Label;
        target.gameObject.SetActive(true);
    }

    public void OnUnVisibleInteractButton(IInteractable interactable)
    {
        trackedTarget = null;
        target.gameObject.SetActive(false);
    }
}
