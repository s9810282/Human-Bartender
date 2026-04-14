using VContainer;
using VContainer.Unity;

public class InGameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CocktailCraftManager>()
         .As<ICocktailCraft>();
        builder.RegisterComponentInHierarchy<CameraController>()
         .As<ICameraZoom>();
        builder.RegisterComponentInHierarchy<CameraController>()
         .As<ICameraMove>();

        builder.RegisterComponentInHierarchy<DialogueCharacterManager>()
            .As<ICharacterSetter>();
        builder.RegisterComponentInHierarchy<DialogueCharacterManager>()
            .As<IDialogueFader>(); ;

    }
}
