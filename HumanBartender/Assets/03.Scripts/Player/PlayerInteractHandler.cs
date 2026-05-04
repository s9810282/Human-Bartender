using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractHandler : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Player interactor;

    [Header("Event")]
    [SerializeField] private InteractorEvent interactPressedEvent;
    [SerializeField] private Vector2Event onMoveEvent;
    


    public void OnInteract(InputValue value)
    {
        interactPressedEvent?.Raise(interactor);
    }
    public void OnMove(InputValue value)
    {
        onMoveEvent?.Raise(value.Get<Vector2>());
    }
}
