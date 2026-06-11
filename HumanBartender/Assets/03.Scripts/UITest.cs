using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

public class UITest : MonoBehaviour
{
    [SerializeField] PlayerDataSO playerDataAsset;
    [SerializeField] UIDialogueTextView textView;
    [SerializeField] private UICashPanel currency;
    [SerializeField] private SettlementUI settlement;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);
        playerDataAsset.AddMoney(500);



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
