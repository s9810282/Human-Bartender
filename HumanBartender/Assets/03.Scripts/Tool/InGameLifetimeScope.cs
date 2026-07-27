using VContainer;
using VContainer.Unity;

/// <summary>
/// 인게임(Bar) 씬 전용 VContainer 스코프.
/// CocktailCraftManager, CameraControllerNew, PlayCamera, DialogueCharacterManager를 인터페이스로 등록한다.
/// </summary>
public class InGameLifetimeScope : LifetimeScope
{
    private CutSceneManager _cutSceneManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CocktailCraftManager>()
         .As<ICocktailCraft>();

        builder.RegisterComponentInHierarchy<CameraControllerNew>()
         .As<ICameraControlNew>();

        builder.RegisterComponentInHierarchy<PlayCamera>()
         .As<ISlotCamera>();

        builder.RegisterComponentInHierarchy<DialogueCharacterManager>()
            .As<ICharacterSetter>()
            .As<IDialogueFader>();

        builder.RegisterComponentInHierarchy<DialogueRunner>();
        builder.RegisterComponentInHierarchy<DialogueTriggerManager>();
        builder.RegisterComponentInHierarchy<VisualNovelFlow>();

        builder.RegisterComponentInHierarchy<TycoonFlow>();
        builder.RegisterComponentInHierarchy<PlayPhaseController>();

        //builder.RegisterBuildCallback(container =>
        //{
        //    _cutSceneManager = FindAnyObjectByType<CutSceneManager>();

        //    if (_cutSceneManager != null)
        //    {
        //        var zoom = container.Resolve<ICameraControl>();

        //        _cutSceneManager.SetSceneDependencies(zoom);
        //    }
        //});
    }
}
