using VContainer;
using VContainer.Unity;

public class InGameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CocktailCraftManager>();
    }
}
