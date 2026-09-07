using UnityEngine;

public class HomeSofa : InteractiveEntity
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [SerializeField] HomeManager manager;

    public override async void Interact(IInteractor player)
    {
        isInteracting = true;
        isInteract = false;
        player.State = EInteractorState.Interct;
        OnInteracted?.Raise(this);
        Debug.Log("[Sleep] 페이드 시작");
        await SceneTransitionManager.Instance.FadeOutAsync(0.5f);

        Debug.Log($"[Sleep] 페이드 완료 / manager={manager}");
        manager.gotosleep();
    }
}
