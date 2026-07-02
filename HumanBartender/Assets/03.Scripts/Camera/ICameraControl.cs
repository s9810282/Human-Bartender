using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>인게임(Bar) 씬 카메라의 줌/이동을 담당하는 인터페이스.</summary>
public interface ICameraControl
{

    public void ActionZoomAndBack(ECameraZoomType zoomType = ECameraZoomType.Base, UniTaskCompletionSource tcs = null);
    public void ActionZoom(ECameraZoomType zoomType = ECameraZoomType.Base);
    public void CameraZoom(ECameraZoomType zoomType = ECameraZoomType.Base, float dur = 1f);

    public void CameraMove(ESlotType slot, float dur = 1f);
    public void CameraMove(Vector3 pos, float dur = 1);
}
