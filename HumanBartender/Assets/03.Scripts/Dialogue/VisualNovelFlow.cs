using Cysharp.Threading.Tasks;
using UnityEngine;

public class VisualNovelFlow : MonoBehaviour
{
    [SerializeField] DayDataSO dayData;
    [SerializeField] DialogueRunner runner;

    public async UniTask PlayDayAsync()
    {
        GameStateManager.Instance.CurrentGameState = GameState.Play;
        foreach (var scene in dayData.dayData.Scenes)
        {
            await runner.PlayAsync(scene.Dialogues);
        }
    }
}