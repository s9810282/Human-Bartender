using UnityEngine;

public class Player : MonoBehaviour, IInteractor
{
    [Header("Composition")]
    [SerializeField] PlayerMovement2D movement2D;
    [SerializeField] PlayerAnimator2D animator2D;

    [SerializeField] InteractionDetector detector;
    [SerializeField] EInteractorState state = EInteractorState.None;

    [Header("Stupid Issue")]
    [SerializeField] protected ITrackedbleEvent OnTrackedText;
    [SerializeField] private bool isAvaliable;
    [SerializeField] protected Vector2 buttonOffset;


    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    public EInteractorState State { get => state; set => state = value; }


    Vector2 ITrackedble.ButtonOffset => buttonOffset;
    Vector2 ITrackedble.TextOffset => buttonOffset;
    bool ITrackedble.IsAvaliable => isAvaliable;

    


    public void Update()
    {
        if (state == EInteractorState.Lock) return;
        if (state == EInteractorState.Interct) return;

        movement2D.Handle();
        animator2D.Handle(movement2D.VelocityX);

        if (state == EInteractorState.ForceMove) return;
        detector.Handle();
    }

    public void Lock()
    {
        animator2D.Handle(0);
        state = EInteractorState.Lock;
    }
    public void UnLock() => state = EInteractorState.None;

    public void Interact(IInteractor player)
    {
        OnTrackedText?.Raise(this);
    }

    public void InteractorEvent()
    {
        Interact(this);
    }
}