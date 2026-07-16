/// <summary>Play(Bar) 씬에서 카메라를 슬롯(Left/Right/Middle) 위치로 이동시키는 인터페이스.</summary>
public interface ISlotCamera
{
    public void MoveToSlot(ESlotType slot, float dur = 1f);
}
