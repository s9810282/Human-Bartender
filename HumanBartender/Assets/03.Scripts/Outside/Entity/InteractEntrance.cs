using UnityEngine;

public class InteractEntrance : InteractiveEntity
{
    [Space(20f)]
    [SerializeField] public Vector2 spawnPoint;
    [SerializeField] EGameFlow eGameFlow;
    [SerializeField] string targetScene;

    public override void Interact(IInteractor player)
    {
        if (GameStateManager.Instance.GameFlow == eGameFlow)
            SceneTransitionManager.Instance.LoadScene(targetScene);
    }
}
