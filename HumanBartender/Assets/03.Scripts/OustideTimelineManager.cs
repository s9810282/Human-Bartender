using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public interface  IOutsideTimeliner
{
    public void PlayTimelineCutScene(string id);
    public void PlayTimelineCutScene(TimelineAsset timeline);

    public void InitHandler(List<CutsceneLine> lines);
}

[System.Serializable]
public class OutsideTimeline
{
    public string id;
    public TimelineAsset asset;
}



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

    public void InitHandler(List<CutsceneLine> lines)
    {
        handler.Init(lines);
    }
}
