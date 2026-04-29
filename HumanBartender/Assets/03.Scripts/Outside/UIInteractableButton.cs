using Unity.VisualScripting;
using UnityEngine;

public class UIInteractableButton : UIOutside
{
    [Header("Event Channels")]
    [SerializeField] private InteractableEventChannel visibleInteractble;

    private void OnEnable()
    {
        if (visibleInteractble != null)
            visibleInteractble.OnRaised += OnVisibleInteractButton;
    }

    public void OnDisable()
    {
        if (visibleInteractble != null)
            visibleInteractble.OnRaised -= OnVisibleInteractButton;
    }

    public override void LateUpdate()
    {
        base.LateUpdate();
    }

    public void OnVisibleInteractButton(IInteractable interactable)
    {
        trackedTarget = interactable;

        if (interactable == null)
        {
            target.gameObject.SetActive(false);
            return;
        }

        target.gameObject.SetActive(true);
    }

    public void OnUnVisibleInteractButton(IInteractable interactable)
    {
        trackedTarget = null;
        target.gameObject.SetActive(false);
    }
}
