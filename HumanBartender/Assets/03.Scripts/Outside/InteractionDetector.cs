using System;
using System.Collections.Generic;
using UnityEngine;


public class InteractionDetector : MonoBehaviour
{
    [Header("Event Channels")]
    [SerializeField] private InteractorEvent interactPressedChannel;
    [SerializeField] private InteractableEvent OnInteractedTargetChannel;

    [Header("Event")]
    [SerializeField] private InteractableEvent OnTargetChanged;
    [SerializeField] private VoidEvent PlayerStopEvent;

    [Header("References")]
    [SerializeField] private Transform origin;

    [Header("Detection")]
    [Tooltip("감지 반경")]
    [SerializeField] private float detectionRadius = 1.5f;

    [Tooltip("감지 오프셋")]
    [SerializeField] private Vector2 detectionOffset = Vector2.zero;
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Tooltip("트리거 콜라이더도 감지 대상에 포함할지 여부")]
    [SerializeField] private bool detectTriggers = true;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    

    private readonly Collider2D[] hitBuffer = new Collider2D[16];
    private readonly HashSet<IInteractable> currentFrameCandidates = new();
    private IInteractable currentTarget;
    private ContactFilter2D contactFilter;

    public IInteractable CurrentTarget => currentTarget;
    public bool HasTarget => currentTarget != null;

    private void Awake()
    {
        if (origin == null) origin = transform;
        RebuildContactFilter();
    }

    private void OnValidate()
    {
        RebuildContactFilter();
    }

    private void RebuildContactFilter()
    {
        contactFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = interactableLayer,
            useTriggers = detectTriggers,
        };
        contactFilter.useDepth = false;
    }

    private void OnEnable()
    {
        if (interactPressedChannel != null)
            interactPressedChannel.OnRaised += HandleInteractInput;

        if (OnInteractedTargetChannel != null)
            OnInteractedTargetChannel.OnRaised += InteractedTarget;
    }

    private void OnDisable()
    {
        if (interactPressedChannel != null)
            interactPressedChannel.OnRaised -= HandleInteractInput;

        if (OnInteractedTargetChannel != null)
            OnInteractedTargetChannel.OnRaised -= InteractedTarget;

        if (currentTarget != null)
        {
            currentTarget.OnFocusExit();
            currentTarget = null;
            OnTargetChanged?.Raise(null);
        }
        currentFrameCandidates.Clear();
    }

    public void Handle()
    {
        ScanCandidates();
        UpdateCurrentTarget();
    }

    private void ScanCandidates()
    {
        currentFrameCandidates.Clear();

        Vector2 center = (Vector2)origin.position + detectionOffset;
        int hitCount = Physics2D.OverlapCircle(center, detectionRadius, contactFilter, hitBuffer);

        for (int i = 0; i < hitCount; i++)
        {
            var col = hitBuffer[i];
            if (col == null) continue;

            if (!col.TryGetComponent<IInteractable>(out var interactable))
                interactable = col.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;
            if (!interactable.IsAvaliable) continue;

            currentFrameCandidates.Add(interactable);
        }
    }
    private void UpdateCurrentTarget()
    {
        IInteractable best = null;
        float bestScore = float.PositiveInfinity;
        Vector2 originPos = origin.position;

        foreach (var c in currentFrameCandidates)
        {
            Vector2 toTarget = (Vector2)c.Transform.position - originPos;
            float distance = toTarget.magnitude;

            float score = distance;

            score -= c.Priority * 0.01f;

            if (score < bestScore)
            {
                bestScore = score;
                best = c;
            }
        }

        if (!ReferenceEquals(best, currentTarget))
        {
            if (best != null && !best.IsAvaliable) return;

            currentTarget?.OnFocusExit();
            currentTarget = best;
            currentTarget?.OnFocusEnter();
            OnTargetChanged?.Raise(currentTarget);
        }
    }

    /// <summary>
    /// Interaction 중 하이라이트 및 버튼 숨기기
    /// </summary>
    /// <param name="target"></param>
    public void InteractedTarget(IInteractable target = null)
    {
        target?.OnFocusExit();
        currentTarget = null;

        OnTargetChanged?.Raise(currentTarget);
    }

    private void HandleInteractInput(IInteractor interactor)
    {
        if (currentTarget == null || !currentTarget.IsAvaliable) return;

        PlayerStopEvent?.Raise(new Void());
        currentTarget.Interact(interactor);
    }
}
