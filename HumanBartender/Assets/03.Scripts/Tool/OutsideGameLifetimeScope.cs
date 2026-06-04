using VContainer;
using VContainer.Unity;

public class OutsideGameLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<CameraControllerNew>()
        .As<ICameraControlNew>();
    }
}
