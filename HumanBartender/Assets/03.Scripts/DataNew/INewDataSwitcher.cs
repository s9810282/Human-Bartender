/// <summary>현재 진행 중인 날짜(json/script/day_N.json)의 스크립트 데이터를 교체하는 인터페이스.</summary>
public interface INewDataSwitcher
{
    void SwitchDay(int day);
}
