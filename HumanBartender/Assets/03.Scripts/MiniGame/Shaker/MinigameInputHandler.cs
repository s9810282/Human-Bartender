using UnityEngine;
using UnityEngine.InputSystem;


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
