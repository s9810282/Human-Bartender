using UnityEngine;
using VContainer;

/// <summary>
/// Home 씬 매니저. 정산 비용을 계산하여 정산 UI를 표시하고,
/// 플레이어가 확인하면 일차를 올리고 데이터를 교체한 뒤 Outside 씬으로 전환한다.
/// </summary>
public class HomeManager : MonoBehaviour
{
    [SerializeField] SettlementDataSO settlementData;
    [SerializeField] VoidEvent showSettleMent;
    [SerializeField] VoidEvent Refresh;
    [SerializeField] Player luna;
    [Inject] ISoundManager soundManager;
    [Inject] IPlayerDataReader playerData;
    [Inject] ISettlementLog settlementLog;
    [Inject] IDataSwitcher dataSwitcher;
    private void Start()
    {
        soundManager.StopBGM();
    }
    public void gotosleep()
    {
        settlementLog.SetCost(CalculateCost(settlementData.dailySettlement[GameStateManager.Instance.CurrentDay]));
        showSettleMent?.Raise(new Void());
    }

    public async void CheckSettlement()
    {
        GameStateManager.Instance.CurrentDay++;

        Logger.Log($"current Day{GameStateManager.Instance.CurrentDay}");

        string dayFileName = $"day{GameStateManager.Instance.CurrentDay}.json";
        string dayCraftFileName = $"day{GameStateManager.Instance.CurrentDay}_crafts.json";

        dataSwitcher.SwitchDay(dayFileName, dayCraftFileName);

        GameStateManager.Instance.GameFlow = EGameFlow.CommuteIn;

        Refresh?.Raise(new Void());

        await SceneTransitionManager.Instance.FadeInAsync(0.5f);
        luna.State = EInteractorState.None;
    }

    public int CalculateCost(DailySettlementData data)
    {
        int cost = 0;

        foreach (var item in data.Expenses)
        {
            if (item.Condition == null)
                cost += item.Amount;
            else
            {
                bool isPay = CheckCondition(item.Condition);

                if (isPay)
                    cost += item.Amount;
            }
        }

        return cost;
    }
    public bool CheckCondition(OutsideCondition? checkType)
    {
        if (checkType == null) return true;

        switch (checkType.Value.Type)
        {
            case EConditionCheckType.None:
                break;

            case EConditionCheckType.Affinity:
                int characterTier = playerData.GetCurCharacterAffinityValue(checkType.Value.Character);
                int targettier = checkType.Value.Min;
                return characterTier >= targettier;

            case EConditionCheckType.Skill:
                return playerData.GetSkillValue() >= checkType.Value.Min;

            case EConditionCheckType.Money:
                return playerData.HasEnoughMoney(checkType.Value.Min);

            case EConditionCheckType.Flag:
                //추후 작업
                break;
        }

        return false;
    }
}
