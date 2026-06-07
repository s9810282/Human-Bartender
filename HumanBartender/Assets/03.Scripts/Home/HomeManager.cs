using UnityEngine;
using VContainer;

public class HomeManager : MonoBehaviour
{
    [SerializeField] SettlementDataSO settlementData;
    [SerializeField] VoidEvent showSettleMent;

    [Inject] ISoundManager soundManager;
    [Inject] IPlayerDataReader playerData;
    [Inject] ISettlementLog settlementLog;
    [Inject] IDataSwitcher dataSwitcher;



    private void Start()
    {
        soundManager.StopBGM();
        settlementLog.SetCost(CalculateCost(settlementData.dailySettlement[GameStateManager.Instance.CurrentDay]));

        showSettleMent?.Raise(new Void());
    }


    public void CheckSettlement()
    {
        GameStateManager.Instance.CurrentDay++;

        Logger.Log($"current Day{GameStateManager.Instance.CurrentDay}");

        string dayFileName = $"day{GameStateManager.Instance.CurrentDay}.json";
        string dayCraftFileName = $"day{GameStateManager.Instance.CurrentDay}_crafts.json";

        dataSwitcher.SwitchDay(dayFileName, dayCraftFileName);

        GameStateManager.Instance.GameFlow = EGameFlow.CommuteIn;

        SceneTransitionManager.Instance.LoadScene("Outside");
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
