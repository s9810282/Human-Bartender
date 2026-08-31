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

        builder.RegisterComponentInHierarchy<PlayPhaseController>();

        // 2부 대본 국면. [Inject]로 플레이어 데이터와 사운드를 받는다.
        builder.RegisterComponentInHierarchy<StoryFlow>();

        // 1부 제조 루프. 기믹 큐가 띄우는 미니게임에도 사운드·데이터를 주입해 줘야 해서
        // 실행기가 IObjectResolver를 받을 수 있도록 스코프에 올린다.
        builder.RegisterComponentInHierarchy<GimmickRunner>();
        builder.RegisterComponentInHierarchy<CraftFlowController>();

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
