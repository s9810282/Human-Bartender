using UnityEngine;
using VContainer;

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
