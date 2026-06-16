using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractHandler : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player interactor;

    [Header("Event")]
    [SerializeField] private InteractorEvent interactPressedEvent;
    [SerializeField] private VoidEvent runnerAdvanceInputEvent;
    [SerializeField] private Vector2Event onMoveEvent;
    [SerializeField] private BoolEvent setSpeedEvent;


    public void OnInteract(InputValue value)
    {
        runnerAdvanceInputEvent?.Raise(new Void());
        interactPressedEvent?.Raise(interactor);
    }
    public void OnMove(InputValue value)
    {
        onMoveEvent?.Raise(value.Get<Vector2>());
    }
    public void OnSprint(InputValue value)
    {
        if (value.isPressed)
        {
            setSpeedEvent?.Raise(true);
        }
        else
        {
            setSpeedEvent?.Raise(false);
        }
    }
}
