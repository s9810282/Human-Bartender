using LiquidSimulation;
using UnityEngine;

public class PouringTest : MonoBehaviour
{
    [SerializeField] private LiquidData[] liquidData;
    [SerializeField] private LiquidContainer glass;
    [SerializeField] private int amount = 80;

    public void Start()
    {
       
    }

    // 버튼이나 이벤트에서 호출
    public void Pour(Void n)
    {
        int rand = Random.Range(0, liquidData.Length);
        PouringSystem.Instance.StartDirectPour(liquidData[rand], glass, amount);
    }
    public void Pour()
    {
        PouringSystem.Instance.StartDirectPour(liquidData[0], glass, amount);
    }
}
