using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public class ElevatorCamera : MonoBehaviour
{
    [SerializeField] Vector3 targetPos;
    [SerializeField] float duration = 1f;

    [Inject] ICameraMove cameraMove;
    [Inject] ICameraZoom cameraZoom;

    Transform target;

    public void Init(Transform t)
    {
        target = t;

        cameraMove.CameraMove(targetPos, duration);
        cameraZoom.ActionZoom(ECameraZoomType.Sub);
    }

    public void Handle()
    {

    }

    public void Exit()
    {

    }
}
