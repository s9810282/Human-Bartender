using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractHandler : MonoBehaviour
{
    [SerializeField] private InteractorEventChannel interactPressedChannel;
    [SerializeField] private PlayerInteractor interactor;


    public void OnInteract(InputValue value)
    {        
        interactPressedChannel.Raise(interactor);
    }
}
