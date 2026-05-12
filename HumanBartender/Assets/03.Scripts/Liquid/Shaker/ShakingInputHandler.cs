using UnityEngine;
using UnityEngine.InputSystem;


public class ShakingInputHandler : MonoBehaviour
{
    VoidEvent startGame;
    VoidEvent onClickEvent;

    bool isInit = false;

    public void OnClick(InputValue value)
    {
        if (value.isPressed)
        {
            Logger.Log("On Down");

            if (isInit)
            {
                isInit = true;
                startGame?.Raise(new Void());
            }
            else
            {
                onClickEvent?.Raise(new Void());
            }
        }
        else
        {
            Logger.Log("On Up");
        }
    }
}
