using System.Collections.Generic;
using UnityEngine;

/// <summary>슬롯 타입(Left/Right/Middle)별로 카메라(cameraAnchor)가 따라갈 Transform과 오프셋을 매핑하는 데이터.</summary>
[System.Serializable]
public class SlotCameraOption
{
    public ESlotType slotType = ESlotType.Middle;
    public Transform cameraParent;
    public Vector3 cameraOffset = Vector3.zero;
}

/// <summary>
/// Play(Bar) 씬 전용 카메라 슬롯 전환기. CameraControllerNew(cameraZoom)를 통해 cameraAnchor를
/// 슬롯별 Transform(cameraParent)의 자식으로 붙이고 오프셋을 전환시킨다. OutsideCamera와 동일한 패턴.
/// </summary>
public class PlayCamera : MonoBehaviour, ISlotCamera
{
    [Header("References")]
    [SerializeField] private CameraControllerNew cameraZoom;

    [Header("Slot Options")]
    [SerializeField] private SlotCameraOption[] slotOptions;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private static readonly ESlotType[] SlotOrder = { ESlotType.Left, ESlotType.Middle, ESlotType.Right };

    private Dictionary<ESlotType, SlotCameraOption> _slotMap;
    private int _currentSlotIndex = 1; // Middle

    private void Start()
    {
        _slotMap = new Dictionary<ESlotType, SlotCameraOption>(slotOptions.Length);
        foreach (var opt in slotOptions)
        {
            _slotMap[opt.slotType] = opt;
        }
    }

    /// <summary>지정된 슬롯의 Transform으로 cameraAnchor를 dur초 동안 부드럽게 이동시킨다.</summary>
    public void MoveToSlot(ESlotType slot, float dur = 1f)
    {
        var opt = _slotMap[slot];
        cameraZoom.FollowTarget(opt.cameraParent);
        cameraZoom.TransitionFollowOffset(opt.cameraOffset, dur, ease);

        int idx = System.Array.IndexOf(SlotOrder, slot);
        if (idx >= 0) _currentSlotIndex = idx;
    }

    /// <summary>현재 슬롯 기준으로 한 칸 옆(step: -1=왼쪽, +1=오른쪽) 슬롯으로 이동시킨다.</summary>
    public void MoveAdjacent(int step, float dur = 1f)
    {
        _currentSlotIndex = Mathf.Clamp(_currentSlotIndex + step, 0, SlotOrder.Length - 1);
        MoveToSlot(SlotOrder[_currentSlotIndex], dur);
    }
}
