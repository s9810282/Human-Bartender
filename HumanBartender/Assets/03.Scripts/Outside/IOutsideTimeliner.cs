using System.Collections.Generic;
using UnityEngine.Timeline;

/// <summary>
/// Outside 씬에서 Unity Timeline 컷씬 재생을 담당하는 인터페이스.
/// </summary>
public interface IOutsideTimeliner
{
    /// <summary>등록된 id에 해당하는 Timeline 에셋을 재생한다.</summary>
    public void PlayTimelineCutScene(string id);
    /// <summary>전달받은 TimelineAsset을 직접 재생한다.</summary>
    public void PlayTimelineCutScene(TimelineAsset timeline);
    /// <summary>CutsceneDialogueHandler에 대사 라인 목록을 초기화한다.</summary>
    public void InitHandler(List<CutsceneLine> lines);
}
