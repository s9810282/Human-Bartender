using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] PlayerDataSO playerData;

    protected override void Configure(IContainerBuilder builder)
    {
        playerData.Init();

        builder.RegisterComponentInHierarchy<IngredientLibrary>();

        builder.RegisterComponentInHierarchy<CutSceneManager>()
       .AsSelf()
       .As<IEffectPlayer>()
       .As<ICutScenePlayer>();
        
        builder.RegisterInstance(playerData)
                      .As<IPlayerDataReader>()
                      .As<IPlayerDataWriter>();

        builder.RegisterComponentInHierarchy<SoundManager>()
        .As<ISoundManager>();

        builder.RegisterComponentInHierarchy<UIDisplayOptions>();
        builder.Register<DisplaySettings>(Lifetime.Singleton)
            .AsImplementedInterfaces();
    }
}
