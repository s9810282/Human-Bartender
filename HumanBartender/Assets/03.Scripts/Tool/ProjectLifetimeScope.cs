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

        // 데이터 로더는 하나다. 구형 DataLoadManager가 채우던 SO까지 이쪽이 이어받았고,
        // 구형 일차 교체(IDataSwitcher)도 여기 구현이 붙어 있어 AsImplementedInterfaces로 함께 등록된다.
        //
        // 구형 로더를 여기서 다시 등록하면 안 된다. RegisterComponentInHierarchy는 꺼진 오브젝트를
        // 찾지 못해, 그 컴포넌트를 끈 순간 HomeManager의 주입이 터진다.
        builder.RegisterComponentInHierarchy<NewDataLoadManager>()
            .AsImplementedInterfaces();
        //치우 수정
        builder.RegisterInstance(GameStateManager.Instance);
        builder.Register<IConditionUtil, ConditionUtil>(Lifetime.Singleton);
    }
}
