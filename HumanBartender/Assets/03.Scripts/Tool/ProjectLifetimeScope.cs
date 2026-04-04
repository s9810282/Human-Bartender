using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<IngredientLibrary>();
        builder.RegisterComponentInHierarchy<CutSceneManager>();
    }
}
