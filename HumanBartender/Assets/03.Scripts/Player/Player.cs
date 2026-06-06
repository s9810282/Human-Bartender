using UnityEngine;

public class Player : MonoBehaviour, IInteractor
{
    [Header("Composition")]
    [SerializeField] PlayerMovement2D movement2D;
    [SerializeField] PlayerAnimator2D animator2D;

    [SerializeField] InteractionDetector detector;
    


    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    public EInteractorState State { get => state; set => state = value; }


    EInteractorState state = EInteractorState.None;


    public void Update()
    {
        if (state == EInteractorState.Interct) return;

        movement2D.Handle();
        animator2D.Handle(movement2D.VelocityX);

        if (state == EInteractorState.ForceMove) return;
        detector.Handle();
    }
}