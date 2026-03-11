using LiquidSimulation;
using UnityEngine;

/// <summary>
/// 각 액체 종류의 속성을 정의하는 ScriptableObject
/// Unity 에디터에서 Create > Liquid Simulation > Liquid Data 로 생성
/// </summary>
[CreateAssetMenu(fileName = "NewLiquidData", menuName = "Liquid Simulation/Liquid Data")]
public class LiquidData : ScriptableObject
{
    public LiquidType liquidType;
    public Color32 color = new Color32(255, 255, 255, 255);
    [Range(0.1f, 2.0f)]
    public float density = 1.0f;         // 밀도: 그레나딘(1.8) > 칼루아(1.4) > 주스(1.1) > 보드카(0.8)
    [Range(0f, 1f)]
    public float viscosity = 0.3f;       // 점성: 높을수록 천천히 움직임
    public string displayName = "Unknown";
}
