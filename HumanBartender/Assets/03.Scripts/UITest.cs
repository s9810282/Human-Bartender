using UnityEngine;
using VContainer;

/// <summary>
/// 정산 및 재화 UI를 테스트하는 임시 컴포넌트.
/// Start()에서 테스트용 돈/판매 데이터를 세팅하고 정산 이벤트를 발생시킨다.
/// </summary>
public class UITest : MonoBehaviour
{
    [SerializeField] PlayerDataSO playerDataAsset;
    [SerializeField] UIDialogueTextView textView;
    [SerializeField] private UICashPanel currency;
    [SerializeField] private SettlementUI settlement;
    [SerializeField] VoidEvent showSettleMent;

    [Inject] ISettlementLog settlementLog;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);

        settlementLog.SetCost(100);
        settlementLog.AddSalesQty("gin_fizz", 1);
        settlementLog.AddSalesQty("gin_fizz", 1);
        settlementLog.AddSalesQty("dry_martini", 1);
        settlementLog.AddSalesQty("cosmopolitan", 1);
        settlementLog.AddSalesQty("godfather", 1);
        settlementLog.AddSalesQty("mojito", 1);

        showSettleMent?.Raise(new Void());

        //textView.StartType(
        //    new TypingData(
        //    "[테스트] 표정 전환angerasdaasdasdadadadadadadada\nasdasdadadadadaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", 
        //    "Luna", 
        //    Vector2.zero,
        //    Color.white, 
        //    true
        //    )).Forget();

    }
}
