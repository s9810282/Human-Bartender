using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] PlayerDataSO playerData;

    protected override void Configure(IContainerBuilder builder)
    {

        builder.RegisterComponentInHierarchy<IngredientLibrary>();

        builder.RegisterComponentInHierarchy<CutSceneManager>()
       .AsSelf()
       .As<IEffectPlayer>()
       .As<ICutScenePlayer>();
        
        builder.RegisterInstance(playerData)
                      .As<IPlayerDataReader>()
                      .As<IPlayerDataWriter>();
    }
}
