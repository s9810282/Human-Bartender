using UnityEngine;
using UnityEngine.InputSystem;


public class ShakingInputHandler : MonoBehaviour
{
    [SerializeField] VoidEvent startGame;
    [SerializeField] VoidEvent onClickEvent;

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
                onClickEvent?.Raise(new Void());
            }
        }
        else
        {

        }
    }
}
