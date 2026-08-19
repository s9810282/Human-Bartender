using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 스터 입력. WASD와 화살표를 같은 4방위로 묶어 StirManager의 큐에 밀어 넣는다.
///
/// Cap처럼 Update에서 폴링하지 않고 InputAction 콜백을 쓰는 이유는 명세 §7의 "동시에 여러 키가
/// 들어오면 들어온 순서대로 처리한다" 때문이다. 폴링은 한 프레임에 눌린 키들을 필드 선언 순서로밖에
/// 볼 수 없어서 실제 입력 순서를 알 수 없다.
///
/// OS 키 반복(누르고 있을 때 반복 입력)은 Input System이 상태 변화만 이벤트로 만들기 때문에
/// 애초에 오지 않는다 — 처음 눌린 한 번만 판정된다.
/// </summary>
public class StirInputHandler : MonoBehaviour
{
    [SerializeField] StirManager stir;

    /// <summary>EStirDirection 순서(W, D, S, A)와 인덱스를 맞춘 액션 묶음.</summary>
    InputAction[] actions;
    Action<InputAction.CallbackContext>[] callbacks;

    void Awake()
    {
        BuildActions();

        // 판정이 끝나면 입력을 해제한다 (명세 §7). 매니저가 완료 시점을 알려주는 쪽이
        // 매 프레임 IsCompleted를 확인하는 것보다 확실하다.
        if (stir != null) stir.Completed += DisableActions;
    }

    void OnEnable() => EnableActions();

    void OnDisable() => DisableActions();

    void OnDestroy()
    {
        if (stir != null) stir.Completed -= DisableActions;

        if (actions == null) return;

        for (int i = 0; i < actions.Length; i++)
        {
            if (callbacks[i] != null) actions[i].performed -= callbacks[i];
            actions[i].Dispose();
        }

        actions = null;
        callbacks = null;
    }

    void BuildActions()
    {
        actions = new InputAction[StirDirections.Count];
        callbacks = new Action<InputAction.CallbackContext>[StirDirections.Count];

        CreateAction((int)EStirDirection.Up, "<Keyboard>/w", "<Keyboard>/upArrow");
        CreateAction((int)EStirDirection.Right, "<Keyboard>/d", "<Keyboard>/rightArrow");
        CreateAction((int)EStirDirection.Down, "<Keyboard>/s", "<Keyboard>/downArrow");
        CreateAction((int)EStirDirection.Left, "<Keyboard>/a", "<Keyboard>/leftArrow");
    }

    void CreateAction(int direction, string keyPath, string arrowPath)
    {
        var action = new InputAction($"Stir_{StirDirections.KeyLabel(direction)}", InputActionType.Button);
        action.AddBinding(keyPath);
        action.AddBinding(arrowPath);

        // 람다를 그대로 넘기면 해제할 때 같은 참조를 못 찾는다. 델리게이트를 보관해두고 뗀다.
        Action<InputAction.CallbackContext> callback = _ => stir?.EnqueueDirection(direction);
        action.performed += callback;

        actions[direction] = action;
        callbacks[direction] = callback;
    }

    void EnableActions()
    {
        if (actions == null) return;

        foreach (InputAction action in actions) action.Enable();
    }

    void DisableActions()
    {
        if (actions == null) return;

        foreach (InputAction action in actions) action.Disable();
    }
}
