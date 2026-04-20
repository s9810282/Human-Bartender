using VContainer;
using VContainer.Unity;

public class InGameLifetimeScope : LifetimeScope
{
    private CutSceneManager _cutSceneManager;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CocktailCraftManager>()
         .As<ICocktailCraft>();

        builder.RegisterComponentInHierarchy<CameraController>()
         .As<ICameraZoom>()
         .As<ICameraMove>();
        
        builder.RegisterComponentInHierarchy<DialogueCharacterManager>()
            .As<ICharacterSetter>()
            .As<IDialogueFader>();

        builder.RegisterBuildCallback(container =>
        {
            _cutSceneManager = FindAnyObjectByType<CutSceneManager>();

            if (_cutSceneManager != null)
            {
                var zoom = container.Resolve<ICameraZoom>();
                var move = container.Resolve<ICameraMove>();
                _cutSceneManager.SetSceneDependencies(zoom, move);
            }
        });
    }
}
