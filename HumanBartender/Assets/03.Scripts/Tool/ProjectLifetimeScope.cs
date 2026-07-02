using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 프로젝트 전역 VContainer 루트 스코프.
/// CutSceneManager, PlayerData, SoundManager, DisplaySettings, DialogueHistory, DataLoadManager 등
/// 모든 씬에서 공유되는 싱글톤들을 등록한다.
/// </summary>
public class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] PlayerDataSO playerData;
    [SerializeField] PlayerSettlement playerSettlementData;

    protected override void Configure(IContainerBuilder builder)
    {
        playerData.Init();
        playerSettlementData.Init();

        builder.RegisterComponentInHierarchy<CutSceneManager>()
       .AsSelf()
       .As<IEffectPlayer>()
       .As<ICutScenePlayer>();
        
        builder.RegisterInstance(playerData)
                      .As<IPlayerDataReader>()
                      .As<IPlayerDataWriter>();

        builder.RegisterInstance(playerSettlementData)
                        .As<ISettlementLog>();

        builder.RegisterComponentInHierarchy<SoundManager>()
        .As<ISoundManager>();

        builder.RegisterComponentInHierarchy<UIDisplayOptions>();
        builder.Register<DisplaySettings>(Lifetime.Singleton)
            .AsImplementedInterfaces();

        builder.Register<DialogueHistory>(Lifetime.Singleton);
        builder.RegisterComponentInHierarchy<DialogueHistoryView>();

        builder.RegisterComponentInHierarchy<DataLoadManager>()
            .As<IDataSwitcher>();
    }
}
