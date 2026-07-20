using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Play(Bar) 씬 전용 입력 어댑터. 스페이스바로 대사를 진행시키고, Q/E로 카메라 슬롯(왼쪽/오른쪽)을 전환한다.
/// </summary>
public class PlayInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private PlayCamera playCamera;

    /// <summary>대사 진행 입력. 화면 클릭과 동일하게 DialogueRunner의 진행 로직을 호출한다.</summary>
    public void OnAdvance(InputValue value)
    {
        dialogueRunner.OnAdvanceInput();
    }

    /// <summary>카메라를 현재 슬롯 기준 왼쪽 옆 칸으로 전환한다. 컷씬/미니게임 등 Play 상태가 아닐 때는 무시한다.</summary>
    public void OnLeft(InputValue value)
    {
        if (GameStateManager.Instance.CurrentGameState != GameState.Play) return;
        playCamera.MoveAdjacent(-1, 0.5f);
    }

    /// <summary>카메라를 현재 슬롯 기준 오른쪽 옆 칸으로 전환한다. 컷씬/미니게임 등 Play 상태가 아닐 때는 무시한다.</summary>
    public void OnRight(InputValue value)
    {
        if (GameStateManager.Instance.CurrentGameState != GameState.Play) return;
        playCamera.MoveAdjacent(1, 0.5f);
    }
}
