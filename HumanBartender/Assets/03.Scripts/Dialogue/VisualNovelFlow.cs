using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 하루(Day) 단위 데이터에 포함된 여러 씬(Scene)을 순서대로 DialogueRunner에 넘겨 재생하는 진입점.
/// </summary>
public class VisualNovelFlow : MonoBehaviour
{
    [SerializeField] DayDataSO dayData;
    [SerializeField] DialogueRunner runner;

    /// <summary>게임 상태를 Play로 전환하고, 하루 데이터의 모든 씬을 순차적으로 재생한다.</summary>
    public async UniTask PlayDayAsync()
    {
        GameStateManager.Instance.CurrentGameState = GameState.Play;
        foreach (var scene in dayData.dayData.Scenes)
        {
            await runner.PlayAsync(scene.Dialogues);
        }
    }
}