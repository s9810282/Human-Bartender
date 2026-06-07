using System.Collections.Generic;
using UnityEngine;

interface ISettlementLog
{
    public void AddSalesQty(string id, int val);
    public void AddTip(string id, int val);
    public void SetCost(int cost);
    
}


[CreateAssetMenu(fileName = "PlayerSettlement", menuName = "Scriptable Objects/PlayerSettlement")]
public class PlayerSettlement : ScriptableObject, ISettlementLog
{

    public int maintenanceCost;                             // 가게 유지비 (차감)
    public Dictionary<string, int> salesQty = new();        // 판매 내역: 메뉴명 -> 수량
    public Dictionary<string, int> tips = new();            // 팁 내역  : 메뉴명 -> 팁 금액

    public void Init()
    {
        this.maintenanceCost = 0;
        this.salesQty = new Dictionary<string, int>();
        this.tips = new Dictionary<string, int>();
    }


    public void AddSalesQty(string id, int val)
    {
        if (salesQty.ContainsKey(id))
            salesQty[id] += val;
        else
            salesQty.Add(id, val);
    }
    public void AddTip(string id, int val)
    {
        if (tips.ContainsKey(id))
            tips[id] += val;
        else
            tips.Add(id, val);
    }
    public void SetCost(int cost)
    {
        maintenanceCost = cost;
    }
}