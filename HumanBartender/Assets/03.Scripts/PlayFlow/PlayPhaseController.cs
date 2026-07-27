using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// Play 씬 진입 시 1부(TycoonFlow)와 2부(VisualNovelFlow)를 순서대로 실행하는 중간관리자.
/// 각 Flow는 이 컨트롤러가 호출할 때만 시작되며, 완료(RunAsync 반환) 시 다음 국면으로 넘어간다.
/// </summary>
public class PlayPhaseController : MonoBehaviour
{
    [Inject] TycoonFlow tycoonFlow;
    [Inject] VisualNovelFlow dialogueFlow;

    public EPlayPhase CurrentPhase { get; private set; } = EPlayPhase.Tycoon;

    private void Start()
    {
        RunDayAsync().Forget();
    }

    public void OpenBar()
    {
        RunDayAsync().Forget();
    }

    private async UniTask RunDayAsync()
    {
        CurrentPhase = EPlayPhase.Tycoon;
        await tycoonFlow.RunAsync();

        CurrentPhase = EPlayPhase.Dialogue;
        await dialogueFlow.RunAsync();
    }
}
