using UnityEngine;

public interface ITrackedble : IEntity
{
    Vector2 ButtonOffset { get; }
    Vector2 TextOffset { get; }
    bool IsAvaliable { get; }
}


/// <summary>
/// Unity측에서 IEntity가 2번 상속되는 등의 일을 알아서 처리해줌.
/// 사실 IEntity를 뺴도되지만 좀 더 직관적으로 보게 하기 위함.
/// </summary>
public interface IInteractable : IEntity, ITrackedble
{
    int Priority { get; }
    string Label { get; }
    bool IsInteracting { get; set; }

    public void Interact(IInteractor player);
    public void OnFocusEnter();
    public void OnFocusExit();
}
