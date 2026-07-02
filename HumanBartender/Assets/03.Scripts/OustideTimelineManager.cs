using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Outside 씬에서 Unity Timeline 컷씬 재생을 담당하는 인터페이스.
/// </summary>
public interface  IOutsideTimeliner
{
    /// <summary>등록된 id에 해당하는 Timeline 에셋을 재생한다.</summary>
    public void PlayTimelineCutScene(string id);
    /// <summary>전달받은 TimelineAsset을 직접 재생한다.</summary>
    public void PlayTimelineCutScene(TimelineAsset timeline);
    /// <summary>CutsceneDialogueHandler에 대사 라인 목록을 초기화한다.</summary>
    public void InitHandler(List<CutsceneLine> lines);
}

[System.Serializable]
public class OutsideTimeline
{
    public string id;
    public TimelineAsset asset;
}



/// <summary>
/// Outside 씬의 Unity Timeline 컷씬 재생 매니저.
/// id 기반으로 TimelineAsset을 조회하거나 직접 에셋을 받아 PlayableDirector로 재생한다.
/// </summary>
public class OustideTimelineManager : MonoBehaviour, IOutsideTimeliner
{
    [Header("Timeline")]
    [SerializeField] PlayableDirector director;
    [SerializeField] CutsceneDialogueHandler handler;
    [SerializeField] private List<OutsideTimeline> timelineList = new();

    private Dictionary<string, OutsideTimeline> _timelines;

    private void Start()
    {
        _timelines = new Dictionary<string, OutsideTimeline>();

        foreach (var s in timelineList)
        {
            if (s != null && !string.IsNullOrEmpty(s.id)) _timelines[s.id] = s;
        }
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            director.Play();
        }
    }

    public void PlayTimelineCutScene(string id)
    {
        if (director == null) return;

        TimelineAsset timeline = _timelines[id].asset;
        director.playableAsset = timeline;
        director.time = 0;
        director.Play();
    }
    public void PlayTimelineCutScene(TimelineAsset timeline)
    {
        if (director == null) return;

        director.playableAsset = timeline;
        director.time = 0;
        director.Play();
    }

    public void OnTriggerEnding()
    {
        SceneTransitionManager.Instance.LoadScene("TempEnding");
    }

    public void InitHandler(List<CutsceneLine> lines)
    {
        handler.Init(lines);
    }
}
