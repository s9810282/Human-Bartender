using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UIInteractableButton : UIOutsideTracker
{
    [SerializeField] TMP_Text interactText;

    public override void OnEnable()
    {
        base.OnEnable();

        if (OnTrackedText != null)
            OnTrackedText.OnRaised += OnVisibleInteractButton;
    }

    public override void OnDisable()
    {
        base.OnDisable();

        if (OnTrackedText != null)
            OnTrackedText.OnRaised -= OnVisibleInteractButton;
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
            return;
        }

        interactText.text = interactable.Label;
        target.gameObject.SetActive(true);
    }

    public void OnUnVisibleInteractButton(IInteractable interactable)
    {
        trackedTarget = null;
        target.gameObject.SetActive(false);
    }
}
