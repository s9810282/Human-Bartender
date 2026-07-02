/// <summary>현재 플레이 중인 일차/제조 데이터를 교체하는 인터페이스.</summary>
public interface IDataSwitcher
{
    void SwitchDay(string dayId, string craftId);
}
