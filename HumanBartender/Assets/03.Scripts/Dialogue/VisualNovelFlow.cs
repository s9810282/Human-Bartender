using Cysharp.Threading.Tasks;
using System;
using UnityEngine;
using VContainer;

/// <summary>
/// 하루(Day) 단위 데이터에 포함된 여러 씬(Scene)을 순서대로 DialogueRunner에 넘겨 재생하는 진입점.
/// Play 씬의 2부(Dialogue) 국면을 담당하며, PlayPhaseController가 1부(Tycoon) 종료 후 호출한다.
/// DialogueManager(레거시)의 InitSystem을 대체한다.
/// </summary>
public class VisualNovelFlow : MonoBehaviour, IPlayPhaseFlow
{
    [SerializeField] DayDataSO dayData;
    [SerializeField] DialogueRunner runner;
    [SerializeField] MonoBehaviour presenter; // IDialoguePresenter 구현체

    [Inject] ISoundManager soundManager;

    public UniTask RunAsync() => PlayDayAsync();

    /// <summary>게임 상태를 Play로 전환하고, 하루 데이터의 모든 씬을 순차적으로 재생한다.</summary>
    public async UniTask PlayDayAsync()
    {
        runner.Bind(presenter as IDialoguePresenter);

        await UniTask.Delay(TimeSpan.FromSeconds(3f));

        GameStateManager.Instance.IsDialogInitStart = true;
        GameStateManager.Instance.GameFlow = EGameFlow.Bar;
        GameStateManager.Instance.CurrentGameState = GameState.Play;

        soundManager.PlayBGM("BGM_bar_01", 1f, true);

        foreach (var scene in dayData.dayData.Scenes)
        {
            await runner.PlayAsync(scene.Dialogues);
        }
    }
}
