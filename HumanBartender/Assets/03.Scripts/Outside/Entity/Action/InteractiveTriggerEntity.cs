using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// 클릭(Interact) 없이 포커스 진입만으로 컷씬을 재생하는 트리거 엔티티(예: 특정 구역 진입 시 자동 연출).
/// 실제 상호작용(Interact)은 비어 있고 OnFocusEnter에서 바로 타임라인 컷씬을 실행한다.
/// </summary>
public class InteractiveTriggerEntity : InteractiveEntity
{
    [Header("DAta")]
    [SerializeField] string cutSceneId;
    [SerializeField] List<CutsceneLine> lines;

    [Inject] IOutsideTimeliner timeliner;

    /// <summary>InteractiveEntityManager가 데이터 기반으로 재생할 컷씬 id를 주입한다.</summary>
    public void SetId(string id) => cutSceneId = id;


    public override void Interact(IInteractor player)
    {

    }

    /// <summary>포커스(감지 범위 진입) 시 1회성으로 컷씬을 재생하고, 재사용되지 않도록 즉시 비활성화한다.</summary>
    public override void OnFocusEnter()
    {
        OnInteracted?.Raise(this);
        IsAvaliable = false;

        timeliner.InitHandler(lines);
        timeliner.PlayTimelineCutScene(cutSceneId);
    }

    public override void OnFocusExit()
    {

    }
}
