using System.Diagnostics;
using VContainer;

public interface IConditionEvaluator
{
    bool Check(string when);
}

public class ConditionEvaluator : IConditionEvaluator
{
    private readonly GameStateManager _gameStateManager;
    private readonly IPlayerDataReader _playerDataReader;

    // 멤버 변수 활용 가능
    private int _cachedDay;

    // VContainer가 등록된 GameStateManager와 IPlayerDataReader를 자동으로 주입
    [Inject]
    public ConditionEvaluator(GameStateManager gameStateManager, IPlayerDataReader playerDataReader)
    {
        _gameStateManager = gameStateManager;
        _playerDataReader = playerDataReader;
    }

    public bool Check(string when)
    {
        UnityEngine.Debug.Log($"[ConditionEvaluator] Check() 호출됨! 조건식: {when} | 현재 Day: {_gameStateManager?.CurrentDay}");
        return true;
    }
}