using UnityEngine;

public class Player : MonoBehaviour, IInteractor, IInteractable
{
    [Header("Composition")]
    [SerializeField] PlayerMovement2D movement2D;
    [SerializeField] PlayerAnimator2D animator2D;

    [SerializeField] InteractionDetector detector;
    [SerializeField] EInteractorState state = EInteractorState.None;

    [Header("Stupid Issue")]
    [SerializeField] protected InteractableEvent OnTrackedText;
    [SerializeField] protected int priority;
    [SerializeField] private bool isAvaliable;
    [SerializeField] protected bool isInteracting;
    [SerializeField] private string label;
    [SerializeField] protected Vector2 buttonOffset;


    public GameObject GameObject => gameObject;
    public Transform Transform => transform;
    public EInteractorState State { get => state; set => state = value; }
    public bool IsInteracting { get => isInteracting; set => isInteracting = value; }
    public bool IsAvaliable { get => isAvaliable; set => isAvaliable = value; }
    public string EntityLabel { get => label; set => label = value; }


    int IInteractable.Priority => priority;
    string IInteractable.Label => label;
    Vector2 IInteractable.Offset => buttonOffset;
    bool IInteractable.IsAvaliable => isAvaliable;

    public Vector3 Position => transform.position;




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

    public void OnFocusEnter()
    {
        
    }
    public void OnFocusExit()
    {
        
    }

    public void InteractorEvent()
    {
        Interact(this);
    }
}