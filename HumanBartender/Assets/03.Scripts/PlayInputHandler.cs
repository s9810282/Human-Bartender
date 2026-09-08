using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Play(Bar) 씬 전용 입력 어댑터. 스페이스바로 대사를 진행시키고, Q/E로 카메라 슬롯(왼쪽/오른쪽)을 전환한다.
/// </summary>
public class PlayInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner dialogueRunner;

    [Tooltip("2부 대본 실행기. 대본이 도는 동안에는 이쪽이 진행 입력을 받는다. 비워 두면 기존 경로만 쓴다.")]
    [SerializeField] private StoryFlow storyFlow;
    [SerializeField] private PlayCamera playCamera;
    [SerializeField] private PlayPhaseController playPhaseController;

    /// <summary>
    /// 대사 진행 입력. 2부(Dialogue) 국면에서만 동작한다.
    ///
    /// 대본을 도는 실행기가 둘이라 먼저 물어보고 넘긴다. 둘이 동시에 돌지는 않지만, 어느 쪽이
    /// 받을지를 입력 쪽이 알 필요는 없어서 실행 중인 쪽이 스스로 답하게 한다.
    /// </summary>
    public void OnAdvance(InputValue value)
    {
        if (playPhaseController.CurrentPhase != EPlayPhase.Dialogue) return;

        if (storyFlow != null && storyFlow.TryAdvance()) return;

        dialogueRunner.OnAdvanceInput();
    }

    /// <summary>카메라를 현재 슬롯 기준 왼쪽 옆 칸으로 전환한다. 컷씬/미니게임 또는 1부(Tycoon) 국면일 때는 무시한다.</summary>
    public void OnLeft(InputValue value)
    {
        if (playPhaseController.CurrentPhase != EPlayPhase.Tycoon) return;
        playCamera.MoveAdjacent(-1, 0.5f);
    }

    /// <summary>카메라를 현재 슬롯 기준 오른쪽 옆 칸으로 전환한다. 컷씬/미니게임 또는 1부(Tycoon) 국면일 때는 무시한다.</summary>
    public void OnRight(InputValue value)
    {
        if (playPhaseController.CurrentPhase != EPlayPhase.Tycoon) return;
        playCamera.MoveAdjacent(1, 0.5f);
    }
}
