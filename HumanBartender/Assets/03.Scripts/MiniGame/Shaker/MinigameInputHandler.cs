using UnityEngine;
using UnityEngine.InputSystem;


/// <summary>
/// 미니게임 입력 핸들러. 첫 번째 클릭은 게임 시작 이벤트를 발생시키고,
/// 이후 클릭은 누름/뗌 이벤트로 전달한다.
/// </summary>
public class MinigameInputHandler : MonoBehaviour
{
    [SerializeField] VoidEvent startGame;
    [SerializeField] VoidEvent onPressEvent;
    [SerializeField] VoidEvent onReleaseEvent;

    bool isInit = false;

    public void OnClick(InputValue value)
    {
        if (value.isPressed)
        {
            if (!isInit)
            {
                isInit = true;
                startGame?.Raise(new Void());
            }
            else
            {
                onPressEvent?.Raise(new Void());
            }
        }
        else
        {
            onReleaseEvent?.Raise(new Void());
        }
    }
}
